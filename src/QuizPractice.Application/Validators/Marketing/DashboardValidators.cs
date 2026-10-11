using FluentValidation;
using QuizPractice.Application.Dtos.Marketing;

namespace QuizPractice.Application.Validators.Marketing;

/// <summary>UC-34 E1, and a cap on the range so the trend keeps a readable number of days.</summary>
public class DashboardRequestValidator : AbstractValidator<DashboardRequest>
{
    public const int MaxDays = 366;

    public DashboardRequestValidator()
    {
        RuleFor(x => x.To)
            .Must((request, to) => to >= request.From)
            .WithMessage("The end date is earlier than the start date.")
            .When(x => x.From is not null && x.To is not null);

        RuleFor(x => x.To)
            .Must((request, to) => to!.Value.DayNumber - request.From!.Value.DayNumber < MaxDays)
            .WithMessage($"Choose a range of at most {MaxDays} days.")
            .When(x => x.From is not null && x.To is not null && x.To >= x.From);
    }
}
