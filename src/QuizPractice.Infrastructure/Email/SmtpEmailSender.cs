using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuizPractice.Application.Abstractions;
using QuizPractice.Infrastructure.Security;

namespace QuizPractice.Infrastructure.Email;

/// <summary>The "Email" section of appsettings.json.</summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    public string From { get; set; } = "no-reply@qps.vn";

    public string FromName { get; set; } = "QuizPractice";

    /// <summary>Empty: no mail leaves the machine, each one is written as an .eml file into PickupDirectory.</summary>
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    /// <summary>Where the .eml files go when Host is empty. Relative paths start at App_Data.</summary>
    public string PickupDirectory { get; set; } = "mails";

    /// <summary>Address of the React pages the links open, e.g. http://localhost:5173.</summary>
    public string LinkBaseUrl { get; set; } = "http://localhost:5173";
}

/// <summary>
/// Sends the account mails with System.Net.Mail (built into .NET). In development,
/// with no SMTP host set, every mail is a file in App_Data/mails: open it in Outlook or
/// Notepad and follow the link.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    IOptions<MailLinkOptions> links,
    string privateRootPath,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private EmailOptions Options => options.Value;

    public Task<bool> SendVerificationLinkAsync(string email, string fullName, string token, CancellationToken ct = default) =>
        SendAsync(email, "Verify your QuizPractice account", $"""
            Hello {fullName},

            Thank you for registering at QuizPractice. Open the link below to verify your email.
            Your login password will then be sent to this address.

            {Link("verify-email", token)}

            The link works for the next {links.Value.VerifyEmailHours} hours and only until the account is verified.
            If you did not register, ignore this mail.
            """, ct);

    public Task<bool> SendLoginPasswordAsync(string email, string fullName, string password, CancellationToken ct = default) =>
        SendAsync(email, "Your QuizPractice password", $"""
            Hello {fullName},

            Your email is verified. Sign in at {Options.LinkBaseUrl} with:

            Email:    {email}
            Password: {password}

            You can replace this password with Change Password in the account menu.
            """, ct);

    public Task<bool> SendAccountCreatedAsync(string email, string fullName, string password, string? notes, CancellationToken ct = default) =>
        SendAsync(email, "Your QuizPractice account is ready", $"""
            Hello {fullName},

            Your registration has been confirmed and an account has been created for you.
            Sign in at {Options.LinkBaseUrl} with:

            Email:    {email}
            Password: {password}

            You can replace this password with Change Password in the account menu.
            {(string.IsNullOrWhiteSpace(notes) ? "" : $"{Environment.NewLine}Notes from our Sale team: {notes}{Environment.NewLine}")}
            """, ct);

    public Task<bool> SendAccountByAdminAsync(string email, string fullName, string password, CancellationToken ct = default) =>
        SendAsync(email, "Your QuizPractice account", $"""
            Hello {fullName},

            The administrator has opened a QuizPractice account for you. Sign in at {Options.LinkBaseUrl} with:

            Email:    {email}
            Password: {password}

            You can replace this password with Change Password in the account menu.
            """, ct);

    public Task<bool> SendPasswordResetLinkAsync(string email, string fullName, string token, CancellationToken ct = default) =>
        SendAsync(email, "Reset your QuizPractice password", $"""
            Hello {fullName},

            Someone asked to reset the password of this account. Open the link below to set a new one.

            {Link("reset-password", token)}

            The link works for the next {links.Value.ResetPasswordMinutes} minutes and stops working once a new password is set.
            If you did not ask for it, ignore this mail: your password stays as it is.
            """, ct);

    private string Link(string page, string token) =>
        $"{Options.LinkBaseUrl.TrimEnd('/')}/{page}?token={Uri.EscapeDataString(token)}";

    private async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        using var message = new MailMessage(new MailAddress(Options.From, Options.FromName), new MailAddress(to))
        {
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            // 8-bit keeps the link on one line and readable in the .eml file; names may be Vietnamese.
            BodyTransferEncoding = TransferEncoding.EightBit,
        };

        using var client = CreateClient();
        try
        {
            await client.SendMailAsync(message, ct);
            return true;
        }
        catch (Exception e) when (e is SmtpException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            logger.LogError(e, "The mail \"{Subject}\" to {To} could not be sent.", subject, to);
            return false;
        }
    }

    private SmtpClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(Options.Host))
        {
            var folder = Path.Combine(privateRootPath, Options.PickupDirectory);
            Directory.CreateDirectory(folder);
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = Path.GetFullPath(folder),
            };
        }

        return new SmtpClient(Options.Host, Options.Port)
        {
            EnableSsl = Options.EnableSsl,
            Credentials = string.IsNullOrEmpty(Options.UserName) ? null : new NetworkCredential(Options.UserName, Options.Password),
        };
    }
}
