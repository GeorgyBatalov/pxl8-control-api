using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pxl8.ControlApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "billing_periods",
                columns: table => new
                {
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    bandwidth_consumed_bytes = table.Column<long>(type: "bigint", nullable: false),
                    transforms_consumed = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_periods", x => x.period_id);
                });

            migrationBuilder.CreateTable(
                name: "budget_leases",
                columns: table => new
                {
                    lease_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dataplane_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bandwidth_granted_bytes = table.Column<long>(type: "bigint", nullable: false),
                    transforms_granted = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_leases", x => x.lease_id);
                });

            migrationBuilder.CreateTable(
                name: "usage_reports",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dataplane_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bandwidth_used_bytes = table.Column<long>(type: "bigint", nullable: false),
                    transforms_used = table.Column<int>(type: "integer", nullable: false),
                    reported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_reports", x => x.report_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_billing_periods_tenant_period_key",
                table: "billing_periods",
                columns: new[] { "tenant_id", "period_key" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_leases_active_unique",
                table: "budget_leases",
                columns: new[] { "tenant_id", "period_id", "dataplane_id" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_budget_leases_request_id",
                table: "budget_leases",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_leases_tenant_period_dataplane_status",
                table: "budget_leases",
                columns: new[] { "tenant_id", "period_id", "dataplane_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_usage_reports_report_id",
                table: "usage_reports",
                column: "report_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usage_reports_tenant_period_received",
                table: "usage_reports",
                columns: new[] { "tenant_id", "period_id", "received_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_periods");

            migrationBuilder.DropTable(
                name: "budget_leases");

            migrationBuilder.DropTable(
                name: "usage_reports");
        }
    }
}
