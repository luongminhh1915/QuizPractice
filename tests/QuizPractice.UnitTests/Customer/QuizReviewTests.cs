using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Services.Learning;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Customer;

/// <summary>CUS-07: UC-31 Review Quiz Results (QuizReviewService).</summary>
public class QuizReviewTests
{
    private readonly Mock<IAttemptRepository> _attempts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Question _q1 = LearningData.Question(1, correct: 0);
    private readonly Question _q2 = LearningData.Question(2, correct: 3);

    public QuizReviewTests() => LearningData.SetupLabels(_attempts);

    private QuizReviewService Service(Mock<ICurrentUser>? user = null) =>
        new(_attempts.Object, _unitOfWork.Object, (user ?? MockUser.Customer()).Object);

    private Attempt Own(Attempt attempt)
    {
        _attempts.Setup(a => a.GetOwnAsync(attempt.Id, TestData.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        return attempt;
    }

    [Fact]
    public async Task GetAsync_ScoredAttempt_ShowsTheChosenAndTheCorrectAnswers()
    {
        var attempt = Own(LearningData.Attempt(900, questions: [_q1, _q2]));
        attempt.Questions[0].ChosenOptionId = LearningData.CorrectOptionOf(_q1);
        attempt.Questions[1].ChosenOptionId = LearningData.WrongOptionOf(_q2);
        attempt.Questions[1].IsMarked = true;
        attempt.Status = AttemptStatus.Submitted;
        attempt.Score = 50;
        attempt.PassRate = 50;
        attempt.FinishedAt = DateTime.UtcNow;

        var dto = ResultAssert.Succeeded(await Service().GetAsync(900));

        Assert.Equal(1, dto.Result.Correct);
        Assert.True(dto.Questions[0].IsCorrect);
        Assert.False(dto.Questions[1].IsCorrect);
        Assert.True(dto.Questions[1].IsMarked);
        Assert.Equal("D", dto.Questions[1].Options.Single(o => o.IsCorrect).Letter);
        Assert.Equal("Because of rule 2.", dto.Questions[1].Explanation);
    }

    [Fact]
    public async Task GetAsync_AttemptStillRunning_ReturnsNotScored()
    {
        Own(LearningData.Attempt(900, questions: [_q1, _q2]));

        var result = await Service().GetAsync(900);

        ResultAssert.Failed(result, ErrorType.Conflict, "Attempt.NotScored");
    }

    [Fact]
    public async Task GetAsync_ExamPastItsTime_IsScoredThenReviewed()
    {
        var exam = LearningData.Exam(300, durationMin: 30, rules: (11, 2));
        var attempt = Own(LearningData.Attempt(900, AttemptMode.Exam, exam, DateTime.UtcNow.AddMinutes(-40), questions: [_q1, _q2]));

        var dto = ResultAssert.Succeeded(await Service().GetAsync(900));

        Assert.Equal(AttemptStatus.Submitted, attempt.Status);
        Assert.Equal(0m, dto.Result.Score);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_AnotherCustomersAttempt_ReturnsNotFound()
    {
        var result = await Service().GetAsync(999);

        ResultAssert.Failed(result, ErrorType.NotFound, "Attempt.NotFound");
    }

    [Fact]
    public async Task GetAsync_Guest_ReturnsAuthRequired()
    {
        var result = await Service(MockUser.Guest()).GetAsync(900);

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
    }
}
