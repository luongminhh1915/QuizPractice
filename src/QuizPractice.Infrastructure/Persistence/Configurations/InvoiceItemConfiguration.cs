using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizPractice.Domain.Entities;

namespace QuizPractice.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> b)
    {
        b.ToTable("InvoiceItems");
        b.HasKey(x => new { x.InvoiceId, x.RegistrationId });
        b.HasOne(x => x.Invoice).WithMany(i => i.Items).HasForeignKey(x => x.InvoiceId);
        b.HasOne(x => x.Registration).WithMany().HasForeignKey(x => x.RegistrationId);
        b.HasCheck("amount", "[amount] >= 0");
    }
}
