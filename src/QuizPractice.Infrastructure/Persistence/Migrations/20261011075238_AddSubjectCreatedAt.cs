using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizPractice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Subjects",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Subjects");
        }
    }
}
