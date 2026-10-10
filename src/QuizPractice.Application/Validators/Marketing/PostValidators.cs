using FluentValidation;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.Marketing;

/// <summary>UC-36 E1 and E2. The category and the version need the database: PostService checks them.</summary>
public class SavePostRequestValidator : AbstractValidator<SavePostRequest>
{
    public SavePostRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(FieldLength.Name);
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Choose a post category.");
        RuleFor(x => x.Brief).NotEmpty().MaximumLength(FieldLength.Brief);
        RuleFor(x => x.Content).MaximumLength(FieldLength.LongText);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Thumbnail!).SetValidator(new ImageUploadValidator()).When(x => x.Thumbnail is not null);
    }
}

public class PostListRequestValidator : AbstractValidator<PostListRequest>
{
    private static readonly string[] SortColumns = ["id", "title", "category", "featured", "status"];

    public PostListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .WithMessage($"Sort by one of: {string.Join(", ", SortColumns)}.")
            .When(x => x.SortBy is not null);
    }
}
