using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace CurrencyExchange.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    BuyMargin = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    SellMargin = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseCurrencyCode = table.Column<string>(type: "TEXT", nullable: false),
                    MidRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    BuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    SellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FromCurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ToCurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Profit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Currencies_FromCurrencyId",
                        column: x => x.FromCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Currencies_ToCurrencyId",
                        column: x => x.ToCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "BuyMargin", "Code", "IsActive", "Name", "SellMargin", "Symbol" },
                values: new object[,]
                {
                    { 1, 1.5m, "USD", true, "Доллар США",        1.5m, "$"    },
                    { 2, 1.5m, "EUR", true, "Евро",              1.5m, "€"    },
                    { 3, 2.0m, "GBP", true, "Фунт стерлингов",   2.0m, "£"    },
                    { 4, 2.0m, "CNY", true, "Юань",              2.0m, "¥"    },
                    { 5, 2.5m, "TRY", true, "Турецкая лира",     2.5m, "₺"    },
                    { 6, 2.0m, "AED", true, "Дирхам ОАЭ",        2.0m, "د.إ"  },
                    { 7, 0.0m, "RUB", true, "Российский рубль",  0.0m, "₽"    }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyId",
                table: "ExchangeRates",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FromCurrencyId",
                table: "Transactions",
                column: "FromCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ToCurrencyId",
                table: "Transactions",
                column: "ToCurrencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExchangeRates");
            migrationBuilder.DropTable(name: "Transactions");
            migrationBuilder.DropTable(name: "Currencies");
        }
    }
}
