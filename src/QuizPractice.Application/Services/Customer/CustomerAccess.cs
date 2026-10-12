using QuizPractice.Application.Common;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// Answers the customer services share. The controllers already require the Customer
/// role (permission table, D23); the services still check who is signed in, because a
/// record of another customer must never be read or changed (UC-15 to UC-18, UC-67).
/// </summary>
internal static class CustomerAccess
{
    public static Error SignInRequired => new(ErrorType.Unauthorized, "Auth.Required", "Log in as a customer to see your account.");

    /// <summary>BR-05: no Paid registration of the subject covers today (UC-20 E1, UC-23 E1).</summary>
    public static Error NoAccess => new(ErrorType.Forbidden, "Subject.NoAccess",
        "You have no valid access to this subject. Register it, or renew the registration, to practise it.");

    /// <summary>A record of another customer answers like a missing one: nothing is told about it.</summary>
    public static Error NotFound(string what, int id) => Error.NotFound(what, id);
}
