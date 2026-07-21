using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoShipmentAndRiskData : Migration
    {
        // RESOLVED: Shipments/TrackingEvents demo data (TRK-DEMO-001,
        // DEMO-LANE-001 through 005) referenced throughout CLAUDE.md,
        // README.md, and docs/deployment.md's smoke tests was never
        // actually captured in any migration - it existed only as manual
        // inserts into a local development database, so production Neon
        // never received it. This migration closes that gap.
        //
        // Seed design matches documented intent rather than reproducing
        // undocumented local drift: exactly 5 delivered shipments on one
        // lane (Carrier=Maersk Line, Mode=Sea, OriginRegion=Lagos,
        // DestinationRegion=Apapa - matching RiskAssessmentService's
        // actual grouping key), landing on the documented minimum sample
        // size of 5 exactly. Transit durations (3/4/5/6/7 days) average
        // to a clean 5.0-day lane average. TRK-DEMO-001 is a separate,
        // deliberately in-transit shipment on the same lane (Departed
        // event only, no Delivered event), for general Tracking/Risk
        // Assessment demo lookups distinct from the lane-average set.
        //
        // Dates are computed relative to DateTime.UtcNow at migration
        // execution time, not hardcoded, so the demo reads as current
        // whenever this migration actually runs - on a fresh database,
        // today, or months from now.
        private static readonly Guid ClientId = new Guid("11111111-1111-1111-1111-111111111111");

        private const string Carrier = "Maersk Line";
        private const string Mode = "Sea";
        private const string OriginAddress = "123 Dock Rd, Lagos";
        private const string DestinationAddress = "45 Port Ave, Apapa";
        private const string OriginRegion = "Lagos";
        private const string DestinationRegion = "Apapa";
        private const string ConsigneeName = "Demo Consignee Ltd";
        private const string ConsigneeAddress = "45 Port Ave, Apapa";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow;

            // TRK-DEMO-001: in-transit, departed 2 days ago, no Delivered
            // event - does not count toward the lane average, and
            // RiskAssessmentService's Delivered-vs-In-Transit logic
            // correctly treats it as still in transit.
            var trkDemoId = new Guid("33333333-3333-3333-3333-333333333001");
            var trkDemoCreated = now.AddDays(-2);

            migrationBuilder.InsertData(
                table: "Shipments",
                columns: new[] { "Id", "TrackingNumber", "ClientId", "Carrier", "Mode", "OriginAddress", "DestinationAddress", "OriginRegion", "DestinationRegion", "ConsigneeName", "ConsigneeAddress", "CreatedAtUtc" },
                values: new object[] { trkDemoId, "TRK-DEMO-001", ClientId, Carrier, Mode, OriginAddress, DestinationAddress, OriginRegion, DestinationRegion, ConsigneeName, ConsigneeAddress, trkDemoCreated });

            migrationBuilder.InsertData(
                table: "TrackingEvents",
                columns: new[] { "Id", "ShipmentId", "MilestoneType", "Location", "TimestampUtc", "Notes" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444001"), trkDemoId, "DepartedOriginFacility", "Lagos Port", trkDemoCreated, null });

            // DEMO-LANE-001 through 005: delivered shipments, transit
            // durations 3/4/5/6/7 days - average exactly 5.0 days,
            // landing precisely on the documented minimum sample size.
            var laneDurations = new (string TrackingNumber, Guid ShipmentId, Guid DepartedEventId, Guid DeliveredEventId, int TransitDays)[]
            {
                ("DEMO-LANE-001", new Guid("33333333-3333-3333-3333-333333333011"), new Guid("44444444-4444-4444-4444-444444444101"), new Guid("44444444-4444-4444-4444-444444444102"), 3),
                ("DEMO-LANE-002", new Guid("33333333-3333-3333-3333-333333333012"), new Guid("44444444-4444-4444-4444-444444444201"), new Guid("44444444-4444-4444-4444-444444444202"), 4),
                ("DEMO-LANE-003", new Guid("33333333-3333-3333-3333-333333333013"), new Guid("44444444-4444-4444-4444-444444444301"), new Guid("44444444-4444-4444-4444-444444444302"), 5),
                ("DEMO-LANE-004", new Guid("33333333-3333-3333-3333-333333333014"), new Guid("44444444-4444-4444-4444-444444444401"), new Guid("44444444-4444-4444-4444-444444444402"), 6),
                ("DEMO-LANE-005", new Guid("33333333-3333-3333-3333-333333333015"), new Guid("44444444-4444-4444-4444-444444444501"), new Guid("44444444-4444-4444-4444-444444444502"), 7),
            };

            foreach (var lane in laneDurations)
            {
                // Departure is set far enough in the past that Delivered
                // (Departure + TransitDays) still lands before "now".
                var departed = now.AddDays(-10);
                var delivered = departed.AddDays(lane.TransitDays);

                migrationBuilder.InsertData(
                    table: "Shipments",
                    columns: new[] { "Id", "TrackingNumber", "ClientId", "Carrier", "Mode", "OriginAddress", "DestinationAddress", "OriginRegion", "DestinationRegion", "ConsigneeName", "ConsigneeAddress", "CreatedAtUtc" },
                    values: new object[] { lane.ShipmentId, lane.TrackingNumber, ClientId, Carrier, Mode, OriginAddress, DestinationAddress, OriginRegion, DestinationRegion, ConsigneeName, ConsigneeAddress, departed });

                migrationBuilder.InsertData(
                    table: "TrackingEvents",
                    columns: new[] { "Id", "ShipmentId", "MilestoneType", "Location", "TimestampUtc", "Notes" },
                    values: new object[] { lane.DepartedEventId, lane.ShipmentId, "DepartedOriginFacility", "Lagos Port", departed, null });

                migrationBuilder.InsertData(
                    table: "TrackingEvents",
                    columns: new[] { "Id", "ShipmentId", "MilestoneType", "Location", "TimestampUtc", "Notes" },
                    values: new object[] { lane.DeliveredEventId, lane.ShipmentId, "Delivered", "Apapa Terminal", delivered, null });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Children (TrackingEvents) before parents (Shipments) - FK
            // constraint is Cascade on delete, so deleting Shipments alone
            // would also work, but explicit deletion of both is clearer
            // and matches this project's stated preference for explicit
            // over implicit behavior.
            var eventIds = new[]
            {
                new Guid("44444444-4444-4444-4444-444444444001"),
                new Guid("44444444-4444-4444-4444-444444444101"),
                new Guid("44444444-4444-4444-4444-444444444102"),
                new Guid("44444444-4444-4444-4444-444444444201"),
                new Guid("44444444-4444-4444-4444-444444444202"),
                new Guid("44444444-4444-4444-4444-444444444301"),
                new Guid("44444444-4444-4444-4444-444444444302"),
                new Guid("44444444-4444-4444-4444-444444444401"),
                new Guid("44444444-4444-4444-4444-444444444402"),
                new Guid("44444444-4444-4444-4444-444444444501"),
                new Guid("44444444-4444-4444-4444-444444444502"),
            };

            foreach (var id in eventIds)
            {
                migrationBuilder.DeleteData(table: "TrackingEvents", keyColumn: "Id", keyValue: id);
            }

            var shipmentIds = new[]
            {
                new Guid("33333333-3333-3333-3333-333333333001"),
                new Guid("33333333-3333-3333-3333-333333333011"),
                new Guid("33333333-3333-3333-3333-333333333012"),
                new Guid("33333333-3333-3333-3333-333333333013"),
                new Guid("33333333-3333-3333-3333-333333333014"),
                new Guid("33333333-3333-3333-3333-333333333015"),
            };

            foreach (var id in shipmentIds)
            {
                migrationBuilder.DeleteData(table: "Shipments", keyColumn: "Id", keyValue: id);
            }
        }
    }
}