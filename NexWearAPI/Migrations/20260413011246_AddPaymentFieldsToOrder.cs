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
            migrationBuilder.AddColumn<string>(
               name: "MPOrderId",
               table: "Orders",
               type: "character varying(100)",
               maxLength: 100,
               nullable: true);

            // Eliminar columnas que ya no se usan
            //migrationBuilder.DropColumn(
            //    name: "PaypalCaptureId",
            //    table: "Orders");

            //migrationBuilder.DropColumn(
            //    name: "ShippingAddress",
            //    table: "Orders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MPOrderId",
                table: "Orders");
        }
    }
}
