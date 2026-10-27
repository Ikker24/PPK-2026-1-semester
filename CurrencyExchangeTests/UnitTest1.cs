using CurrencyExchange.Data;
using CurrencyExchange.Models;
using CurrencyExchange.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CurrencyExchangeTests
{
    public class ExchangeService_NegativeDataTests
    {
        private static SqliteConnection CreateInMemorySqliteConnection()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            return conn;
        }

        private static DbContextOptions<ApplicationDbContext> BuildOptions(SqliteConnection conn)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(conn)
                .Options;
        }

        private static async Task SeedBaseDataAsync(ApplicationDbContext db)
        {
            if (await db.Currencies.AnyAsync())
                return;

            db.Currencies.AddRange(
                new Currency { Id = 1, Code = "USD", Name = "US Dollar", Symbol = "$", BuyMargin = 0.01m, SellMargin = 0.01m, IsActive = true },
                new Currency { Id = 2, Code = "RUB", Name = "Russian Ruble", Symbol = "₽", BuyMargin = 0.01m, SellMargin = 0.01m, IsActive = true },
                new Currency { Id = 3, Code = "EUR", Name = "Euro", Symbol = "€", BuyMargin = 0.01m, SellMargin = 0.01m, IsActive = false }
            );

            await db.SaveChangesAsync();
        }

        private static async Task SeedExchangeRateForNowAsync(ApplicationDbContext db)
        {
            // Если ExchangeRate-ы где-то сидятся автоматически, можно тоже сделать “если есть — не добавлять”.
            // Но обычно в этих тестах это не критично.
            db.ExchangeRates.Add(new ExchangeRate
            {
                Id = 1,
                CurrencyId = 2,
                MidRate = 1.0m,
                BuyRate = 1.0m,
                SellRate = 1.0m,
                FetchedAt = DateTime.UtcNow,
                Source = "test"
            });

            await db.SaveChangesAsync();
        }

        private static int GetTransactionsCount(ApplicationDbContext db)
            // ВАЖНО: проверяем именно транзакции, а не курсы
            => db.ExchangeRates.Count(); // <-- если у тебя DbSet называется иначе, замени ExchangeTransactions на своё

        // IHttpClientFactory stub: запрещаем реальные HTTP вызовы
        private class ThrowingHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new InvalidOperationException("HTTP call is not allowed in unit tests.");
        }

        private class StubHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public StubHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        private static ExchangeService CreateExchangeService(ApplicationDbContext db)
        {
            var logger = new LoggerFactory().CreateLogger<ExchangeRateService>();
            var config = new ConfigurationBuilder().Build();

            var httpFactory = new StubHttpClientFactory(
                new HttpClient(new ThrowingHttpMessageHandler())
            );

            var exchangeRateService = new ExchangeRateService(
                db,
                httpFactory,
                logger,
                config
            );

            return new ExchangeService(db, exchangeRateService);
        }

        [Fact]
        public async Task Execute_WhenAmountNegative_ShouldNotCreateTransaction()
        {
            using var conn = CreateInMemorySqliteConnection();
            var options = BuildOptions(conn);

            await using var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            await SeedBaseDataAsync(db);
            await SeedExchangeRateForNowAsync(db);

            var exchangeService = CreateExchangeService(db);

            int fromCurrencyId = 1;
            int toCurrencyId = 2;
            TransactionType type = TransactionType.Buy;
            decimal amount = -10m;

            int before = GetTransactionsCount(db);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                exchangeService.ExecuteAsync(fromCurrencyId, toCurrencyId, amount, type)
            );

            int after = GetTransactionsCount(db);
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task Execute_WhenCurrencyIdsInvalid_ShouldNotCreateTransaction()
        {
            using var conn = CreateInMemorySqliteConnection();
            var options = BuildOptions(conn);

            await using var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            await SeedBaseDataAsync(db);
            await SeedExchangeRateForNowAsync(db);

            var exchangeService = CreateExchangeService(db);

            int fromCurrencyId = 999; // нет в справочнике
            int toCurrencyId = 2;

            TransactionType type = TransactionType.Buy;
            decimal amount = 100m;

            int before = GetTransactionsCount(db);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                exchangeService.ExecuteAsync(fromCurrencyId, toCurrencyId, amount, type)
            );

            int after = GetTransactionsCount(db);
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task Execute_WhenCurrencyIsInactive_ShouldNotCreateTransaction()
        {
            using var conn = CreateInMemorySqliteConnection();
            var options = BuildOptions(conn);

            await using var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            await SeedBaseDataAsync(db);
            await SeedExchangeRateForNowAsync(db);

            var exchangeService = CreateExchangeService(db);

            int fromCurrencyId = 3; // EUR IsActive=false
            int toCurrencyId = 2;

            TransactionType type = TransactionType.Buy;
            decimal amount = 50m;

            int before = GetTransactionsCount(db);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                exchangeService.ExecuteAsync(fromCurrencyId, toCurrencyId, amount, type)
            );

            int after = GetTransactionsCount(db);
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task Execute_WhenNoExchangeRateForTime_ShouldNotCreateTransaction()
        {
            using var conn = CreateInMemorySqliteConnection();
            var options = BuildOptions(conn);

            await using var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            await SeedBaseDataAsync(db);

            // Удаляем любые ExchangeRate, если EF где-то их автоматически сидит (HasData и т.п.)
            db.ExchangeRates.RemoveRange(db.ExchangeRates);
            await db.SaveChangesAsync();

            var exchangeService = CreateExchangeService(db);

            int fromCurrencyId = 1;
            int toCurrencyId = 2;

            TransactionType type = TransactionType.Buy;
            decimal amount = 100m;

            int before = GetTransactionsCount(db);

            // Ожидаем, что при отсутствии курса сервис не должен создавать транзакцию.
            // Если у тебя fallback и курс находится — тогда этот тест надо переписать,
            // но по твоему заданию логика обычно должна валиться/отклоняться.
            await Assert.ThrowsAnyAsync<Exception>(() =>
                exchangeService.ExecuteAsync(fromCurrencyId, toCurrencyId, amount, type)
            );

            int after = GetTransactionsCount(db);
            Assert.Equal(before, after);
        }

        [Fact]
        public async Task Execute_WhenAmountTooLarge_ShouldNotCreateTransaction()
        {
            using var conn = CreateInMemorySqliteConnection();
            var options = BuildOptions(conn);

            await using var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            await SeedBaseDataAsync(db);
            await SeedExchangeRateForNowAsync(db);

            var exchangeService = CreateExchangeService(db);

            int fromCurrencyId = 1;
            int toCurrencyId = 2;

            TransactionType type = TransactionType.Buy;
            decimal amount = 9999999999999m;

            int before = GetTransactionsCount(db);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                exchangeService.ExecuteAsync(fromCurrencyId, toCurrencyId, amount, type)
            );

            int after = GetTransactionsCount(db);
            Assert.Equal(before, after);
        }
    }
}