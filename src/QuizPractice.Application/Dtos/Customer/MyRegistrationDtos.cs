using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Customer;

/// <summary>Criteria of My Registrations (UC-15): the newest first, ten a page.</summary>
public sealed record MyRegistrationListRequest(int Page = 1, int PageSize = 10);

/// <summary>The unpaid invoice a registration waits in (BR-24); its code opens Invoice QR.</summary>
public sealed record InvoiceRefDto(int Id, string Code);

/// <summary>
/// One registration of the customer (SRS III 3.1). CanChange: Submitted and in no unpaid
/// invoice, so Edit, Cancel and the Pay by QR check box are offered (UC-15 step 4, BR-24).
/// </summary>
public sealed record MyRegistrationDto(
    int Id,
    int SubjectId,
    string SubjectName,
    int PackageId,
    string PackageName,
    decimal TotalCost,
    DateTime RegisteredAt,
    RegistrationStatus Status,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    InvoiceRefDto? UnpaidInvoice,
    bool CanChange)
{
    public static MyRegistrationDto From(Registration r)
    {
        var unpaid = r.Invoice is { Status: InvoiceStatus.Unpaid } invoice ? new InvoiceRefDto(invoice.Id, invoice.Code) : null;
        return new MyRegistrationDto(
            r.Id, r.SubjectId, r.Subject.Name, r.PackageId, r.Package.Name, r.TotalCost,
            DateTime.SpecifyKind(r.RegisteredAt, DateTimeKind.Utc), r.Status, r.ValidFrom, r.ValidTo,
            unpaid, r.Status == RegistrationStatus.Submitted && unpaid is null);
    }
}
