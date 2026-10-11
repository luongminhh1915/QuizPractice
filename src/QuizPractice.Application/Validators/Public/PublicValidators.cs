using FluentValidation;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.Public;

public class SubjectListRequestValidator : AbstractValidator<SubjectListRequest>
{
    public SubjectListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public class BlogListRequestValidator : AbstractValidator<BlogListRequest>
{
    public BlogListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
