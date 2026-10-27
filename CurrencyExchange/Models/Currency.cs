namespace CurrencyExchange.Models;

public class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;  // USD, EUR, etc.
    public string Name { get; set; } = string.Empty;  // Доллар США
    public string Symbol { get; set; } = string.Empty; // $
    public bool IsActive { get; set; } = true;

    // Наценка пункта обмена (в %)
    public decimal BuyMargin { get; set; } = 1.5m;
    public decimal SellMargin { get; set; } = 1.5m;

    public ICollection<ExchangeTransaction> Transactions { get; set; } = new List<ExchangeTransaction>();
    public ICollection<ExchangeRate> Rates { get; set; } = new List<ExchangeRate>();
}
