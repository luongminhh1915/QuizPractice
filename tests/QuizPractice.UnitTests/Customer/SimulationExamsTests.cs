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
}
