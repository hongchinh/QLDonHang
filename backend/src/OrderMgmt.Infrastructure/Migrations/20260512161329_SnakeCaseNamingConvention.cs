using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseNamingConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customer_addresses_customers_CustomerId",
                table: "customer_addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_products_product_groups_ProductGroupId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_products_units_UnitId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_role_permissions_permissions_PermissionId",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_role_permissions_roles_RoleId",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_roles_RoleId",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_users_UserId",
                table: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Username",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_roles",
                table: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_units",
                table: "units");

            migrationBuilder.DropIndex(
                name: "IX_units_Code",
                table: "units");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_Code",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_role_permissions",
                table: "role_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_products",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_Code",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_product_groups",
                table: "product_groups");

            migrationBuilder.DropIndex(
                name: "IX_product_groups_Code",
                table: "product_groups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permissions",
                table: "permissions");

            migrationBuilder.DropIndex(
                name: "IX_permissions_Code",
                table: "permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customers",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_Code",
                table: "customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customer_addresses",
                table: "customer_addresses");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "users",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "users",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "users",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "users",
                newName: "last_login_at");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "users",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "users",
                newName: "full_name");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "users",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "users",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "users",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "user_roles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "user_roles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                newName: "ix_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "units",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "units",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "units",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "units",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "units",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "units",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "units",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "units",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "units",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "units",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "units",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "roles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "roles",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "roles",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "roles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "roles",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "roles",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "IsSystem",
                table: "roles",
                newName: "is_system");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "roles",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "roles",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "roles",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "roles",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "roles",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "PermissionId",
                table: "role_permissions",
                newName: "permission_id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "role_permissions",
                newName: "role_id");

            migrationBuilder.RenameIndex(
                name: "IX_role_permissions_PermissionId",
                table: "role_permissions",
                newName: "ix_role_permissions_permission_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "refresh_tokens",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "refresh_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "refresh_tokens",
                newName: "user_agent");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "refresh_tokens",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "refresh_tokens",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "refresh_tokens",
                newName: "token_hash");

            migrationBuilder.RenameColumn(
                name: "RevokedReason",
                table: "refresh_tokens",
                newName: "revoked_reason");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                table: "refresh_tokens",
                newName: "revoked_at");

            migrationBuilder.RenameColumn(
                name: "ReplacedByTokenHash",
                table: "refresh_tokens",
                newName: "replaced_by_token_hash");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "refresh_tokens",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "refresh_tokens",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "refresh_tokens",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "refresh_tokens",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedFromIp",
                table: "refresh_tokens",
                newName: "created_from_ip");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "refresh_tokens",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "refresh_tokens",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAt",
                table: "refresh_tokens",
                newName: "ix_refresh_tokens_user_id_expires_at");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                newName: "ix_refresh_tokens_token_hash");

            migrationBuilder.RenameColumn(
                name: "Width",
                table: "products",
                newName: "width");

            migrationBuilder.RenameColumn(
                name: "Thickness",
                table: "products",
                newName: "thickness");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "products",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Specification",
                table: "products",
                newName: "specification");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "products",
                newName: "note");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "products",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Length",
                table: "products",
                newName: "length");

            migrationBuilder.RenameColumn(
                name: "Density",
                table: "products",
                newName: "density");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "products",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "products",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "products",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "products",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "UnitId",
                table: "products",
                newName: "unit_id");

            migrationBuilder.RenameColumn(
                name: "ProductGroupId",
                table: "products",
                newName: "product_group_id");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "products",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "products",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "products",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DefaultTaxRate",
                table: "products",
                newName: "default_tax_rate");

            migrationBuilder.RenameColumn(
                name: "DefaultPrice",
                table: "products",
                newName: "default_price");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "products",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "products",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CostPrice",
                table: "products",
                newName: "cost_price");

            migrationBuilder.RenameIndex(
                name: "IX_products_UnitId",
                table: "products",
                newName: "ix_products_unit_id");

            migrationBuilder.RenameIndex(
                name: "IX_products_ProductGroupId",
                table: "products",
                newName: "ix_products_product_group_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "product_groups",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "product_groups",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "product_groups",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "product_groups",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "product_groups",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "product_groups",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "product_groups",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "product_groups",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "product_groups",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "product_groups",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "product_groups",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "product_groups",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "product_groups",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "permissions",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Module",
                table: "permissions",
                newName: "module");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "permissions",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "permissions",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "permissions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "permissions",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "permissions",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "permissions",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "permissions",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "permissions",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "permissions",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "permissions",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "customers",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "customers",
                newName: "note");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "customers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Group",
                table: "customers",
                newName: "group");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "customers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "customers",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "customers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "customers",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "customers",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TaxCode",
                table: "customers",
                newName: "tax_code");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "customers",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "customers",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "customers",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "customers",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DefaultShippingAddress",
                table: "customers",
                newName: "default_shipping_address");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "customers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "customers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContactPerson",
                table: "customers",
                newName: "contact_person");

            migrationBuilder.RenameColumn(
                name: "CompanyAddress",
                table: "customers",
                newName: "company_address");

            migrationBuilder.RenameIndex(
                name: "IX_customers_Name",
                table: "customers",
                newName: "ix_customers_name");

            migrationBuilder.RenameIndex(
                name: "IX_customers_TaxCode",
                table: "customers",
                newName: "ix_customers_tax_code");

            migrationBuilder.RenameIndex(
                name: "IX_customers_PhoneNumber",
                table: "customers",
                newName: "ix_customers_phone_number");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "customer_addresses",
                newName: "note");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "customer_addresses",
                newName: "label");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "customer_addresses",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "customer_addresses",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "customer_addresses",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "customer_addresses",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RecipientPhone",
                table: "customer_addresses",
                newName: "recipient_phone");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "customer_addresses",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "IsDefault",
                table: "customer_addresses",
                newName: "is_default");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                table: "customer_addresses",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "customer_addresses",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "DefaultRecipient",
                table: "customer_addresses",
                newName: "default_recipient");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "customer_addresses",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "customer_addresses",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "customer_addresses",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_customer_addresses_CustomerId",
                table: "customer_addresses",
                newName: "ix_customer_addresses_customer_id");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_roles",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_units",
                table: "units",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_roles",
                table: "roles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_refresh_tokens",
                table: "refresh_tokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_products",
                table: "products",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_product_groups",
                table: "product_groups",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_permissions",
                table: "permissions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_customers",
                table: "customers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_customer_addresses",
                table: "customer_addresses",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_units_code",
                table: "units",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_roles_code",
                table: "roles",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_products_code",
                table: "products",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_product_groups_code",
                table: "product_groups",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_customers_code",
                table: "customers",
                column: "code",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_addresses_customers_customer_id",
                table: "customer_addresses",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_product_groups_product_group_id",
                table: "products",
                column: "product_group_id",
                principalTable: "product_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_products_units_unit_id",
                table: "products",
                column: "unit_id",
                principalTable: "units",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_users_user_id",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_role_permissions_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_permissions_roles_role_id",
                table: "role_permissions",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_roles_role_id",
                table: "user_roles",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_users_user_id",
                table: "user_roles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_addresses_customers_customer_id",
                table: "customer_addresses");

            migrationBuilder.DropForeignKey(
                name: "fk_products_product_groups_product_group_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_units_unit_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_users_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_role_permissions_permissions_permission_id",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_role_permissions_roles_role_id",
                table: "role_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_user_roles_roles_role_id",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_user_roles_users_user_id",
                table: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_username",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_roles",
                table: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_units",
                table: "units");

            migrationBuilder.DropIndex(
                name: "ix_units_code",
                table: "units");

            migrationBuilder.DropPrimaryKey(
                name: "pk_roles",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "ix_roles_code",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_role_permissions",
                table: "role_permissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_products",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_code",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "pk_product_groups",
                table: "product_groups");

            migrationBuilder.DropIndex(
                name: "ix_product_groups_code",
                table: "product_groups");

            migrationBuilder.DropPrimaryKey(
                name: "pk_permissions",
                table: "permissions");

            migrationBuilder.DropIndex(
                name: "ix_permissions_code",
                table: "permissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_customers",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_code",
                table: "customers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_customer_addresses",
                table: "customer_addresses");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "users",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "users",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "users",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "last_login_at",
                table: "users",
                newName: "LastLoginAt");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "users",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "users",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "users",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "users",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "users",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "user_roles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "user_roles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                newName: "IX_user_roles_RoleId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "units",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "units",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "units",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "units",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "units",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "units",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "units",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "units",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "units",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "units",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "units",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "roles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "roles",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "roles",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "roles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "roles",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "roles",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "is_system",
                table: "roles",
                newName: "IsSystem");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "roles",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "roles",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "roles",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "roles",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "roles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "permission_id",
                table: "role_permissions",
                newName: "PermissionId");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "role_permissions",
                newName: "RoleId");

            migrationBuilder.RenameIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                newName: "IX_role_permissions_PermissionId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "refresh_tokens",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "refresh_tokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "user_agent",
                table: "refresh_tokens",
                newName: "UserAgent");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "refresh_tokens",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "refresh_tokens",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "token_hash",
                table: "refresh_tokens",
                newName: "TokenHash");

            migrationBuilder.RenameColumn(
                name: "revoked_reason",
                table: "refresh_tokens",
                newName: "RevokedReason");

            migrationBuilder.RenameColumn(
                name: "revoked_at",
                table: "refresh_tokens",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "replaced_by_token_hash",
                table: "refresh_tokens",
                newName: "ReplacedByTokenHash");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "refresh_tokens",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "refresh_tokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "refresh_tokens",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "refresh_tokens",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_from_ip",
                table: "refresh_tokens",
                newName: "CreatedFromIp");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "refresh_tokens",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "refresh_tokens",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_refresh_tokens_user_id_expires_at",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_UserId_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_TokenHash");

            migrationBuilder.RenameColumn(
                name: "width",
                table: "products",
                newName: "Width");

            migrationBuilder.RenameColumn(
                name: "thickness",
                table: "products",
                newName: "Thickness");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "products",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "specification",
                table: "products",
                newName: "Specification");

            migrationBuilder.RenameColumn(
                name: "note",
                table: "products",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "products",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "length",
                table: "products",
                newName: "Length");

            migrationBuilder.RenameColumn(
                name: "density",
                table: "products",
                newName: "Density");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "products",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "products",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "products",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "products",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "unit_id",
                table: "products",
                newName: "UnitId");

            migrationBuilder.RenameColumn(
                name: "product_group_id",
                table: "products",
                newName: "ProductGroupId");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "products",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "products",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "products",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "default_tax_rate",
                table: "products",
                newName: "DefaultTaxRate");

            migrationBuilder.RenameColumn(
                name: "default_price",
                table: "products",
                newName: "DefaultPrice");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "products",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "products",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cost_price",
                table: "products",
                newName: "CostPrice");

            migrationBuilder.RenameIndex(
                name: "ix_products_unit_id",
                table: "products",
                newName: "IX_products_UnitId");

            migrationBuilder.RenameIndex(
                name: "ix_products_product_group_id",
                table: "products",
                newName: "IX_products_ProductGroupId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "product_groups",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "product_groups",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "product_groups",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "product_groups",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "product_groups",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "product_groups",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "product_groups",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "product_groups",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "product_groups",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "product_groups",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "product_groups",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "product_groups",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "product_groups",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "permissions",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "module",
                table: "permissions",
                newName: "Module");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "permissions",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "permissions",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "permissions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "permissions",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "permissions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "permissions",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "permissions",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "permissions",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "permissions",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "permissions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "customers",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "note",
                table: "customers",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "customers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "group",
                table: "customers",
                newName: "Group");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "customers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "customers",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "customers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "customers",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "customers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tax_code",
                table: "customers",
                newName: "TaxCode");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "customers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "customers",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "customers",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "customers",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "default_shipping_address",
                table: "customers",
                newName: "DefaultShippingAddress");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "customers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "customers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "contact_person",
                table: "customers",
                newName: "ContactPerson");

            migrationBuilder.RenameColumn(
                name: "company_address",
                table: "customers",
                newName: "CompanyAddress");

            migrationBuilder.RenameIndex(
                name: "ix_customers_name",
                table: "customers",
                newName: "IX_customers_Name");

            migrationBuilder.RenameIndex(
                name: "ix_customers_tax_code",
                table: "customers",
                newName: "IX_customers_TaxCode");

            migrationBuilder.RenameIndex(
                name: "ix_customers_phone_number",
                table: "customers",
                newName: "IX_customers_PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "note",
                table: "customer_addresses",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "label",
                table: "customer_addresses",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "customer_addresses",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "customer_addresses",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "customer_addresses",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "customer_addresses",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "recipient_phone",
                table: "customer_addresses",
                newName: "RecipientPhone");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                table: "customer_addresses",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "is_default",
                table: "customer_addresses",
                newName: "IsDefault");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                table: "customer_addresses",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "customer_addresses",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "default_recipient",
                table: "customer_addresses",
                newName: "DefaultRecipient");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "customer_addresses",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "customer_addresses",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "customer_addresses",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_customer_addresses_customer_id",
                table: "customer_addresses",
                newName: "IX_customer_addresses_CustomerId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_roles",
                table: "user_roles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_units",
                table: "units",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                table: "roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_role_permissions",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_products",
                table: "products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_product_groups",
                table: "product_groups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permissions",
                table: "permissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customers",
                table: "customers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customer_addresses",
                table: "customer_addresses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_units_Code",
                table: "units",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_products_Code",
                table: "products",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_product_groups_Code",
                table: "product_groups",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Code",
                table: "permissions",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Code",
                table: "customers",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_addresses_customers_CustomerId",
                table: "customer_addresses",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_products_product_groups_ProductGroupId",
                table: "products",
                column: "ProductGroupId",
                principalTable: "product_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_products_units_UnitId",
                table: "products",
                column: "UnitId",
                principalTable: "units",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                table: "refresh_tokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_role_permissions_permissions_PermissionId",
                table: "role_permissions",
                column: "PermissionId",
                principalTable: "permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_role_permissions_roles_RoleId",
                table: "role_permissions",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_roles_RoleId",
                table: "user_roles",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_users_UserId",
                table: "user_roles",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
