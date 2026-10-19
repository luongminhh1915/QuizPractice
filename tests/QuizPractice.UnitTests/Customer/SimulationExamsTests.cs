using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Application.Validators.Customer;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Customer;

/// <summary>CUS-05: UC-22 View Simulation Exams, UC-23 Start Simulation Exam (SimulationExamService).</summary>
public class SimulationExamsTests
{
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IQuizRepository> _quizzes = new();
    private readonly Mock<IAttemptRepository> _attempts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Subject _java = TestData.Subject(1, "Java OOP");
    private readonly Quiz _exam;
    private Attempt? _added;

    public SimulationExamsTests()
    {
        // Two questions from topic 11 and one from topic 12.
        _exam = LearningData.Exam(300, subject: _java, rules: [(11, 2), (12, 1)]);
        _quizzes.Setup(q => q.GetWithRulesAsync(300, It.IsAny<CancellationToken>())).ReturnsAsync(_exam);
        _quizzes.Setup(q => q.GetTopicsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([LearningData.Topic(11, "Classes"), LearningData.Topic(12, "Inheritance")]);
        _quizzes.Setup(q => q.GetTopicMembersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. LearningData.Members(11, 1, 2, 3), .. LearningData.Members(12, 4)]);
        LearningData.SetupLabels(_attempts);
        LearningData.SetupAccess(_registrations, _java);
        _attempts.Setup(a => a.Add(It.IsAny<Attempt>())).Callback<Attempt>(a => { a.Id = 901; _added = a; });
    }

    private SimulationExamService Service(Mock<ICurrentUser>? user = null) =>
        new(_registrations.Object, _quizzes.Object, _attempts.Object, _unitOfWork.Object, (user ?? MockUser.Customer()).Object,
            new ExamListRequestValidator());

    private void OpenAttempt(Attempt attempt) =>
        _attempts.Setup(a => a.ListOpenExamsAsync(TestData.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync([attempt]);

    // ------------------------------------------------------------------ UC-22

    [Fact]
    public async Task ListAsync_SearchesOnlyReachableSubjects()
    {
        ExamSearch? search = null;
        _quizzes.Setup(q => q.SearchExamsAsync(It.IsAny<ExamSearch>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ExamSearch, PageRequest, CancellationToken>((s, _, _) => search = s)
            .ReturnsAsync(new Page<Quiz>([_exam], 1, 1, 10));

        var page = ResultAssert.Succeeded(await Service().ListAsync(new ExamListRequest(Search: "oop")));

        Assert.Equal([1], search!.SubjectIds);
        Assert.Equal("oop", search.Text);
        var row = Assert.Single(page.Items);
        Assert.Equal(("Java OOP", "Medium", 3, 30, 70), (row.SubjectName, row.Level, row.QuestionCount, row.DurationMin, row.PassRate));
    }

    [Fact]
    public async Task ListAsync_UnknownSortColumn_ReturnsValidationError()
    {
        var result = await Service().ListAsync(new ExamListRequest(SortBy: "duration"));

        ResultAssert.Invalid(result, "SortBy");
    }

    [Fact]
    public async Task GetFiltersAsync_NoReachableSubject_ReturnsEmptyFilters()
    {
        LearningData.SetupAccess(_registrations);

        var filters = ResultAssert.Succeeded(await Service().GetFiltersAsync());

        Assert.Empty(filters.Subjects);
    }

    // ------------------------------------------------------------------ UC-23

    [Fact]
    public async Task GetAsync_WithUnfinishedAttempt_OffersToResumeIt()
    {
        OpenAttempt(LearningData.Attempt(950, AttemptMode.Exam, _exam, questions: LearningData.Question(1)));

        var dto = ResultAssert.Succeeded(await Service().GetAsync(300));

        Assert.Equal(950, dto.OpenAttemptId);
    }

    [Fact]
    public async Task StartAsync_DrawsQuestionsByTheRulesOfTheExam()
    {
        var created = ResultAssert.Succeeded(await Service().StartAsync(300));

        Assert.Equal(901, created.Id);
        Assert.Equal(AttemptMode.Exam, _added!.Mode);
        Assert.Equal(300, _added.QuizId);
        Assert.Equal(3, _added.QuestionCount);
        var ids = _added.Questions.Select(q => q.QuestionId).ToList();
        Assert.Equal(2, ids.Count(id => id is 1 or 2 or 3));
        Assert.Contains(4, ids);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithUnfinishedAttempt_ReturnsUnfinished()
    {
        OpenAttempt(LearningData.Attempt(950, AttemptMode.Exam, _exam, questions: LearningData.Question(1)));

        var result = await Service().StartAsync(300);

        ResultAssert.Failed(result, ErrorType.Conflict, "Exam.Unfinished");
        _attempts.Verify(a => a.Add(It.IsAny<Attempt>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_UnfinishedAttemptPastItsTime_IsScoredAndANewOneStarts()
    {
        var expired = LearningData.Attempt(950, AttemptMode.Exam, _exam, DateTime.UtcNow.AddMinutes(-31), questions: LearningData.Question(1));
        OpenAttempt(expired);

        var result = await Service().StartAsync(300);

        Assert.True(result.IsSuccess);
        Assert.Equal(AttemptStatus.Submitted, expired.Status);
    }

    [Fact]
    public async Task StartAsync_GroupShortOfQuestions_ReturnsNotAvailable()
    {
        _exam.Rules[1].QuestionCount = 2; // topic 12 holds only one shown question

        var result = await Service().StartAsync(300);

        ResultAssert.Failed(result, ErrorType.Conflict, "Exam.NotAvailable");
    }

    [Fact]
    public async Task StartAsync_NoValidAccess_ReturnsNoAccess()
    {
        LearningData.SetupAccess(_registrations);

        var result = await Service().StartAsync(300);

        ResultAssert.Failed(result, ErrorType.Forbidden, "Subject.NoAccess");
    }

    [Fact]
    public async Task StartAsync_InactiveExam_ReturnsNotFound()
    {
        _exam.Status = ActiveStatus.Inactive;

        var result = await Service().StartAsync(300);

        ResultAssert.Failed(result, ErrorType.NotFound, "Exam.NotFound");
    }

    [Fact]
    public async Task StartAsync_LessonQuiz_ReturnsNotFound()
    {
        _exam.Type = LearningData.LessonQuizType;

        var result = await Service().StartAsync(300);

        ResultAssert.Failed(result, ErrorType.NotFound, "Exam.NotFound");
    }

    [Fact]
    public async Task StartAsync_Guest_ReturnsAuthRequired()
    {
        var result = await Service(MockUser.Guest()).StartAsync(300);

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
    }
}
