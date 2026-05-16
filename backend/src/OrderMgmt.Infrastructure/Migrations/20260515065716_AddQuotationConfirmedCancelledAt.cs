using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationConfirmedCancelledAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "quotations",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                table: "quotations",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "confirmed_by_user_id",
                table: "quotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotations_owner_status_confirmed_at",
                table: "quotations",
                columns: new[] { "owner_user_id", "is_deleted", "status", "confirmed_at" });

            migrationBuilder.Sql(@"
                UPDATE quotations
                SET confirmed_at = updated_at
                WHERE status = 3 AND confirmed_at IS NULL;

                UPDATE quotations
                SET cancelled_at = updated_at
                WHERE status = 9 AND cancelled_at IS NULL;
            ");

            // Migrate legacy ConvertedToOrder (status=4) rows into Confirmed (status=3),
            // backfilling confirmed_at from updated_at if not already set.
            migrationBuilder.Sql(@"
                UPDATE quotations
                SET status = 3, confirmed_at = COALESCE(confirmed_at, updated_at)
                WHERE status = 4;
            ");

            // Drop role_permission rows for removed permission codes so role checks no longer grant them.
            migrationBuilder.Sql(@"
                DELETE FROM role_permissions
                WHERE permission_id IN (
                    SELECT id FROM permissions
                    WHERE code = 'quotations.convert' OR code LIKE 'orders.%'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_quotations_owner_status_confirmed_at",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "confirmed_by_user_id",
                table: "quotations");
        }
    }
}
