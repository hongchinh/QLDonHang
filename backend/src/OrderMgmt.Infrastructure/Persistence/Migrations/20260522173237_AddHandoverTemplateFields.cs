using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderMgmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverTemplateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "handover_no_price_template_file_name",
                table: "user_quotation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handover_no_price_template_original_name",
                table: "user_quotation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "handover_no_price_template_uploaded_at",
                table: "user_quotation_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handover_with_price_template_file_name",
                table: "user_quotation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handover_with_price_template_original_name",
                table: "user_quotation_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "handover_with_price_template_uploaded_at",
                table: "user_quotation_settings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "handover_no_price_template_file_name",
                table: "user_quotation_settings");

            migrationBuilder.DropColumn(
                name: "handover_no_price_template_original_name",
                table: "user_quotation_settings");

            migrationBuilder.DropColumn(
                name: "handover_no_price_template_uploaded_at",
                table: "user_quotation_settings");

            migrationBuilder.DropColumn(
                name: "handover_with_price_template_file_name",
                table: "user_quotation_settings");

            migrationBuilder.DropColumn(
                name: "handover_with_price_template_original_name",
                table: "user_quotation_settings");

            migrationBuilder.DropColumn(
                name: "handover_with_price_template_uploaded_at",
                table: "user_quotation_settings");
        }
    }
}
