using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Configurations;

public class PricePackageConfiguration : IEntityTypeConfiguration<PricePackage>
{
    public void Configure(EntityTypeBuilder<PricePackage> b)
    {
        b.ToTable("PricePackages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("package_id");
        b.Property(x => x.Name).HasMaxLength(FieldLength.Name);
        b.HasIndex(x => new { x.SubjectId, x.Name }).IsUnique();
        b.HasCheck("access_months", "[access_months] BETWEEN 1 AND 24");
        b.HasCheck("list_price", "[list_price] >= 0");
        b.HasCheck("sale_price", "[sale_price] >= 0 AND [sale_price] <= [list_price]");
        b.HasEnumColumn(x => x.Status, 10, ActiveStatus.Active);
    }
}
