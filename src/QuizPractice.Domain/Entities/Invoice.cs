using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Entities;

/// <summary>Groups one or more Submitted registrations paid by one bank transfer (BR-19, BR-24).</summary>
public class Invoice
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Sale user who confirmed it; empty while Unpaid (UC-69).</summary>
    public int? ConfirmedById { get; set; }
    public User? ConfirmedBy { get; set; }

    /// <summary>Transfer code: "QPS" + 6 upper-case hex characters.</summary>
    public string Code { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    // Copy of the receiving account when the invoice was made.
    public string BankName { get; set; } = string.Empty;
    public string BankBin { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Concurrency token: a confirmation and a cancellation cannot both win.</summary>
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>The registrations paid in this invoice now (Registration.InvoiceId).</summary>
    public List<Registration> Registrations { get; set; } = [];

    /// <summary>The registrations the invoice was made for; never changes afterwards.</summary>
    public List<InvoiceItem> Items { get; set; } = [];
}
