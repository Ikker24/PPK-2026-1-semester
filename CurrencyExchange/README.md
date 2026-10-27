# ForEx Pro — Система управления пунктом обмена валют

## Стек
- **ASP.NET Core 8** MVC
- **Entity Framework Core 8** + SQLite (база создаётся автоматически)
- **Bootstrap 5.3** + **Chart.js 4** (CDN, интернет нужен только для UI)
- Курсы валют: [open.er-api.com](https://open.er-api.com) (бесплатно, без ключа)

## Как запустить

### Требования
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Visual Studio 2022 / JetBrains Rider / VS Code

### Запуск через Visual Studio
1. Открыть `CurrencyExchange.csproj`
2. Нажать **F5** (или Ctrl+F5)
3. База данных создастся автоматически при первом запуске
4. На главной странице нажать **«Обновить курсы»** — загрузятся актуальные данные

### Запуск через командную строку
```bash
cd CurrencyExchange
dotnet run
```
Приложение откроется на `http://localhost:5000`

## Функционал

| Раздел | Описание |
|--------|----------|
| **Курсы** | Таблица текущих курсов покупки/продажи, кнопка обновления с API |
| **Обмен** | Калькулятор + проведение операции (RUB↔иностранная, кросс-курс) |
| **История** | Журнал всех транзакций с фильтрацией по валюте и дате |
| **Отчёты** | KPI-карточки, график прибыли по дням (Chart.js), топ валют |

## Поддерживаемые валюты
USD, EUR, GBP, CNY, TRY, AED (можно добавить любые в `ApplicationDbContext.cs`)

## Структура проекта
```
CurrencyExchange/
├── Controllers/
│   ├── HomeController.cs       # Главная + обновление курсов
│   ├── ExchangeController.cs   # Расчёт и проведение обмена
│   └── ReportsController.cs    # История и отчёты
├── Models/
│   ├── Currency.cs
│   ├── ExchangeTransaction.cs
│   ├── ExchangeRate.cs
│   └── ViewModels.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Services/
│   ├── ExchangeRateService.cs  # Получение курсов с API
│   └── ExchangeService.cs      # Бизнес-логика обмена
├── Views/
│   ├── Home/Index.cshtml
│   ├── Exchange/Index.cshtml
│   └── Reports/{Index,History}.cshtml
├── Migrations/                 # EF Core миграции (готовы)
├── wwwroot/css/site.css
├── Program.cs
└── appsettings.json
```
