using CurrencyExchange.Data;
using CurrencyExchange.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace CurrencyExchange.Services;

public interface IExchangeRateService
{
    Task<List<ExchangeRate>> GetCurrentRatesAsync(bool forceRefresh = false);
    Task RefreshRatesAsync(bool force = false);
    Task<decimal?> GetMidRateFromApiAsync(string currencyCode);
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly IConfiguration _config;

    // Кэш на 1 час — не долбим API
    private static DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan _cacheTimeout = TimeSpan.FromHours(1);

    public ExchangeRateService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ExchangeRateService> logger,
        IConfiguration config)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    public async Task<List<ExchangeRate>> GetCurrentRatesAsync(bool forceRefresh = false)
    {
        // Берём активные валюты
        var currencies = await _db.Currencies.Where(c => c.IsActive).ToListAsync();

        // Обновляем по времени (или принудительно)
        if (forceRefresh || DateTime.UtcNow - _lastRefresh > _cacheTimeout)
        {
            await RefreshRatesAsync(force: true);
        } else {
            // Если есть активные валюты без записей в ExchangeRates (например, только что добавили/включили),
            // то всё равно подтянем их курсы.
            var missing = await _db.Currencies
                .Where(c => c.IsActive)
                .Where(c => !_db.ExchangeRates.Any(r => r.CurrencyId == c.Id))
                .AnyAsync();
            if (missing)
            {
                await RefreshRatesAsync(force: true);
            }
        }

        // Берём последний курс для каждой валюты
        var rates = await GetLatestRatesAsync(currencies);

        // Если добавили новую валюту, для которой ещё нет записей в ExchangeRates,
        // принудительно подтянем курсы (даже если кеш не истёк)
        if (rates.Count < currencies.Count)
        {
            await RefreshRatesAsync();
            rates = await GetLatestRatesAsync(currencies);
        }

        return rates;
    }

    private async Task<List<ExchangeRate>> GetLatestRatesAsync(List<Currency> currencies)
    {
        var rates = new List<ExchangeRate>();

        foreach (var currency in currencies)
        {
            var latest = await _db.ExchangeRates
                .Where(r => r.CurrencyId == currency.Id)
                .OrderByDescending(r => r.FetchedAt)
                .FirstOrDefaultAsync();

            if (latest != null)
                rates.Add(latest);
        }

        return rates;
    }

    public async Task<decimal?> GetMidRateFromApiAsync(string currencyCode)
    {
        currencyCode = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode == "RUB")
            return null;

        try
        {
            var apiKey = _config["ExchangeRateApi:Key"] ?? "";
            string url;

            // API отдаёт сколько единиц валюты за 1 RUB
            if (!string.IsNullOrEmpty(apiKey))
                url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/RUB";
            else
                url = "https://open.er-api.com/v6/latest/RUB";

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetStringAsync(url);
            var json = JObject.Parse(response);

            var rates = json["rates"] as JObject;
            if (rates == null) return null;

            if (!rates.TryGetValue(currencyCode, out var rateToken)) return null;

            decimal ratePerRub = (decimal)rateToken;
            if (ratePerRub == 0) return null;

            // Convert to: RUB per 1 currency? Actually midRate используется как RUB за 1 единицу иностранной валюты.
            // В RefreshRatesAsync: midRate = 1 / ratePerRub
            decimal midRate = 1m / ratePerRub;
            return midRate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить MidRate из API для {CurrencyCode}", currencyCode);
            return null;
        }
    }

    public async Task RefreshRatesAsync(bool force = false)
    {
        try
        {
            _logger.LogInformation("Обновление курсов валют...");

            // Используем открытый API exchangerate-api.com (бесплатный)
            // Базовая валюта RUB — получаем сколько единиц иностранной валюты за 1 RUB
            // Но проще взять базу USD и пересчитать через RUB
            var apiKey = _config["ExchangeRateApi:Key"] ?? "";
            string url;

            if (!string.IsNullOrEmpty(apiKey))
            {
                url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/RUB";
            }
            else
            {
                // Бесплатный Open API без ключа
                url = "https://open.er-api.com/v6/latest/RUB";
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetStringAsync(url);
            var json = JObject.Parse(response);

            var rates = json["rates"] as JObject;
            if (rates == null) return;

            var currencies = await _db.Currencies.Where(c => c.IsActive).ToListAsync();

            foreach (var currency in currencies)
            {
                // Курс: сколько единиц валюты за 1 RUB
                if (!rates.TryGetValue(currency.Code, out var rateToken)) continue;

                decimal ratePerRub = (decimal)rateToken;
                if (ratePerRub == 0) continue;

                // Нам нужно: сколько RUB за 1 единицу иностранной валюты
                decimal midRate = 1m / ratePerRub;

                var buyRate  = midRate * (1m - currency.BuyMargin  / 100m);
                var sellRate = midRate * (1m + currency.SellMargin / 100m);

                _db.ExchangeRates.Add(new ExchangeRate
                {
                    CurrencyId       = currency.Id,
                    BaseCurrencyCode = "RUB",
                    MidRate          = Math.Round(midRate,  4),
                    BuyRate          = Math.Round(buyRate,  4),
                    SellRate         = Math.Round(sellRate, 4),
                    FetchedAt        = DateTime.UtcNow,
                    Source           = "open.er-api.com"
                });
            }

            await _db.SaveChangesAsync();
            _lastRefresh = DateTime.UtcNow;

            _logger.LogInformation("Курсы успешно обновлены.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить курсы из API, используем последние сохранённые.");
        }
    }
}
