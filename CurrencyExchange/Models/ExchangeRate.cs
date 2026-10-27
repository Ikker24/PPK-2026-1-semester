namespace CurrencyExchange.Models;

public class ExchangeRate
{
    public int Id { get; set; }
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    // Базовая валюта (RUB)
    public string BaseCurrencyCode { get; set; } = "RUB";

    // Курс ЦБ / биржевой
    public decimal MidRate { get; set; }

    // Курсы пункта обмена (с учётом маржи)
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "API";
}
