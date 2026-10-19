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
}
