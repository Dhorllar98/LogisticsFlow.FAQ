using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SpecialHandlingInstructions = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    NegotiatedRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateAgreements_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "AccountId", "CompanyName", "CreatedAtUtc", "SecretHash" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "ACC-DEMO-001", "Acme Freight Ltd", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$qHMa4rcIKtMtFvqhKX3ryuXsrrNF3d6hNcaJVB6tHPnnZHGxCSdVG" });

            migrationBuilder.InsertData(
                table: "RateAgreements",
                columns: new[] { "Id", "ClientId", "DestinationAddress", "EffectiveFrom", "EffectiveTo", "NegotiatedRate", "OriginAddress", "SpecialHandlingInstructions" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new Guid("11111111-1111-1111-1111-111111111111"), "45 Port Ave, Apapa", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1500.00m, "123 Dock Rd, Lagos", "Fragile - keep upright" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_AccountId",
                table: "Clients",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RateAgreements_ClientId_EffectiveFrom",
                table: "RateAgreements",
                columns: new[] { "ClientId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateAgreements");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
