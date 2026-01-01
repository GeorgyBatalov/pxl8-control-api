using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pxl8.ControlApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingPeriodLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "bandwidth_limit",
                table: "billing_periods",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "storage_limit",
                table: "billing_periods",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "transforms_limit",
                table: "billing_periods",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bandwidth_limit",
                table: "billing_periods");

            migrationBuilder.DropColumn(
                name: "storage_limit",
                table: "billing_periods");

            migrationBuilder.DropColumn(
                name: "transforms_limit",
                table: "billing_periods");
        }
    }
}
