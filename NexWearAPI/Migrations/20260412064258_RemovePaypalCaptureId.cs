using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexWearAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaypalCaptureId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaypalCaptureId",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaypalCaptureId",
                table: "Orders");
        }
    }
}
