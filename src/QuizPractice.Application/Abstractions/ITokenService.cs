namespace QuizPractice.Application.Abstractions;

/// <summary>What a link sent by mail is for; a token made for one purpose is refused for the other.</summary>
public enum LinkPurpose
{
    VerifyEmail,
    ResetPassword,
}

/// <summary>What a genuine, unexpired link carries.</summary>
public sealed record LinkToken(int UserId, string Stamp);

/// <summary>
/// Stateless links of the account mails (SDS, decided 06/10): the token holds the user
/// id, the purpose, the expiry and a stamp, signed by the server; nothing is stored.
/// The stamp makes a link single-use: the service puts in it something the use of the
/// link changes (UC-05: part of the current password hash), and refuses the link once
/// the stamp no longer matches. The lifetime of each purpose is set in the
/// configuration file (BR-01, BR-02).
/// </summary>
public interface ITokenService
{
    string Create(LinkPurpose purpose, int userId, string stamp);

    /// <summary>The content of the token, or null when it is not genuine, is for another purpose or has expired.</summary>
    LinkToken? Read(LinkPurpose purpose, string token);
}
