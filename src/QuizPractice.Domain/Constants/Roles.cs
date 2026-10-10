namespace QuizPractice.Domain.Constants;

/// <summary>
/// The five roles: values of the User Role setting (BR-16). Constants so that
/// [Authorize(Roles = Roles.Admin)] and role checks in services read one spelling.
/// A Guest is not a role: it is anyone not signed in.
/// </summary>
public static class Roles
{
    public const string Customer = "Customer";
    public const string Expert = "Expert";
    public const string Marketing = "Marketing";
    public const string Sale = "Sale";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Customer, Expert, Marketing, Sale, Admin];
}
