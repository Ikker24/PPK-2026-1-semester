using CurrencyExchange.Data;
using CurrencyExchange.Models;
using CurrencyExchange.Models.ViewModels;
using CurrencyExchange.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurrencyExchange.Controllers;

public class ExchangeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IExchangeService _exchangeService;
    private readonly IExchangeRateService _rateService;

    public ExchangeController(
        ApplicationDbContext db,
        IExchangeService exchangeService,
        IExchangeRateService rateService)
    {
        _db = db;
        _exchangeService = exchangeService;
        _rateService = rateService;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Calculate(ExchangeViewModel form)
    {
        var vm = await BuildViewModelAsync();
        vm.FromCurrencyId = form.FromCurrencyId;
        vm.ToCurrencyId   = form.ToCurrencyId;
        vm.Amount         = form.Amount;
        vm.Type           = form.Type;

        if (form.Amount <= 0)
        {
            vm.Message = "Введите корректную сумму.";
            return View("Index", vm);
        }

        try
        {
            var (result, rate, profit) = await _exchangeService.CalculateAsync(
                form.FromCurrencyId, form.ToCurrencyId, form.Amount, form.Type);

            vm.ResultAmount = result;
            vm.AppliedRate  = rate;
        }
        catch (Exception ex)
        {
            vm.Message = ex.Message;
        }

        return View("Index", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Execute(ExchangeViewModel form)
    {
        if (form.Amount <= 0)
        {
            TempData["Error"] = "Введите корректную сумму.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var tx = await _exchangeService.ExecuteAsync(
                form.FromCurrencyId, form.ToCurrencyId, form.Amount, form.Type);

            TempData["Success"] = $"Операция выполнена! Клиент получает {tx.ToAmount:N2} единиц.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<ExchangeViewModel> BuildViewModelAsync()
    {
        // Загружаем все активные валюты включая RUB (Id=7)
        var currencies = await _db.Currencies.Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();

        // RUB ставим первым для удобства
        var rub = currencies.FirstOrDefault(c => c.Code == "RUB");
        if (rub != null)
        {
            currencies.Remove(rub);
            currencies.Insert(0, rub);
        }

        var rates = await _rateService.GetCurrentRatesAsync();


        return new ExchangeViewModel
        {
            Currencies   = currencies,
            CurrentRates = rates
        };
    }
}
