using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Entities;

public class Subject
{
    public const byte DefaultPassRate = 50;

    public int Id { get; set; }

    /// <summary>Owner Expert; empty until assigned (UC-42 A2, UC-43).</summary>
    public int? ExpertId { get; set; }
    public User? Expert { get; set; }

    /// <summary>Setting of type Subject Category.</summary>
    public int CategoryId { get; set; }
    public Setting Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string Thumbnail { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }

    /// <summary>Pass mark of a practice, in percent (BR-21).</summary>
    public byte PassRate { get; set; } = DefaultPassRate;

    public SubjectStatus Status { get; set; } = SubjectStatus.Unpublished;
    /// <summary>
    /// When the subject was created (UC-34: new subjects in a range). Filled by the database
    /// (SYSUTCDATETIME) when left at its default, so creating code need not set it.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<PricePackage> PricePackages { get; set; } = [];
}
