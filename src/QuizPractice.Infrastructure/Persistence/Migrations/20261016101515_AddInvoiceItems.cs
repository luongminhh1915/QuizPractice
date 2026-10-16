using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizPractice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    invoice_id = table.Column<int>(type: "int", nullable: false),
                    registration_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,0)", precision: 12, scale: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceItems", x => new { x.invoice_id, x.registration_id });
                    table.CheckConstraint("CK_InvoiceItems_amount", "[amount] >= 0");
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "Invoices",
                        principalColumn: "invoice_id");
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Registrations_registration_id",
                        column: x => x.registration_id,
                        principalTable: "Registrations",
                        principalColumn: "registration_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_registration_id",
                table: "InvoiceItems",
                column: "registration_id");

            // Invoices made before this table: each lists the registrations still pointing
            // at it. A cancelled invoice whose registration already moved to a newer one
            // cannot get that line back.
            migrationBuilder.Sql(
                "INSERT INTO InvoiceItems (invoice_id, registration_id, amount) "
                + "SELECT invoice_id, registration_id, total_cost FROM Registrations WHERE invoice_id IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceItems");
        }
    }
}
