using CurrencyExchange.Data;
using CurrencyExchange.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrencyExchange.Services;

public interface IExchangeService
{
    Task<(decimal resultAmount, decimal rate, decimal profit)> CalculateAsync(
        int fromCurrencyId, int toCurrencyId, decimal amount, TransactionType type);

    Task<ExchangeTransaction> ExecuteAsync(
        int fromCurrencyId, int toCurrencyId, decimal amount, TransactionType type, string? note = null);
}

public class ExchangeService : IExchangeService
{
    private readonly ApplicationDbContext _db;
    private readonly IExchangeRateService _rateService;

    public ExchangeService(ApplicationDbContext db, IExchangeRateService rateService)
    {
        _db = db;
        _rateService = rateService;
    }

    public async Task<(decimal resultAmount, decimal rate, decimal profit)> CalculateAsync(
        int fromCurrencyId, int toCurrencyId, decimal amount, TransactionType type)
    {
        var rates = await _rateService.GetCurrentRatesAsync();
        var fromCurrency = await _db.Currencies.FindAsync(fromCurrencyId);
        var toCurrency   = await _db.Currencies.FindAsync(toCurrencyId);

        if (fromCurrency == null || toCurrency == null)
            throw new InvalidOperationException("Валюта не найдена.");

        // RUB — базовая валюта (Id = 0 в логике, просто обрабатываем особо)
        bool fromIsRub = fromCurrency.Code == "RUB";
        bool toIsRub   = toCurrency.Code   == "RUB";

        decimal appliedRate;
        decimal resultAmount;
        decimal profit;

        if (fromIsRub && !toIsRub)
        {
            // Клиент покупает иностранную валюту за рубли
            var rate = rates.FirstOrDefault(r => r.CurrencyId == toCurrencyId)
                       ?? throw new InvalidOperationException($"Курс для {toCurrency.Code} не найден.");
            appliedRate  = rate.SellRate;                      // пункт продаёт
            resultAmount = Math.Round(amount / appliedRate, 2);
            var midResult = amount / rate.MidRate;
            profit = Math.Round((midResult - resultAmount) * appliedRate, 2); // упрощённо
        }
        else if (!fromIsRub && toIsRub)
        {
            // Клиент продаёт иностранную валюту, получает рубли
            var rate = rates.FirstOrDefault(r => r.CurrencyId == fromCurrencyId)
                       ?? throw new InvalidOperationException($"Курс для {fromCurrency.Code} не найден.");
            appliedRate  = rate.BuyRate;                       // пункт покупает
            resultAmount = Math.Round(amount * appliedRate, 2);
            profit       = Math.Round(amount * (rate.MidRate - rate.BuyRate), 2);
        }
        else if (!fromIsRub && !toIsRub)
        {
            // Кросс-курс: fromCurrency → RUB → toCurrency
            var fromRate = rates.FirstOrDefault(r => r.CurrencyId == fromCurrencyId)
                           ?? throw new InvalidOperationException($"Курс для {fromCurrency.Code} не найден.");
            var toRate   = rates.FirstOrDefault(r => r.CurrencyId == toCurrencyId)
                           ?? throw new InvalidOperationException($"Курс для {toCurrency.Code} не найден.");

            var rubles   = amount * fromRate.BuyRate;          // сначала продаём
            resultAmount = Math.Round(rubles / toRate.SellRate, 2); // потом покупаем
            appliedRate  = Math.Round(fromRate.BuyRate / toRate.SellRate, 6);

            var rublesAtMid = amount * fromRate.MidRate;
            var resultAtMid = rublesAtMid / toRate.MidRate;
            profit = Math.Round((resultAtMid - resultAmount) * toRate.MidRate, 2);
        }
        else
        {
            throw new InvalidOperationException("Нельзя обменять RUB на RUB.");
        }

        return (resultAmount, appliedRate, Math.Max(profit, 0));
    }

    public async Task<ExchangeTransaction> ExecuteAsync(
        int fromCurrencyId, int toCurrencyId, decimal amount, TransactionType type, string? note = null)
    {
        var (resultAmount, rate, profit) = await CalculateAsync(fromCurrencyId, toCurrencyId, amount, type);

        var transaction = new ExchangeTransaction
        {
            FromCurrencyId = fromCurrencyId,
            ToCurrencyId   = toCurrencyId,
            FromAmount     = amount,
            ToAmount       = resultAmount,
            Rate           = rate,
            Profit         = profit,
            Type           = type,
            Note           = note,
            CreatedAt      = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }
}
