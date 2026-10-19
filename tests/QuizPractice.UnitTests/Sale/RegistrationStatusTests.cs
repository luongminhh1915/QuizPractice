using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>
/// SALE-03: UC-61 Update Registration Status (RegistrationService.ChangeStatusAsync,
/// SaveNotesAsync). Each registration belongs to a customer account (project rule), so
/// Paid only opens the access: account creation (BR-15, step 6, E2) is out of scope.
/// </summary>
public class RegistrationStatusTests : RegistrationServiceFixture
{
    private static DateOnly Today => VietnamTime.Today(DateTime.UtcNow);

    private Registration Submitted(byte months = 6) =>
        Stored(TestData.Registration(7, package: TestData.Package(months: months)));

    [Fact]
    public async Task ChangeStatusAsync_SubmittedToPaid_SetsValidityFromThePackage()
    {
        var registration = Submitted(months: 6);

        var result = ResultAssert.Succeeded(await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, "Transfer found")));

        Assert.Equal(RegistrationStatus.Paid, registration.Status);
        Assert.Equal(Today, registration.ValidFrom);
        Assert.Equal(Today.AddMonths(6), registration.ValidTo);
        Assert.Equal(TestData.SaleId, registration.UpdatedById);
        Assert.Equal("Transfer found", registration.Notes);
        Assert.Equal(RegistrationStatus.Paid, result.Registration.Status);
        UnitOfWork.Verify(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_PaidForCustomerAccount_CreatesNoAccountAndSendsNoMail()
    {
        Submitted();

        var result = ResultAssert.Succeeded(await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null)));

        Assert.False(result.AccountCreated);
        Assert.False(result.MailFailed);
        VerifyNoAccountCreated();
        Users.Verify(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_PackageOfOneMonth_ValidForOneMonth()
    {
        var registration = Submitted(months: 1);

        await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        Assert.Equal(Today.AddMonths(1), registration.ValidTo);
    }

    [Fact]
    public async Task ChangeStatusAsync_PackageWithoutDuration_ReturnsNoDurationAndSavesNothing()
    {
        var registration = Submitted(months: 0);

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.NoDuration");
        Assert.Null(registration.ValidFrom);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task ChangeStatusAsync_SubmittedToCancelled_GrantsNoAccess()
    {
        var registration = Submitted();

        var result = ResultAssert.Succeeded(await Service().ChangeStatusAsync(7, new(RegistrationStatus.Cancelled, "Customer changed their mind")));

        Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
        Assert.Null(registration.ValidFrom);
        Assert.Null(registration.ValidTo);
        Assert.False(result.AccountCreated);
    }

    [Fact]
    public async Task ChangeStatusAsync_RegistrationInUnpaidInvoice_ReturnsInUnpaidInvoice()
    {
        var registration = TestData.Registration(7);
        TestData.InvoiceWith(InvoiceStatus.Unpaid, registration);
        Stored(registration);

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        var error = ResultAssert.Failed(result, ErrorType.Conflict, "Registration.InUnpaidInvoice");
        Assert.Contains("QP7K2M9X", error.Message);
        Assert.Equal(RegistrationStatus.Submitted, registration.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_RegistrationInCancelledInvoice_CanBePaid()
    {
        var registration = TestData.Registration(7);
        TestData.InvoiceWith(InvoiceStatus.Cancelled, registration);
        Stored(registration);

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatus.Paid, registration.Status);
    }

    [Theory]
    [InlineData(RegistrationStatus.Paid)]
    [InlineData(RegistrationStatus.Cancelled)]
    public async Task ChangeStatusAsync_AlreadySettled_ReturnsSettled(RegistrationStatus settled)
    {
        Stored(TestData.Registration(7, settled));

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.Settled");
        VerifyNothingSaved();
    }

    [Fact]
    public async Task ChangeStatusAsync_BackToSubmitted_ReturnsValidationError()
    {
        Submitted();

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Submitted, null));

        ResultAssert.Invalid(result, "Status");
    }

    [Fact]
    public async Task ChangeStatusAsync_UnknownRegistration_ReturnsNotFound()
    {
        var result = await Service().ChangeStatusAsync(404, new(RegistrationStatus.Paid, null));

        ResultAssert.Failed(result, ErrorType.NotFound, "Registration.NotFound");
    }

    [Fact]
    public async Task ChangeStatusAsync_SettledByAnotherSaleUserMeanwhile_ReturnsChanged()
    {
        Submitted();
        UnitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.Changed");
    }

    [Fact]
    public async Task ChangeStatusAsync_NotSignedIn_ReturnsUnauthorized()
    {
        Submitted();

        var result = await Service(MockUser.Guest()).ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
    }

    [Fact]
    public async Task ChangeStatusAsync_NotesOf500Characters_AreAccepted()
    {
        var registration = Submitted();

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, new string('n', 500)));

        Assert.True(result.IsSuccess);
        Assert.Equal(500, registration.Notes!.Length);
    }

    [Fact]
    public async Task ChangeStatusAsync_NotesOf501Characters_ReturnsValidationError()
    {
        Submitted();

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, new string('n', 501)));

        ResultAssert.Invalid(result, "Notes");
    }
}
