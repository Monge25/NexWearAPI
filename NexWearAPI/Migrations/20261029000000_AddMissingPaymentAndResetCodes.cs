using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexWearAPI.Data;

#nullable disable

namespace NexWearAPI.Migrations
{
    /// <summary>
    /// Agrega Orders.PaymentMethod, Orders.PaidAt y la tabla PasswordResetCodes,
    /// que estaban en el modelo pero ninguna migración las creaba.
    /// Usa IF NOT EXISTS para no fallar en bases donde ya existen.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261029000000_AddMissingPaymentAndResetCodes")]
    public partial class AddMissingPaymentAndResetCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""PaymentMethod"" character varying(20) NOT NULL DEFAULT 'mercadopago';
                ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""PaidAt"" timestamp with time zone NULL;

                CREATE TABLE IF NOT EXISTS ""PasswordResetCodes"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""Code"" character varying(6) NOT NULL,
                    ""IsUsed"" boolean NOT NULL,
                    ""ExpiresAt"" timestamp with time zone NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_PasswordResetCodes"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_PasswordResetCodes_Users_UserId"" FOREIGN KEY (""UserId"")
                        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_PasswordResetCodes_UserId"" ON ""PasswordResetCodes"" (""UserId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""PasswordResetCodes"";
                ALTER TABLE ""Orders"" DROP COLUMN IF EXISTS ""PaidAt"";
                ALTER TABLE ""Orders"" DROP COLUMN IF EXISTS ""PaymentMethod"";
            ");
        }
    }
}
