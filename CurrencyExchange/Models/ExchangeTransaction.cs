namespace CurrencyExchange.Models;

public class ExchangeTransaction
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Какую валюту клиент ОТДАЁТ
    public int FromCurrencyId { get; set; }
    public Currency FromCurrency { get; set; } = null!;
    public decimal FromAmount { get; set; }

    // Какую валюту клиент ПОЛУЧАЕТ
    public int ToCurrencyId { get; set; }
    public Currency ToCurrency { get; set; } = null!;
    public decimal ToAmount { get; set; }

    // Курс по которому проведена операция
    public decimal Rate { get; set; }

    // Прибыль пункта обмена
    public decimal Profit { get; set; }

    public TransactionType Type { get; set; }

    public string? Note { get; set; }
}

public enum TransactionType
{
    Buy,   // Клиент продаёт иностранную валюту (пункт покупает)
    Sell   // Клиент покупает иностранную валюту (пункт продаёт)
}
