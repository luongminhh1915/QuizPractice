using FluentValidation;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.CourseContent;

public class LessonListRequestValidator : AbstractValidator<LessonListRequest>
{
    public LessonListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Status).IsInEnum();
    }
}

/// <summary>
/// UC-50 step 6 and E2 for every type (BR-17; the CHECKs of SDS I.3). What the chosen type
/// requires (topic, quiz) needs the database: LessonService checks it.
/// </summary>
public class SaveLessonRequestValidator : AbstractValidator<SaveLessonRequest>
{
    public SaveLessonRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Enter the lesson name.").MaximumLength(FieldLength.Name);
        RuleFor(x => x.TypeId).GreaterThan(0).WithMessage("Choose the lesson type.");
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(1).WithMessage("The order is a whole number from 1.")
            .When(x => x.DisplayOrder is not null);
        RuleFor(x => x.VideoUrl!)
            .MaximumLength(FieldLength.Url)
            .Must(u => Uri.TryCreate(u.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("The video link is a full https:// address, such as a YouTube link.")
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl));
        RuleFor(x => x.HtmlContent).MaximumLength(FieldLength.LongText);
    }
}
