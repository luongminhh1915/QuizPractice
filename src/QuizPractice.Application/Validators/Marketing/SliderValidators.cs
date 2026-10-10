using FluentValidation;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.Marketing;

public class SaveSliderRequestValidator : AbstractValidator<SaveSliderRequest>
{
    public SaveSliderRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(FieldLength.Name);

        // UC-39 E1: an https:// address of up to 500 characters (BR-17).
        RuleFor(x => x.Backlink!)
            .MaximumLength(FieldLength.Url)
            .Must(BeHttpsUrl).WithMessage("The backlink must be a full https:// address.")
            .When(x => !string.IsNullOrEmpty(x.Backlink));

        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(FieldLength.Brief);

        // UC-39 E2.
        RuleFor(x => x.Image!).SetValidator(new ImageUploadValidator()).When(x => x.Image is not null);
    }

    private static bool BeHttpsUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}

public class SliderListRequestValidator : AbstractValidator<SliderListRequest>
{
    private static readonly string[] SortColumns = ["id", "title", "backlink", "status"];

    public SliderListRequestValidator()
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
