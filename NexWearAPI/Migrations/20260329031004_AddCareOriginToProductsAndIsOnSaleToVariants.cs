using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexWearAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCareOriginToProductsAndIsOnSaleToVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnSale",
                table: "ProductVariants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Care",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnSale",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Care",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Products");
        }
    }
}
