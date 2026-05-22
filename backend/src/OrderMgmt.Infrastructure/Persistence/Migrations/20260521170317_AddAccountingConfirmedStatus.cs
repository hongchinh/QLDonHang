using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderMgmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingConfirmedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "accounting_confirmed_at",
                table: "quotations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "accounting_confirmed_by_user_id",
                table: "quotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "quotation_system_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    revenue_reporting_date_field = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotation_system_settings", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "quotation_system_settings",
                columns: new[] { "id", "revenue_reporting_date_field", "updated_at", "updated_by" },
                values: new object[] { 1, "QuotationDate", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quotation_system_settings");

            migrationBuilder.DropColumn(
                name: "accounting_confirmed_at",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "accounting_confirmed_by_user_id",
                table: "quotations");
        }
    }
}
