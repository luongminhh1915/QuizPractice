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

    /// <summary>A record of another customer answers like a missing one: nothing is told about it.</summary>
    public static Error NotFound(string what, int id) => Error.NotFound(what, id);
}
