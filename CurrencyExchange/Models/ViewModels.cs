using CurrencyExchange.Models;

namespace CurrencyExchange.Models.ViewModels;

public class ExchangeViewModel
{
    public List<Currency> Currencies { get; set; } = new();
    public List<ExchangeRate> CurrentRates { get; set; } = new();

    // Форма обмена
    public int FromCurrencyId { get; set; }
    public int ToCurrencyId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }

    // Результат расчёта
    public decimal? ResultAmount { get; set; }
    public decimal? AppliedRate { get; set; }
    public string? Message { get; set; }
}

public class HistoryViewModel
{
    public List<ExchangeTransaction> Transactions { get; set; } = new();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public string? FilterCurrency { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class ReportViewModel
{
    public decimal TotalProfit { get; set; }
    public decimal TotalVolume { get; set; }
    public int TotalTransactions { get; set; }
    public int BuyCount { get; set; }
    public int SellCount { get; set; }

    // Топ валюты по объёму
    public List<CurrencyVolume> TopCurrencies { get; set; } = new();

    // Данные для графика (по дням)
    public List<DailyProfit> DailyProfits { get; set; } = new();

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class CurrencyVolume
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public decimal Volume { get; set; }
    public decimal Profit { get; set; }
    public int Count { get; set; }
}

public class DailyProfit
{
    public string Date { get; set; } = string.Empty;
    public decimal Profit { get; set; }
    public int Transactions { get; set; }
}

public class RatesViewModel
{
    public List<Currency> Currencies { get; set; } = new();
    public List<ExchangeRate> Rates { get; set; } = new();
    public DateTime? LastUpdated { get; set; }
}

public class CurrenciesManageViewModel
{
    public List<CurrencyManageItem> Items { get; set; } = new();

    // Форма добавления
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal BuyMargin { get; set; } = 1.5m;
    public decimal SellMargin { get; set; } = 1.5m;
}

public class CurrencyManageItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal BuyMargin { get; set; }
    public decimal SellMargin { get; set; }
    public decimal? MidRate { get; set; }
    public decimal? BuyRate { get; set; }
    public decimal? SellRate { get; set; }
    public bool IsBase { get; set; }
}
