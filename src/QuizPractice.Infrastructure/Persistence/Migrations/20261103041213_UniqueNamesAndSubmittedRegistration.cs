using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizPractice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueNamesAndSubmittedRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubjectDimensions_subject_id",
                table: "SubjectDimensions");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_subject_id",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_PricePackages_subject_id",
                table: "PricePackages");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_subject_id",
                table: "Lessons");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_name",
                table: "Subjects",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectDimensions_subject_id_type_name",
                table: "SubjectDimensions",
                columns: new[] { "subject_id", "type", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_user_id_subject_id",
                table: "Registrations",
                columns: new[] { "user_id", "subject_id" },
                unique: true,
                filter: "[user_id] IS NOT NULL AND [status] = 'Submitted'");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_subject_id_name",
                table: "Quizzes",
                columns: new[] { "subject_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricePackages_subject_id_name",
                table: "PricePackages",
                columns: new[] { "subject_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_subject_id_topic_id_name",
                table: "Lessons",
                columns: new[] { "subject_id", "topic_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_name",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_SubjectDimensions_subject_id_type_name",
                table: "SubjectDimensions");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_user_id_subject_id",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_subject_id_name",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_PricePackages_subject_id_name",
                table: "PricePackages");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_subject_id_topic_id_name",
                table: "Lessons");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectDimensions_subject_id",
                table: "SubjectDimensions",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_subject_id",
                table: "Quizzes",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_PricePackages_subject_id",
                table: "PricePackages",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_subject_id",
                table: "Lessons",
                column: "subject_id");
        }
    }
}
