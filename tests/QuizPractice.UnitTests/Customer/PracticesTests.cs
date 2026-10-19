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

/// <summary>CUS-04: UC-19 View Practices List, UC-20 Create Practice, UC-21 View Practice Details (PracticeService).</summary>
public class PracticesTests
{
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IQuizRepository> _quizzes = new();
    private readonly Mock<IAttemptRepository> _attempts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Subject _java = TestData.Subject(1, "Java OOP");
    private Attempt? _added;

    public PracticesTests()
    {
        LearningData.SetupLabels(_attempts);
        LearningData.SetupAccess(_registrations, _java);
        _attempts.Setup(a => a.GetShownQuestionIdsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3, 4, 5]);
        _attempts.Setup(a => a.Add(It.IsAny<Attempt>())).Callback<Attempt>(a => { a.Id = 900; _added = a; });
        _quizzes.Setup(q => q.GetTopicsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([LearningData.Topic(11, "Classes"), LearningData.Topic(12, "Inheritance")]);
        _quizzes.Setup(q => q.GetTopicMembersAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. LearningData.Members(11, 1, 2, 3), .. LearningData.Members(12, 4, 5)]);
    }

    private PracticeService Service(Mock<ICurrentUser>? user = null) =>
        new(_registrations.Object, _quizzes.Object, _attempts.Object, _unitOfWork.Object, (user ?? MockUser.Customer()).Object,
            new CreatePracticeRequestValidator(), new PracticeListRequestValidator());

    // ------------------------------------------------------------------ UC-20

    [Fact]
    public async Task GetFormAsync_ListsSubjectsReachableToday()
    {
        DateOnly? asked = null;
        _registrations.Setup(r => r.ListAccessibleSubjectsAsync(TestData.CustomerId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Callback<int, DateOnly, CancellationToken>((_, day, _) => asked = day)
            .ReturnsAsync([_java]);

        var form = ResultAssert.Succeeded(await Service().GetFormAsync());

        Assert.Equal(["Java OOP"], form.Subjects.Select(s => s.Name));
        Assert.Equal(VietnamTime.Today(DateTime.UtcNow), asked); // BR-05 is checked on the Vietnam day
    }

    [Fact]
    public async Task GetFormAsync_NoValidRegistration_ReturnsEmptyList()
    {
        LearningData.SetupAccess(_registrations);

        var form = ResultAssert.Succeeded(await Service().GetFormAsync());

        Assert.Empty(form.Subjects);
    }

    [Fact]
    public async Task GetGroupsAsync_ByTopic_CountsShownQuestionsOfEachTopic()
    {
        var groups = ResultAssert.Succeeded(await Service().GetGroupsAsync(1, SelectionType.Topic));

        Assert.Equal(5, groups.Total);
        Assert.Equal([3, 2], groups.Groups.Select(g => g.Available));
    }

    [Fact]
    public async Task GetGroupsAsync_UndefinedSelection_ReturnsValidationError()
    {
        var result = await Service().GetGroupsAsync(1, (SelectionType)7);

        ResultAssert.Invalid(result, "SelectedBy");
    }

    [Fact]
    public async Task CreateAsync_FromAllTopics_DrawsTheAskedNumberAndStartsTheAttempt()
    {
        var created = ResultAssert.Succeeded(await Service().CreateAsync(new CreatePracticeRequest(1, 3, SelectionType.Topic, null)));

        Assert.Equal(900, created.Id);
        Assert.Equal(AttemptMode.Practice, _added!.Mode);
        Assert.Equal(AttemptStatus.InProgress, _added.Status);
        Assert.Equal([1, 2, 3], _added.Questions.Select(q => (int)q.DisplayOrder));
        Assert.Equal(3, _added.Questions.Select(q => q.QuestionId).Distinct().Count());
        Assert.All(_added.Questions, q => Assert.InRange(q.QuestionId, 1, 5));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FromOneTopic_DrawsOnlyItsQuestions()
    {
        ResultAssert.Succeeded(await Service().CreateAsync(new CreatePracticeRequest(1, 2, SelectionType.Topic, 12)));

        Assert.Equal([4, 5], _added!.Questions.Select(q => q.QuestionId).Order());
        Assert.Equal(12, _added.GroupId);
    }

    [Fact]
    public async Task CreateAsync_CountEqualToAvailableQuestions_IsAccepted()
    {
        var result = await Service().CreateAsync(new CreatePracticeRequest(1, 5, SelectionType.Topic, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, _added!.Questions.Count);
    }

    [Fact]
    public async Task CreateAsync_MoreThanAvailableQuestions_ReturnsValidationErrorAndCreatesNothing()
    {
        var result = await Service().CreateAsync(new CreatePracticeRequest(1, 6, SelectionType.Topic, null));

        var error = ResultAssert.Failed(result, ErrorType.Validation, "Validation");
        Assert.Contains("Only 5 questions", error.Fields!["QuestionCount"][0]);
        _attempts.Verify(a => a.Add(It.IsAny<Attempt>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task CreateAsync_CountOutsideOneToHundred_ReturnsValidationError(int count)
    {
        var result = await Service().CreateAsync(new CreatePracticeRequest(1, count, SelectionType.Topic, null));

        ResultAssert.Invalid(result, "QuestionCount");
    }

    [Fact]
    public async Task CreateAsync_GroupOfAnotherSubject_ReturnsValidationError()
    {
        var result = await Service().CreateAsync(new CreatePracticeRequest(1, 1, SelectionType.Topic, 99));

        ResultAssert.Invalid(result, "GroupId");
    }

    [Fact]
    public async Task CreateAsync_AccessEndedSinceTheFormLoaded_ReturnsNoAccess()
    {
        LearningData.SetupAccess(_registrations);

        var result = await Service().CreateAsync(new CreatePracticeRequest(1, 3, SelectionType.Topic, null));

        ResultAssert.Failed(result, ErrorType.Forbidden, "Subject.NoAccess");
        _attempts.Verify(a => a.Add(It.IsAny<Attempt>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Guest_ReturnsAuthRequired()
    {
        var result = await Service(MockUser.Guest()).CreateAsync(new CreatePracticeRequest(1, 3, SelectionType.Topic, null));

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
    }

    // ------------------------------------------------------------------ UC-19, UC-21

    [Fact]
    public async Task ListAsync_FilteredBySubject_PassesTheCriteriaAndLabelsPractices()
    {
        AttemptSearch? search = null;
        var attempt = LearningData.Attempt(900, questions: LearningData.Question(1));
        _attempts.Setup(a => a.ListByUserAsync(TestData.CustomerId, It.IsAny<AttemptSearch>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<int, AttemptSearch, PageRequest, CancellationToken>((_, s, _, _) => search = s)
            .ReturnsAsync(new Page<Attempt>([attempt], 1, 1, 10));

        var page = ResultAssert.Succeeded(await Service().ListAsync(new PracticeListRequest(SubjectId: 1)));

        Assert.Equal(1, search!.SubjectId);
        var row = Assert.Single(page.Items);
        Assert.Equal("Practice by topic: All topics", row.Title);
        Assert.Null(row.Outcome);
    }

    [Fact]
    public async Task GetAsync_ScoredPractice_ShowsTheOutcome()
    {
        var q1 = LearningData.Question(1);
        var q2 = LearningData.Question(2);
        var attempt = LearningData.Attempt(900, questions: [q1, q2]);
        attempt.Questions[0].ChosenOptionId = LearningData.CorrectOptionOf(q1);
        attempt.Status = AttemptStatus.Submitted;
        attempt.Score = 50;
        attempt.PassRate = 50;
        attempt.FinishedAt = attempt.StartedAt.AddMinutes(3);
        _attempts.Setup(a => a.GetOwnAsync(900, TestData.CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);

        var dto = ResultAssert.Succeeded(await Service().GetAsync(900));

        Assert.Equal(1, dto.Outcome!.Correct);
        Assert.True(dto.Outcome.Passed); // 50 % at a pass rate of 50 % passes (BR-21)
        Assert.Equal(180, dto.Outcome.DurationSeconds);
    }

    [Fact]
    public async Task GetAsync_AnotherCustomersAttempt_ReturnsNotFound()
    {
        var result = await Service().GetAsync(901);

        ResultAssert.Failed(result, ErrorType.NotFound, "Practice.NotFound");
    }
}
