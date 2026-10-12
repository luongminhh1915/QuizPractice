using FluentValidation;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Domain.Common;

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
