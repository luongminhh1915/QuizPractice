using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> b)
    {
        b.ToTable("Quizzes");
        b.HasKey(x => x.Id);
        b.Property(x => x.TypeId).HasColumnName("type");
        b.Property(x => x.LevelId).HasColumnName("level");
        b.Property(x => x.Name).HasMaxLength(FieldLength.Name);
        b.HasIndex(x => new { x.SubjectId, x.Name }).IsUnique();
        b.HasCheck("question_count", "[question_count] BETWEEN 1 AND 100");
        b.Property(x => x.SelectionType).HasMaxLength(FieldLength.Name);
        b.HasCheck("duration_min", "[duration_min] BETWEEN 1 AND 180");
        b.HasCheck("pass_rate", "[pass_rate] BETWEEN 0 AND 100");
        b.HasEnumColumn(x => x.Status, 10, ActiveStatus.Active);
    }
}
