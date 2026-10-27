using Microsoft.EntityFrameworkCore;
using CurrencyExchange.Models;

namespace CurrencyExchange.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Currency> Currencies { get; set; }
    public DbSet<ExchangeTransaction> Transactions { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed popular currencies (Id=7 зарезервирован для RUB — базовая валюта)
        modelBuilder.Entity<Currency>().HasData(
            new Currency { Id = 1, Code = "USD", Name = "Доллар США",       Symbol = "$",  BuyMargin = 1.5m, SellMargin = 1.5m },
            new Currency { Id = 2, Code = "EUR", Name = "Евро",             Symbol = "€",  BuyMargin = 1.5m, SellMargin = 1.5m },
            new Currency { Id = 3, Code = "GBP", Name = "Фунт стерлингов", Symbol = "£",  BuyMargin = 2.0m, SellMargin = 2.0m },
            new Currency { Id = 4, Code = "CNY", Name = "Юань",            Symbol = "¥",  BuyMargin = 2.0m, SellMargin = 2.0m },
            new Currency { Id = 5, Code = "TRY", Name = "Турецкая лира",   Symbol = "₺",  BuyMargin = 2.5m, SellMargin = 2.5m },
            new Currency { Id = 6, Code = "AED", Name = "Дирхам ОАЭ",      Symbol = "د.إ",BuyMargin = 2.0m, SellMargin = 2.0m },
            new Currency { Id = 7, Code = "RUB", Name = "Российский рубль", Symbol = "₽", BuyMargin = 0m,   SellMargin = 0m   }
        );

        // У Currency две связи с ExchangeTransaction (From / To) — без явной
        // конфигурации EF не знает, к какой привязать Currency.Transactions
        modelBuilder.Entity<ExchangeTransaction>()
            .HasOne(t => t.FromCurrency)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.FromCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExchangeTransaction>()
            .HasOne(t => t.ToCurrency)
            .WithMany()
            .HasForeignKey(t => t.ToCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExchangeRate>()
            .HasOne(r => r.Currency)
            .WithMany(c => c.Rates)
            .HasForeignKey(r => r.CurrencyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Decimal precision
        modelBuilder.Entity<ExchangeTransaction>()
            .Property(t => t.FromAmount).HasPrecision(18, 4);
        modelBuilder.Entity<ExchangeTransaction>()
            .Property(t => t.ToAmount).HasPrecision(18, 4);
        modelBuilder.Entity<ExchangeTransaction>()
            .Property(t => t.Rate).HasPrecision(18, 6);
        modelBuilder.Entity<ExchangeTransaction>()
            .Property(t => t.Profit).HasPrecision(18, 4);

        modelBuilder.Entity<ExchangeRate>()
            .Property(r => r.MidRate).HasPrecision(18, 6);
        modelBuilder.Entity<ExchangeRate>()
            .Property(r => r.BuyRate).HasPrecision(18, 6);
        modelBuilder.Entity<ExchangeRate>()
            .Property(r => r.SellRate).HasPrecision(18, 6);

        modelBuilder.Entity<Currency>()
            .Property(c => c.BuyMargin).HasPrecision(5, 2);
        modelBuilder.Entity<Currency>()
            .Property(c => c.SellMargin).HasPrecision(5, 2);
    }
}
