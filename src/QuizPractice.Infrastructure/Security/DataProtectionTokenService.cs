using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using QuizPractice.Application.Abstractions;

namespace QuizPractice.Infrastructure.Security;

/// <summary>The "Links" section of appsettings.json: how long a mailed link works (BR-01, BR-02).</summary>
public sealed class MailLinkOptions
{
    public const string Section = "Links";

    public int VerifyEmailHours { get; set; } = 24;

    public int ResetPasswordMinutes { get; set; } = 30;
}

/// <summary>
/// ASP.NET Core Data Protection with a time limit: the token is encrypted and signed
/// with the key ring the Api keeps in App_Data/keys, so a token cannot be read,
/// changed or made outside the server.
/// </summary>
public sealed class DataProtectionTokenService(IDataProtectionProvider provider, IOptions<MailLinkOptions> options) : ITokenService
{
    public string Create(LinkPurpose purpose, int userId, string stamp)
    {
        var lifetime = purpose == LinkPurpose.VerifyEmail
            ? TimeSpan.FromHours(options.Value.VerifyEmailHours)
            : TimeSpan.FromMinutes(options.Value.ResetPasswordMinutes);
        return Protector(purpose).Protect($"{userId.ToString(CultureInfo.InvariantCulture)}|{stamp}", lifetime);
    }

    public LinkToken? Read(LinkPurpose purpose, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        string payload;
        try
        {
            payload = Protector(purpose).Unprotect(token.Trim(), out _);
        }
        catch (CryptographicException)
        {
            // Not ours, changed, for the other purpose or expired: all the same to the user (UC-03 E1, UC-05 E1).
            return null;
        }
        catch (FormatException)
        {
            return null;
        }

        var parts = payload.Split('|', 2);
        return parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            ? new LinkToken(userId, parts[1])
            : null;
    }

    private ITimeLimitedDataProtector Protector(LinkPurpose purpose) =>
        provider.CreateProtector("QuizPractice.Links", purpose.ToString()).ToTimeLimitedDataProtector();
}
