using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderMgmt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "owner_user_id",
                table: "quotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE quotations
                SET owner_user_id = COALESCE(
                    created_by,
                    (SELECT id FROM users WHERE username = 'admin' LIMIT 1)
                )
                WHERE owner_user_id IS NULL;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM quotations WHERE owner_user_id IS NULL) THEN
                        RAISE EXCEPTION 'AddQuotationOwner backfill failed: admin user missing or quotations.created_by null and no admin to fallback';
                    END IF;
                END$$;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_user_id",
                table: "quotations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotations_owner_status_date",
                table: "quotations",
                columns: new[] { "owner_user_id", "is_deleted", "quotation_date" });

            migrationBuilder.AddForeignKey(
                name: "fk_quotations_users_owner_user_id",
                table: "quotations",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_quotations_users_owner_user_id",
                table: "quotations");

            migrationBuilder.DropIndex(
                name: "ix_quotations_owner_status_date",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "quotations");
        }
    }
}
