using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentRegionFields : Migration
    {
       protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
            name: "OriginRegion",
            table: "Shipments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

            migrationBuilder.AddColumn<string>(
            name: "DestinationRegion",
            table: "Shipments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

            migrationBuilder.Sql(
            """
            UPDATE "Shipments"
            SET "OriginRegion" = TRIM(regexp_replace("OriginAddress", '^.*,\s*', ''))
            WHERE "OriginRegion" IS NULL;
            """);

            migrationBuilder.Sql(
            """
            UPDATE "Shipments"
            SET "DestinationRegion" = TRIM(regexp_replace("DestinationAddress", '^.*,\s*', ''))
            WHERE "DestinationRegion" IS NULL;
            """);

            migrationBuilder.AlterColumn<string>(
            name: "OriginRegion",
            table: "Shipments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(128)",
            oldNullable: true);

            migrationBuilder.AlterColumn<string>(
            name: "DestinationRegion",
            table: "Shipments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(128)",
            oldNullable: true);

            migrationBuilder.CreateIndex(
            name: "IX_Shipments_Carrier_Mode_OriginRegion_DestinationRegion",
            table: "Shipments",
            columns: new[] { "Carrier", "Mode", "OriginRegion", "DestinationRegion" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
            name: "IX_Shipments_Carrier_Mode_OriginRegion_DestinationRegion",
            table: "Shipments");

            migrationBuilder.DropColumn(name: "OriginRegion", table: "Shipments");
            migrationBuilder.DropColumn(name: "DestinationRegion", table: "Shipments");
        }
    }
}
