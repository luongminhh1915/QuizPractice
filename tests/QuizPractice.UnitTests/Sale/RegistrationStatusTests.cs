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
    public async Task ChangeStatusAsync_RegistrationInCancelledInvoice_CanBePaid()
    {
        var registration = TestData.Registration(7);
        TestData.InvoiceWith(InvoiceStatus.Cancelled, registration);
        Stored(registration);

        var result = await Service().ChangeStatusAsync(7, new(RegistrationStatus.Paid, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(RegistrationStatus.Paid, registration.Status);
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
