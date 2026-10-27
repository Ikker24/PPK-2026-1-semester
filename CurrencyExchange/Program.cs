using CurrencyExchange.Data;
using CurrencyExchange.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// HTTP client для запросов к API курсов
builder.Services.AddHttpClient();

// Наши сервисы
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<IExchangeService, ExchangeService>();

var app = builder.Build();

// Создаём БД и таблицы из модели (seed валют тоже).
// EnsureCreated надёжнее Migrate для этого проекта (без Designer-файлов миграций).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureCreated();

    // Если файл БД уже был, но таблиц нет — пересоздаём
    try
    {
        _ = db.Currencies.Any();
    }
    catch
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
