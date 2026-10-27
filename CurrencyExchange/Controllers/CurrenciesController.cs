using CurrencyExchange.Data;
using CurrencyExchange.Models;
using CurrencyExchange.Models.ViewModels;
using CurrencyExchange.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurrencyExchange.Controllers;

public class CurrenciesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IExchangeRateService _rateService;

    public CurrenciesController(ApplicationDbContext db, IExchangeRateService rateService)
    {
        _db = db;
        _rateService = rateService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currencies = await _db.Currencies
            .OrderBy(c => c.Code)
            .ToListAsync();

        var rates = await _rateService.GetCurrentRatesAsync();

        var rateByCurrencyId = rates.ToDictionary(r => r.CurrencyId, r => r);

        var vm = new CurrenciesManageViewModel
        {
            Code = string.Empty,
            Name = string.Empty,
            Symbol = string.Empty,
            BuyMargin = 1.5m,
            SellMargin = 1.5m,
            Items = currencies.Select(c =>
            {
                rateByCurrencyId.TryGetValue(c.Id, out var r);

                return new CurrencyManageItem
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Symbol = c.Symbol,
                    IsActive = c.IsActive,
                    BuyMargin = c.BuyMargin,
                    SellMargin = c.SellMargin,
                    MidRate = r?.MidRate,
                    BuyRate = r?.BuyRate,
                    SellRate = r?.SellRate,
                    IsBase = c.Code == "RUB"
                };
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AddByCode(string code, decimal? buyMargin, decimal? sellMargin)
    {
        code = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code) || code.Length < 2 || code.Length > 6)
        {
            TempData["Error"] = "Введите корректный код валюты (например, USD).";
            return RedirectToAction(nameof(Index));
        }

        // RUB нельзя «добавить заново»
        if (code == "RUB")
        {
            TempData["Success"] = "RUB уже существует.";
            return RedirectToAction(nameof(Index));
        }

        var effectiveBuyMargin = buyMargin ?? 1.5m;
        var effectiveSellMargin = sellMargin ?? 1.5m;

        if (effectiveBuyMargin < 0) effectiveBuyMargin = 0;
        if (effectiveSellMargin < 0) effectiveSellMargin = 0;

        var existing = await _db.Currencies.FirstOrDefaultAsync(c => c.Code == code);
        if (existing == null)
        {
            var maxId = await _db.Currencies.MaxAsync(c => (int?)c.Id) ?? 0;
            var nextId = maxId + 1;

            existing = new Currency
            {
                Id = nextId,
                Code = code,
                Name = code,
                Symbol = code,
                IsActive = true,
                BuyMargin = effectiveBuyMargin,
                SellMargin = effectiveSellMargin
            };

            _db.Currencies.Add(existing);
        }
        else
        {
            existing.IsActive = true;
            existing.BuyMargin = effectiveBuyMargin;
            existing.SellMargin = effectiveSellMargin;
        }

        // Если валюта уже существует — дальше мы хотим взять MidRate ЦБ из API и только потом применить маржу.
        // У ExchangeRateService есть RefreshRatesAsync, но он считает buy/sell уже из маржи.
        // Здесь мы специально подтянем MidRate из API и сохраним ExchangeRate с учётом текущей маржи.
        // Проще: прямо вызовем RefreshRatesAsync FORCE, чтобы записались ExchangeRates по новой валюте.
        // Сначала сохраняем валюту/маржу.
        await _db.SaveChangesAsync();

        // Затем берём курс ЦБ (MidRate) из API и сразу записываем ExchangeRate,
        // но уже с учётом маржи пункта обмена.
        var mid = await _rateService.GetMidRateFromApiAsync(code);

        if (mid.HasValue)
        {
            var effectiveBuyRate = mid.Value * (1m - effectiveBuyMargin / 100m);
            var effectiveSellRate = mid.Value * (1m + effectiveSellMargin / 100m);

            _db.ExchangeRates.Add(new ExchangeRate
            {
                CurrencyId = existing.Id,
                BaseCurrencyCode = "RUB",
                MidRate = Math.Round(mid.Value, 4),
                BuyRate = Math.Round(effectiveBuyRate, 4),
                SellRate = Math.Round(effectiveSellRate, 4),
                FetchedAt = DateTime.UtcNow,
                Source = "open.er-api.com"
            });

            await _db.SaveChangesAsync();
        }

        TempData["Success"] = $"Валюта {code} добавлена.";
        return RedirectToAction("Index", "Currencies");
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(int id)
    {
        var cur = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == id);
        if (cur == null)
        {
            TempData["Error"] = "Валюта не найдена.";
            return RedirectToAction(nameof(Index));
        }

        if (cur.Code == "RUB")
        {
            TempData["Error"] = "RUB нельзя отключить.";
            return RedirectToAction(nameof(Index));
        }

        cur.IsActive = false;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Валюта {cur.Code} отключена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var cur = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == id);
        if (cur == null)
        {
            TempData["Error"] = "Валюта не найдена.";
            return RedirectToAction(nameof(Index));
        }

        if (cur.Code == "RUB")
        {
            TempData["Error"] = "RUB нельзя удалить.";
            return RedirectToAction(nameof(Index));
        }

        // Если есть операции обмена — удаление невозможно
        var txCount = await _db.Transactions.CountAsync(t => t.FromCurrencyId == id || t.ToCurrencyId == id);
        if (txCount > 0)
        {
            TempData["Error"] = $"Нельзя удалить валюту {cur.Code}: есть связанные операции.";
            return RedirectToAction(nameof(Index));
        }

        _db.Currencies.Remove(cur);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Валюта {cur.Code} удалена.";
        return RedirectToAction(nameof(Index));
    }
}
