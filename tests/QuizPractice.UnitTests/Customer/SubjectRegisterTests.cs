using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Services.Public;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Customer;

/// <summary>
/// CUS-01: UC-14 Register Subject (SubjectRegisterService). Project rule: only a signed-in
/// Customer with an account buys a subject; a guest is refused.
/// </summary>
public class SubjectRegisterTests
{
    private readonly Mock<ISubjectRepository> _subjects = new();
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly User _customer = TestData.Customer();
    private readonly PricePackage _sixMonths = TestData.Package(100, months: 6, salePrice: 300_000m);
    private readonly PricePackage _retired = TestData.Package(102, months: 3, status: ActiveStatus.Inactive);
    private Registration? _added;

    public SubjectRegisterTests()
    {
        var java = TestData.Subject(1, "Java OOP", _sixMonths, _retired);
        _subjects.Setup(s => s.GetPublishedAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(java);
        _users.Setup(u => u.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
        _registrations.Setup(r => r.Add(It.IsAny<Registration>())).Callback<Registration>(r => _added = r);
    }

    private SubjectRegisterService Service(Mock<ICurrentUser>? user = null) =>
        new(_subjects.Object, _registrations.Object, _users.Object, _unitOfWork.Object,
            (user ?? MockUser.Customer()).Object, new RegisterRequestValidator());

    [Fact]
    public async Task RegisterAsync_CustomerWithCompleteProfile_StoresSubmittedRegistration()
    {
        var dto = ResultAssert.Succeeded(await Service().RegisterAsync(1, new SubjectRegisterRequest(100)));

        Assert.Equal(RegistrationStatus.Submitted, dto.Status);
        Assert.Equal(300_000m, dto.TotalCost);
        Assert.Equal(_customer.Id, _added!.UserId);
        Assert.Null(_added.CreatedById);
        Assert.Equal(_customer.Email, _added.Email);
        Assert.Equal(_customer.Mobile, _added.Mobile);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFormAsync_WithSubmittedRegistration_ShowsTheExistingOne()
    {
        var existing = TestData.Registration(7, package: _sixMonths, customer: _customer, createdById: null);
        _registrations.Setup(r => r.FindSubmittedAsync(_customer.Id, 1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var form = ResultAssert.Succeeded(await Service().GetFormAsync(1));

        Assert.Equal(7, form.Existing!.Id);
        Assert.Equal([100], form.Packages.Select(p => p.Id));
        Assert.Equal("customer@gmail.com", form.Contact.Email);
        Assert.Empty(form.ContactProblems);
    }

    [Fact]
    public async Task GetFormAsync_ProfileWithoutMobile_ListsTheProblem()
    {
        _customer.Mobile = "";

        var form = ResultAssert.Succeeded(await Service().GetFormAsync(1));

        Assert.Contains("Mobile", form.ContactProblems.Keys);
    }

    [Fact]
    public async Task RegisterAsync_Guest_ReturnsAuthRequired()
    {
        var result = await Service(MockUser.Guest()).RegisterAsync(1, new SubjectRegisterRequest(100));

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
        _registrations.Verify(r => r.Add(It.IsAny<Registration>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_SignedInAsSale_ReturnsCustomerOnly()
    {
        var result = await Service(MockUser.Sale()).RegisterAsync(1, new SubjectRegisterRequest(100));

        ResultAssert.Failed(result, ErrorType.Forbidden, "Registration.CustomerOnly");
    }

    [Fact]
    public async Task RegisterAsync_SubjectNotPublished_ReturnsNotAvailable()
    {
        var result = await Service().RegisterAsync(2, new SubjectRegisterRequest(100));

        ResultAssert.Failed(result, ErrorType.NotFound, "Subject.NotAvailable");
    }

    [Fact]
    public async Task RegisterAsync_AlreadySubmittedForTheSubject_ReturnsAlreadySubmitted()
    {
        _registrations.Setup(r => r.FindSubmittedAsync(_customer.Id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Registration(7, package: _sixMonths, customer: _customer));

        var result = await Service().RegisterAsync(1, new SubjectRegisterRequest(100));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.AlreadySubmitted");
        _registrations.Verify(r => r.Add(It.IsAny<Registration>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_InactivePackage_ReturnsPackageNotAvailable()
    {
        var result = await Service().RegisterAsync(1, new SubjectRegisterRequest(_retired.Id));

        ResultAssert.Failed(result, ErrorType.Conflict, "Package.NotAvailable");
    }

    [Fact]
    public async Task RegisterAsync_ProfileWithoutMobile_ReturnsProfileIncompleteAndStoresNothing()
    {
        _customer.Mobile = "";

        var result = await Service().RegisterAsync(1, new SubjectRegisterRequest(100));

        var error = ResultAssert.Failed(result, ErrorType.Validation, "Profile.Incomplete");
        Assert.Contains("Mobile", error.Fields!.Keys);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(2)]   // the minimum
    [InlineData(100)] // the maximum
    public async Task RegisterAsync_FullNameAtTheLengthLimits_IsAccepted(int length)
    {
        _customer.FullName = new string('a', length);

        var result = await Service().RegisterAsync(1, new SubjectRegisterRequest(100));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RegisterAsync_FullNameOf101Characters_ReturnsProfileIncomplete()
    {
        _customer.FullName = new string('a', 101);

        var result = await Service().RegisterAsync(1, new SubjectRegisterRequest(100));

        var error = ResultAssert.Failed(result, ErrorType.Validation, "Profile.Incomplete");
        Assert.Contains("FullName", error.Fields!.Keys);
    }
}
