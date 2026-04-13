using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexWearAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFieldsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Renombrar PaypalOrderId → MPOrderId
            migrationBuilder.RenameColumn(
                name: "PaypalOrderId",
                table: "Orders",
                newName: "MPOrderId");

            // Eliminar columnas que ya no se usan
            migrationBuilder.DropColumn(
                name: "PaypalCaptureId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Orders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MPOrderId",
                table: "Orders",
                newName: "PaypalOrderId");

            migrationBuilder.AddColumn<string>(
                name: "PaypalCaptureId",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
