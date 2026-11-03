using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;

namespace QuizPractice.Infrastructure.Persistence.Configurations;

public class SubjectDimensionConfiguration : IEntityTypeConfiguration<SubjectDimension>
{
    public void Configure(EntityTypeBuilder<SubjectDimension> b)
    {
        b.ToTable("SubjectDimensions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("dimension_id");
        b.Property(x => x.TypeId).HasColumnName("type");
        b.Property(x => x.Name).HasMaxLength(FieldLength.Name);
        b.HasIndex(x => new { x.SubjectId, x.TypeId, x.Name }).IsUnique();
    }
}
