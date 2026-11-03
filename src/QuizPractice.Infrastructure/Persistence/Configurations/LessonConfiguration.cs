using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.ToTable("Lessons");
        b.HasKey(x => x.Id);
        b.Property(x => x.TypeId).HasColumnName("type");
        b.HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.TopicId);
        b.Property(x => x.Name).HasMaxLength(FieldLength.Name);

        // A name once under each topic, and once among the topics (topic_id NULL). EF would
        // filter out the NULL rows of a unique index; SQL Server counts NULL as one value.
        b.HasIndex(x => new { x.SubjectId, x.TopicId, x.Name }).IsUnique().HasFilter(null);

        b.HasCheck("display_order", "[display_order] >= 1");
        b.Property(x => x.VideoUrl).HasMaxLength(FieldLength.Url);
        b.HasCheck("video_url", "[video_url] LIKE 'https://%'");
        b.HasEnumColumn(x => x.Status, 10, ActiveStatus.Active);
    }
}
