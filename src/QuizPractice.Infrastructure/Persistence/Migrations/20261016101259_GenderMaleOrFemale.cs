using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizPractice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GenderMaleOrFemale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_gender",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Registrations_gender",
                table: "Registrations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_gender",
                table: "Users",
                sql: "[gender] IN ('Male', 'Female')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Registrations_gender",
                table: "Registrations",
                sql: "[gender] IN ('Male', 'Female')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_gender",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Registrations_gender",
                table: "Registrations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_gender",
                table: "Users",
                sql: "[gender] IN ('Male', 'Female', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Registrations_gender",
                table: "Registrations",
                sql: "[gender] IN ('Male', 'Female', 'Other')");
        }
    }
}
