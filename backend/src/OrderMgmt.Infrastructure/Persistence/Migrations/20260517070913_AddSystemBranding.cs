using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderMgmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_branding",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    logo_full = table.Column<byte[]>(type: "bytea", nullable: true),
                    logo_full_content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    logo_mark = table.Column<byte[]>(type: "bytea", nullable: true),
                    logo_mark_content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_branding", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "system_branding",
                columns: new[] { "id", "logo_full", "logo_full_content_type", "logo_mark", "logo_mark_content_type", "updated_at", "updated_by" },
                values: new object[] { 1, null, null, null, null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_branding");
        }
    }
}
