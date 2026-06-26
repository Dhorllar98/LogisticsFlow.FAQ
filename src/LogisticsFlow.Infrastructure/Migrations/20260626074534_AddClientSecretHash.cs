using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientSecretHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecretHash",
                table: "Clients",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "SecretHash",
                value: "$2a$11$qHMa4rcIKtMtFvqhKX3ryuXsrrNF3d6hNcaJVB6tHPnnZHGxCSdVG");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretHash",
                table: "Clients");
        }
    }
}
