using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Learning;
using QuizPractice.Application.Services.Learning;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Customer;

/// <summary>
/// CUS-06: UC-24 Take Quiz, UC-25 Navigate, UC-26 Mark, UC-28 Peek at Answer, UC-29 Submit
/// and Score, UC-30 Resume (QuizHandleService).
/// </summary>
public class QuizHandleTests
{
    private readonly Mock<IAttemptRepository> _attempts = new();
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IPrivateFileStorage> _media = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Subject _java = TestData.Subject(1, "Java OOP");
    private readonly Question _q1 = LearningData.Question(1, correct: 0);
    private readonly Question _q2 = LearningData.Question(2, correct: 2);
    private readonly Question _q3 = LearningData.Question(3, correct: 1, media: "/media/q3.png");

    public QuizHandleTests()
    {
        LearningData.SetupLabels(_attempts);
        LearningData.SetupAccess(_registrations, _java);
    }

    private QuizHandleService Service(Mock<ICurrentUser>? user = null) =>
        new(_attempts.Object, _registrations.Object, _media.Object, _unitOfWork.Object, (user ?? MockUser.Customer()).Object);

    private Attempt Practice() => Own(LearningData.Attempt(900, questions: [_q1, _q2, _q3]));

    private Attempt Exam(DateTime? startedAt = null) =>
        Own(LearningData.Attempt(900, AttemptMode.Exam, LearningData.Exam(300, durationMin: 30, subject: _java, rules: (11, 3)),
            startedAt, questions: [_q1, _q2, _q3]));

