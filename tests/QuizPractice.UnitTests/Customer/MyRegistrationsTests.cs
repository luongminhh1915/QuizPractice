using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Customer;

/// <summary>
/// CUS-02: UC-15 View My Registrations, UC-16 Edit Submitted Registration, UC-17 Cancel
/// Submitted Registration (MyRegistrationService).
/// </summary>
public class MyRegistrationsTests
{
    private readonly Mock<IRegistrationRepository> _registrations = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly User _customer = TestData.Customer();
    private readonly PricePackage _sixMonths = TestData.Package(100, months: 6, salePrice: 300_000m);
    private readonly PricePackage _twelveMonths = TestData.Package(101, months: 12, salePrice: 500_000m);
    private readonly PricePackage _retired = TestData.Package(102, months: 3, status: ActiveStatus.Inactive);
    private readonly Registration _registration;

    public MyRegistrationsTests()
    {
        TestData.Subject(1, "Java OOP", _sixMonths, _twelveMonths, _retired);
        _registration = TestData.Registration(7, package: _sixMonths, customer: _customer, createdById: null);
        _registrations.Setup(r => r.GetOwnAsync(7, _customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_registration);
        _users.Setup(u => u.GetByIdAsync(_customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_customer);
        _unitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private MyRegistrationService Service(Mock<ICurrentUser>? user = null) =>
        new(_registrations.Object, _users.Object, _unitOfWork.Object, (user ?? MockUser.Customer()).Object, new RegisterRequestValidator());

    // ------------------------------------------------------------------ UC-15

    [Fact]
    public async Task ListAsync_CustomerWithRegistrations_ReturnsOwnRegistrations()
    {
        var paid = TestData.Registration(8, RegistrationStatus.Paid, customer: _customer);
        _registrations.Setup(r => r.ListByUserAsync(_customer.Id, It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page<Registration>([_registration, paid], 2, 1, 10));

        var page = ResultAssert.Succeeded(await Service().ListAsync(new MyRegistrationListRequest()));

        Assert.Equal([7, 8], page.Items.Select(r => r.Id));
        Assert.True(page.Items[0].CanChange);
        Assert.False(page.Items[1].CanChange);
    }

    [Fact]
    public async Task ListAsync_NoRegistrationYet_ReturnsEmptyPage()
    {
        _registrations.Setup(r => r.ListByUserAsync(_customer.Id, It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page<Registration>([], 0, 1, 10));

        var page = ResultAssert.Succeeded(await Service().ListAsync(new MyRegistrationListRequest()));

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task ListAsync_RegistrationInUnpaidInvoice_ShowsTheInvoiceAndCannotChange()
    {
        TestData.InvoiceWith(InvoiceStatus.Unpaid, _registration);
        _registrations.Setup(r => r.ListByUserAsync(_customer.Id, It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page<Registration>([_registration], 1, 1, 10));

        var row = Assert.Single(ResultAssert.Succeeded(await Service().ListAsync(new MyRegistrationListRequest())).Items);

        Assert.Equal("QP7K2M9X", row.UnpaidInvoice!.Code);
        Assert.False(row.CanChange);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 101)]
    public async Task ListAsync_PageOutOfRange_ReturnsValidationError(int page, int pageSize)
    {
        var result = await Service().ListAsync(new MyRegistrationListRequest(page, pageSize));

        ResultAssert.Invalid(result, "Page");
    }

    [Fact]
    public async Task ListAsync_Guest_ReturnsAuthRequired()
    {
        var result = await Service(MockUser.Guest()).ListAsync(new MyRegistrationListRequest());

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
    }

    // ------------------------------------------------------------------ UC-16

    [Fact]
    public async Task GetChangeFormAsync_OwnSubmittedRegistration_PreselectsTheStoredPackage()
    {
        var form = ResultAssert.Succeeded(await Service().GetChangeFormAsync(7));

        Assert.Equal(100, form.SelectedPackageId);
        Assert.Equal([100, 101], form.Packages.Select(p => p.Id));
    }

    [Fact]
    public async Task GetChangeFormAsync_AnotherCustomersRegistration_ReturnsNotFound()
    {
        var result = await Service().GetChangeFormAsync(99);

        ResultAssert.Failed(result, ErrorType.NotFound, "Registration.NotFound");
    }

    [Fact]
    public async Task ChangeAsync_AnotherActivePackage_UpdatesCostAndCopiesTheProfileAgain()
    {
        _customer.Mobile = "0900000000";

        var dto = ResultAssert.Succeeded(await Service().ChangeAsync(7, new ChangeRegistrationRequest(101)));

        Assert.Equal(101, dto.PackageId);
        Assert.Equal(500_000m, dto.TotalCost);
        Assert.Equal(RegistrationStatus.Submitted, dto.Status);
        Assert.Equal("0900000000", _registration.Mobile);
        _unitOfWork.Verify(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeAsync_InactivePackage_ReturnsPackageNotAvailable()
    {
        var result = await Service().ChangeAsync(7, new ChangeRegistrationRequest(_retired.Id));

        ResultAssert.Failed(result, ErrorType.Conflict, "Package.NotAvailable");
    }

    [Fact]
    public async Task ChangeAsync_PaidRegistration_ReturnsSettled()
    {
        _registration.Status = RegistrationStatus.Paid;

        var result = await Service().ChangeAsync(7, new ChangeRegistrationRequest(101));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.Settled");
    }

    [Fact]
    public async Task ChangeAsync_InUnpaidInvoice_ReturnsInUnpaidInvoice()
    {
        TestData.InvoiceWith(InvoiceStatus.Unpaid, _registration);

        var result = await Service().ChangeAsync(7, new ChangeRegistrationRequest(101));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.InUnpaidInvoice");
    }

    [Fact]
    public async Task ChangeAsync_ProfileWithBadMobile_ReturnsProfileIncomplete()
    {
        _customer.Mobile = "12345";

        var result = await Service().ChangeAsync(7, new ChangeRegistrationRequest(101));

        ResultAssert.Failed(result, ErrorType.Validation, "Profile.Incomplete");
        Assert.Equal(100, _registration.PackageId);
    }

    [Fact]
    public async Task ChangeAsync_SettledBySaleMeanwhile_ReturnsSettled()
    {
        _unitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Service().ChangeAsync(7, new ChangeRegistrationRequest(101));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.Settled");
    }
}
