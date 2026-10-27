using CurrencyExchange.Data;
using CurrencyExchange.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurrencyExchange.Controllers;

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ReportsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo)
    {
        var from = dateFrom ?? DateTime.UtcNow.AddDays(-30);
        var to   = (dateTo  ?? DateTime.UtcNow).Date.AddDays(1); // до конца дня

        var transactions = await _db.Transactions
            .Include(t => t.FromCurrency)
            .Include(t => t.ToCurrency)
            .Where(t => t.CreatedAt >= from && t.CreatedAt < to)
            .ToListAsync();

        var vm = new ReportViewModel
        {
            PeriodStart       = from,
            PeriodEnd         = to.AddDays(-1),
            TotalTransactions = transactions.Count,
            BuyCount          = transactions.Count(t => t.Type == Models.TransactionType.Buy),
            SellCount         = transactions.Count(t => t.Type == Models.TransactionType.Sell),
            TotalProfit       = transactions.Sum(t => t.Profit),
            TotalVolume       = transactions.Sum(t => t.FromAmount),
        };

        // Топ валюты
        vm.TopCurrencies = transactions
            .GroupBy(t => t.FromCurrency.Code == "RUB" ? t.ToCurrency : t.FromCurrency)
            .Select(g => new CurrencyVolume
            {
                CurrencyCode = g.Key.Code,
                CurrencyName = g.Key.Name,
                Volume       = g.Sum(t => t.FromAmount),
                Profit       = g.Sum(t => t.Profit),
                Count        = g.Count()
            })
            .OrderByDescending(c => c.Volume)
            .Take(6)
            .ToList();

        // График по дням
        vm.DailyProfits = transactions
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailyProfit
            {
                Date         = g.Key.ToString("dd.MM"),
                Profit       = g.Sum(t => t.Profit),
                Transactions = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return View(vm);
    }

    public async Task<IActionResult> History(string? currency, DateTime? dateFrom, DateTime? dateTo, int page = 1)
    {
        const int pageSize = 20;

        var query = _db.Transactions
            .Include(t => t.FromCurrency)
            .Include(t => t.ToCurrency)
            .AsQueryable();

        if (!string.IsNullOrEmpty(currency))
            query = query.Where(t => t.FromCurrency.Code == currency || t.ToCurrency.Code == currency);

        if (dateFrom.HasValue)
            query = query.Where(t => t.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(t => t.CreatedAt < dateTo.Value.AddDays(1));

        var total = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new HistoryViewModel
        {
            Transactions   = transactions,
            Page           = page,
            TotalPages     = (int)Math.Ceiling((double)total / pageSize),
            FilterCurrency = currency,
            DateFrom       = dateFrom,
            DateTo         = dateTo
        };

        return View(vm);
    }
}
