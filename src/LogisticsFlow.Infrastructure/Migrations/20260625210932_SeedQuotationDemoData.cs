using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedQuotationDemoData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "AccountId", "CompanyName", "CreatedAtUtc" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "ACC-DEMO-001", "Acme Freight Ltd", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "RateAgreements",
                columns: new[] { "Id", "ClientId", "DestinationAddress", "EffectiveFrom", "EffectiveTo", "NegotiatedRate", "OriginAddress", "SpecialHandlingInstructions" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new Guid("11111111-1111-1111-1111-111111111111"), "45 Port Ave, Apapa", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1500.00m, "123 Dock Rd, Lagos", "Fragile - keep upright" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RateAgreements",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
