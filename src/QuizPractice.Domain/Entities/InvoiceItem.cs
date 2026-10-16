namespace QuizPractice.Domain.Entities;

/// <summary>
/// One registration an invoice was made for, with the amount it counted then (BR-24).
/// Registration.InvoiceId names the invoice a registration is paid in now; when a
/// cancelled invoice's registration goes into a new invoice, the cancelled one still
/// lists it here, so its lines always add up to its total.
/// </summary>
public class InvoiceItem
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int RegistrationId { get; set; }
    public Registration Registration { get; set; } = null!;

    public decimal Amount { get; set; }
}
