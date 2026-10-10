namespace QuizPractice.Domain.Constants;

/// <summary>
/// Text and number limits of BR-17 (proposed, pending the supervisor: OI-11).
/// The EF configurations and the validators both read them, so a change agreed
/// with the supervisor is made here once, followed by a migration.
/// </summary>
public static class FieldLength
{
    public const int Email = 100;
    public const int FullNameMin = 2;
    public const int FullName = 100;
    public const int Mobile = 10;
    public const int Address = 255;
    public const int PasswordMin = 8;
    public const int PasswordMax = 64;
    public const int PasswordHash = 255;

    /// <summary>Name or title of a subject, post, slider, lesson, quiz, package or setting value.</summary>
    public const int Name = 200;

    /// <summary>Tagline, brief, notes.</summary>
    public const int Brief = 500;

    /// <summary>Description, post content, question content, explanation, HTML lesson.</summary>
    public const int LongText = 20_000;

    /// <summary>Relative path of an uploaded file, e.g. /uploads/sliders/7f3a9c2e.png.</summary>
    public const int FilePath = 255;

    /// <summary>Lesson video, slider backlink, question media link.</summary>
    public const int Url = 500;

    public const int AnswerOption = 1_000;
    public const int BankName = 100;
    public const int BankBin = 6;
    public const int AccountNumber = 19;
    public const int AccountName = 100;
    public const int InvoiceCode = 9;

    public const long ImageMaxBytes = 2 * 1024 * 1024;

    /// <summary>Question media (UC-53 E3). Not in BR-17: proposed by the team, Duck 10/10.</summary>
    public const long AudioMaxBytes = 10 * 1024 * 1024;

    /// <summary>Question media (UC-53 E3). Not in BR-17: proposed by the team, Duck 10/10.</summary>
    public const long VideoMaxBytes = 20 * 1024 * 1024;
}
