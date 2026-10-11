using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subjects");
        b.HasKey(x => x.Id);
        b.Property(x => x.CategoryId).HasColumnName("category");
        b.Property(x => x.Name).HasMaxLength(FieldLength.Name);
        b.Property(x => x.Tagline).HasMaxLength(FieldLength.Brief);
        b.Property(x => x.Thumbnail).HasMaxLength(FieldLength.FilePath);
        b.Property(x => x.Brief).HasMaxLength(FieldLength.Brief);
        b.Property(x => x.IsFeatured).HasDbDefault(false);
        b.Property(x => x.PassRate).HasDbDefault(Subject.DefaultPassRate);
        b.HasCheck("pass_rate", "[pass_rate] BETWEEN 0 AND 100");
        b.HasEnumColumn(x => x.Status, 12, SubjectStatus.Unpublished);
        b.Property(x => x.CreatedAt).HasUtcNowDefault();
        b.Property(x => x.UpdatedAt).HasUtcNowDefault();
    }
}
