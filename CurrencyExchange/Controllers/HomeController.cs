using CurrencyExchange.Data;
using CurrencyExchange.Models;
using CurrencyExchange.Models.ViewModels;
using CurrencyExchange.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurrencyExchange.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IExchangeRateService _rateService;

    public HomeController(ApplicationDbContext db, IExchangeRateService rateService)
    {
        _db = db;
        _rateService = rateService;
    }

    public async Task<IActionResult> Index()
    {
        var rates = await _rateService.GetCurrentRatesAsync(); // не force

        var currencies = await _db.Currencies.Where(c => c.IsActive).ToListAsync();

        var vm = new RatesViewModel
        {
            Currencies  = currencies,
            Rates       = rates,
            LastUpdated = rates.Any() ? rates.Max(r => r.FetchedAt) : null
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> RefreshRates()
    {
        await _rateService.RefreshRatesAsync();
        TempData["Success"] = "Курсы валют обновлены.";
        return RedirectToAction(nameof(Index));
    }
}
