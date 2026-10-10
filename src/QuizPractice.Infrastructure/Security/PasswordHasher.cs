using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Entities;
using IdentityHasher = Microsoft.AspNetCore.Identity.PasswordHasher<QuizPractice.Domain.Entities.User>;
using IdentityResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace QuizPractice.Infrastructure.Security;

/// <summary>ASP.NET Core Identity hasher (PBKDF2, salted), without the rest of Identity (SDS I.2).</summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly IdentityHasher _inner = new();

    // The Identity hasher never reads the user argument.
    private static readonly User NoUser = new();

    public string Hash(string password) => _inner.HashPassword(NoUser, password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(NoUser, hash, password) is IdentityResult.Success or IdentityResult.SuccessRehashNeeded;
}
