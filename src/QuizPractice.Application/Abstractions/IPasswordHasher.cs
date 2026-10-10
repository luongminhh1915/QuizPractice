namespace QuizPractice.Application.Abstractions;

/// <summary>Hashes passwords; the database never holds plain text (NFR-01, SDS I.3 password_hash).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}
