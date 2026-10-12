using FluentValidation;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Validators.TestContent;

namespace QuizPractice.Application.Validators.Customer;

/// <summary>
/// UC-20 step 3 (the CHECK on Attempts.question_count, SDS I.3). Whether the subject is
/// reachable and enough questions match needs the database: PracticeService checks it (E1, E2).
/// </summary>
public class CreatePracticeRequestValidator : AbstractValidator<CreatePracticeRequest>
{
    public CreatePracticeRequestValidator()
    {
        RuleFor(x => x.SubjectId).GreaterThan(0).WithMessage("Choose a subject.");
        RuleFor(x => x.QuestionCount).InclusiveBetween(1, SaveQuizRequestValidator.MaxQuestions)
            .WithMessage($"Practise from 1 to {SaveQuizRequestValidator.MaxQuestions} questions.");
        RuleFor(x => x.SelectionType).IsInEnum().WithMessage("Choose by topic or by dimension.");
    }
}
