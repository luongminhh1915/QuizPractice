using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> b)
    {
        b.ToTable("Registrations");
        b.HasKey(x => x.Id);
        b.Property(x => x.CreatedById).HasColumnName("created_by");
        b.Property(x => x.UpdatedById).HasColumnName("updated_by");
        b.Property(x => x.FullName).HasMaxLength(FieldLength.FullName);
        b.Property(x => x.Email).HasMaxLength(FieldLength.Email);
        b.Property(x => x.Mobile).IsAscii(FieldLength.Mobile);
        b.HasEnumColumn(x => x.Gender, 6);
        b.HasCheck("total_cost", "[total_cost] >= 0");

        // Concurrency token: of two Sale users settling the same registration, the
        // second save fails instead of overwriting the first (BR-22).
        b.HasEnumColumn(x => x.Status, 10, RegistrationStatus.Submitted).IsConcurrencyToken();

        // A3 of UC-14 and UC-60: one Submitted registration per account and subject.
        b.HasIndex(x => new { x.UserId, x.SubjectId }).IsUnique()
            .HasFilter("[user_id] IS NOT NULL AND [status] = 'Submitted'");

        // The filtered index above skips most rows, so My Registrations keeps its own.
        b.HasIndex(x => x.UserId, "IX_Registrations_user_id");

        b.Property(x => x.RegisteredAt).HasUtcNowDefault();
        b.HasCheck("valid_to", "[valid_to] >= [valid_from]");
        b.Property(x => x.Notes).HasMaxLength(FieldLength.Brief);
    }
}
