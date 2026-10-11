using System.Security.Cryptography;

namespace QuizPractice.Application.Common;

/// <summary>
/// The login password the system makes for an account (UC-03; OI-16, as F#38 does for
/// a user the Admin adds): 12 characters, at least one letter and one digit, so it
/// passes the password rule of BR-17. Letters and digits that look alike (0/O, 1/l/I)
/// are left out, because the user copies it from a mail.
/// </summary>
public static class PasswordGenerator
{
    public const int Length = 12;

    private const string Letters = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string All = Letters + Digits;

    public static string Generate()
    {
        var chars = new char[Length];
        chars[0] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
        chars[1] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        for (var i = 2; i < Length; i++)
        {
            chars[i] = All[RandomNumberGenerator.GetInt32(All.Length)];
        }

        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);
    }
}