    private Attempt Own(Attempt attempt)
    {
        _attempts.Setup(a => a.GetOwnAsync(attempt.Id, TestData.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        return attempt;
    }

    // ------------------------------------------------------------------ UC-24, UC-30

    [Fact]
    public async Task GetAsync_RunningAttempt_ReturnsQuestionsWithoutTheCorrectAnswer()
    {
        var attempt = Practice();
        attempt.Questions[1].ChosenOptionId = LearningData.WrongOptionOf(_q2);
        attempt.Questions[1].IsMarked = true;
        attempt.CurrentOrder = 2;

        var dto = ResultAssert.Succeeded(await Service().GetAsync(900));

        Assert.Equal(3, dto.Questions.Count);
        Assert.Equal(2, dto.CurrentOrder);
        Assert.True(dto.Questions[1].IsMarked);
        Assert.Equal(["A", "B", "C", "D"], dto.Questions[0].Options.Select(o => o.Letter));
        Assert.Equal("image", dto.Questions[2].MediaKind);
        Assert.Null(dto.EndsAt); // a practice has no countdown (BR-06)
    }

    [Fact]
    public async Task GetAsync_Exam_EndsAfterItsDuration()
    {
        var started = DateTime.UtcNow.AddMinutes(-10);
        Exam(started);

        var dto = ResultAssert.Succeeded(await Service().GetAsync(900));

        Assert.Equal(started.AddMinutes(30), dto.EndsAt);
    }

    [Fact]
    public async Task AnswerAsync_FirstAnswer_IsRecordedAndBecomesCurrent()
    {
        var attempt = Practice();

        var dto = ResultAssert.Succeeded(await Service().AnswerAsync(900, 2, new AnswerRequest(22)));

        Assert.Equal(22, attempt.Questions[1].ChosenOptionId);
        Assert.Equal(2, attempt.CurrentOrder);
        Assert.Equal(22, dto.ChosenOptionId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnswerAsync_AnotherChoice_ReplacesTheFirstOne()
    {
        var attempt = Practice();
        attempt.Questions[0].ChosenOptionId = 11;

        await Service().AnswerAsync(900, 1, new AnswerRequest(13));

        Assert.Equal(13, attempt.Questions[0].ChosenOptionId);
    }

    [Fact]
    public async Task AnswerAsync_OptionOfAnotherQuestion_ReturnsValidationError()
    {
        Practice();

        var result = await Service().AnswerAsync(900, 1, new AnswerRequest(21));

        ResultAssert.Invalid(result, "OptionId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task AnswerAsync_OrderOutsideTheAttempt_ReturnsQuestionNotFound(int order)
    {
        Practice();

        var result = await Service().AnswerAsync(900, order, new AnswerRequest(11));

        ResultAssert.Failed(result, ErrorType.NotFound, "Question.NotFound");
    }

    // ------------------------------------------------------------------ UC-25, UC-26

    [Fact]
    public async Task MoveAsync_ToTheLastQuestion_KeepsTheAnswers()
    {
        var attempt = Practice();
        attempt.Questions[0].ChosenOptionId = 11;

        var dto = ResultAssert.Succeeded(await Service().MoveAsync(900, new MoveRequest(3)));

        Assert.Equal(3, dto.Order);
        Assert.Equal(3, attempt.CurrentOrder);
        Assert.Equal(11, attempt.Questions[0].ChosenOptionId);
    }

    [Fact]
    public async Task MoveAsync_PastTheLastQuestion_ReturnsValidationError()
    {
        Practice();

        var result = await Service().MoveAsync(900, new MoveRequest(4));

        ResultAssert.Invalid(result, "Order");
    }

    [Fact]
    public async Task MarkAsync_AnsweredQuestion_KeepsTheAnswer()
    {
        var attempt = Practice();
        attempt.Questions[1].ChosenOptionId = 23;

        var dto = ResultAssert.Succeeded(await Service().MarkAsync(900, 2, new MarkRequest(true)));

        Assert.True(dto.IsMarked);
        Assert.Equal(23, dto.ChosenOptionId);
    }

    [Fact]
    public async Task MarkAsync_Unmark_ClearsTheMark()
    {
        var attempt = Practice();
        attempt.Questions[0].IsMarked = true;

        await Service().MarkAsync(900, 1, new MarkRequest(false));

        Assert.False(attempt.Questions[0].IsMarked);
    }

    // ------------------------------------------------------------------ UC-28

    [Fact]
    public async Task PeekAsync_DuringPractice_ShowsTheCorrectOptionAndStoresNothing()
    {
        Practice();

        var dto = ResultAssert.Succeeded(await Service().PeekAsync(900, 2));

        Assert.Equal("C", dto.Letter);
        Assert.Equal(23, dto.OptionId);
        Assert.Equal("Because of rule 2.", dto.Explanation);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PeekAsync_DuringExam_ReturnsNoPeek()
    {
        Exam();

        var result = await Service().PeekAsync(900, 1);

        ResultAssert.Failed(result, ErrorType.Forbidden, "Attempt.NoPeek");
    }

    // ------------------------------------------------------------------ UC-29

    [Fact]
    public async Task SubmitAsync_TwoOfThreeCorrect_ScoresAndClosesTheAttempt()
    {
        var attempt = Practice();
        attempt.Questions[0].ChosenOptionId = LearningData.CorrectOptionOf(_q1);
        attempt.Questions[1].ChosenOptionId = LearningData.CorrectOptionOf(_q2);
        attempt.Questions[2].ChosenOptionId = LearningData.WrongOptionOf(_q3);

        var dto = ResultAssert.Succeeded(await Service().SubmitAsync(900));

        Assert.Equal(2, dto.Correct);
        Assert.Equal(66.67m, dto.Score);
        Assert.Equal(50, dto.PassRate); // the subject's pass rate for a practice (BR-21)
        Assert.True(dto.Passed);
        Assert.Equal(AttemptStatus.Submitted, attempt.Status);
        Assert.NotNull(attempt.FinishedAt);
    }

    [Fact]
    public async Task SubmitAsync_NoAnswer_ScoresZero()
    {
        Practice();

        var dto = ResultAssert.Succeeded(await Service().SubmitAsync(900));

        Assert.Equal(0, dto.Correct);
        Assert.Equal(0m, dto.Score);
        Assert.False(dto.Passed);
    }

    [Fact]
    public async Task SubmitAsync_ExamAtExactlyItsPassRate_Passes()
    {
        var q = Enumerable.Range(1, 10).Select(i => LearningData.Question(i)).ToArray();
        var attempt = Own(LearningData.Attempt(900, AttemptMode.Exam,
            LearningData.Exam(300, passRate: 70, subject: _java, rules: (11, 10)), questions: q));
        foreach (var row in attempt.Questions.Take(7))
        {
            row.ChosenOptionId = LearningData.CorrectOptionOf(row.Question);
        }

        var dto = ResultAssert.Succeeded(await Service().SubmitAsync(900));

        Assert.Equal(70m, dto.Score);
        Assert.Equal(70, dto.PassRate);
        Assert.True(dto.Passed);
    }

    [Fact]
    public async Task SubmitAsync_AfterTheCountdown_FinishesAtTheEndTime()
    {
        var started = DateTime.UtcNow.AddMinutes(-40);
        var attempt = Exam(started);

        ResultAssert.Succeeded(await Service().SubmitAsync(900));

        Assert.Equal(started.AddMinutes(30), attempt.FinishedAt);
    }

    [Fact]
    public async Task SubmitAsync_AlreadyScored_ReturnsClosed()
    {
        Practice().Status = AttemptStatus.Submitted;

        var result = await Service().SubmitAsync(900);

        ResultAssert.Failed(result, ErrorType.Conflict, "Attempt.Closed");
    }

    [Fact]
    public async Task AnswerAsync_AccessEndedDuringTheAttempt_ReturnsAccessEnded()
    {
        Practice();
        LearningData.SetupAccess(_registrations);

        var result = await Service().AnswerAsync(900, 1, new AnswerRequest(11));

        ResultAssert.Failed(result, ErrorType.Forbidden, "Attempt.AccessEnded");
    }

    [Fact]
    public async Task GetAsync_AnotherCustomersAttempt_ReturnsNotFound()
    {
        var result = await Service().GetAsync(999);

        ResultAssert.Failed(result, ErrorType.NotFound, "Attempt.NotFound");
    }

    // ------------------------------------------------------------------ media

    [Fact]
    public async Task GetMediaAsync_QuestionWithImage_ReturnsTheFile()
    {
        Practice();
        _media.Setup(m => m.OpenRead("/media/q3.png")).Returns(new MemoryStream([1, 2, 3]));

        var file = ResultAssert.Succeeded(await Service().GetMediaAsync(900, 3));

        Assert.Equal("image/png", file.ContentType);
    }

    [Fact]
    public async Task GetMediaAsync_QuestionWithoutMedia_ReturnsNoMedia()
    {
        Practice();

        var result = await Service().GetMediaAsync(900, 1);

        ResultAssert.Failed(result, ErrorType.NotFound, "Question.NoMedia");
    }
}
