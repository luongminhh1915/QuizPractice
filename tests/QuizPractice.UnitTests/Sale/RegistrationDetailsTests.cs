using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>
/// SALE-02: UC-60 Add or Edit Registration (RegistrationService.CreateAsync, UpdateAsync,
/// GetAsync, GetChoicesAsync). Every registration is recorded for an existing Customer
/// account (project rule); the email-without-account case is out of scope.
/// </summary>
public class RegistrationDetailsTests : RegistrationServiceFixture
{
    private readonly User _customer = TestData.Customer();
    private readonly PricePackage _sixMonths = TestData.Package(100, months: 6, salePrice: 300_000m);
    private readonly PricePackage _twelveMonths = TestData.Package(101, months: 12, salePrice: 500_000m);
    private readonly PricePackage _retired = TestData.Package(102, months: 3, salePrice: 100_000m, status: ActiveStatus.Inactive);
    private readonly Subject _java;
    private Registration? _added;

    public RegistrationDetailsTests()
    {
        _java = TestData.Subject(1, "Java OOP", _twelveMonths, _sixMonths, _retired);
        Offered(_java);
        Account(_customer);

        // Add gives the new row an id and the navigation properties the reload would bring.
        Registrations.Setup(r => r.Add(It.IsAny<Registration>())).Callback<Registration>(r =>
        {
            r.Id = 99;
            r.Subject = _java;
            r.Package = _java.PricePackages.Single(p => p.Id == r.PackageId);
            r.CreatedBy = TestData.SaleUser(r.CreatedById!.Value);
            r.UpdatedBy = r.CreatedBy;
            _added = r;
        });
        Registrations.Setup(r => r.GetForSaleAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(() => _added);
    }

    private SaveRegistrationRequest Form(int packageId = 100, string email = "customer@gmail.com",
        string fullName = "Nguyen Van An", string mobile = "0912345678", int subjectId = 1) =>
        new(subjectId, packageId, fullName, Gender.Male, email, mobile, "Paid in cash at the office");

    // ------------------------------------------------------------------ Create

    [Fact]
    public async Task CreateAsync_ExistingCustomerEmail_StoresSubmittedRegistrationLinkedToTheAccount()
    {
        var dto = ResultAssert.Succeeded(await Service().CreateAsync(Form()));

        Assert.NotNull(_added);
        Assert.Equal(RegistrationStatus.Submitted, _added!.Status);
        Assert.Equal(_customer.Id, _added.UserId);
        Assert.Equal(TestData.SaleId, _added.CreatedById);
        Assert.Equal(TestData.SaleId, _added.UpdatedById);
        Assert.Equal(300_000m, _added.TotalCost);
        Assert.True(dto.HasAccount);
        Assert.True(dto.CanEditAll);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FieldsWithSpaces_AreTrimmed()
    {
        await Service().CreateAsync(Form(email: "  customer@gmail.com ", fullName: "  Nguyen Van An  "));

        Assert.Equal("customer@gmail.com", _added!.Email);
        Assert.Equal("Nguyen Van An", _added.FullName);
    }

    [Fact]
    public async Task CreateAsync_EmailOfStaffAccount_ReturnsValidationErrorOnEmail()
    {
        var expert = TestData.Expert();
        Account(expert);

        var result = await Service().CreateAsync(Form(email: expert.Email));

        ResultAssert.Invalid(result, "Email");
        Registrations.Verify(r => r.Add(It.IsAny<Registration>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SubjectNotOnSale_ReturnsValidationErrorOnSubject()
    {
        var result = await Service().CreateAsync(Form(subjectId: 2));

        ResultAssert.Invalid(result, "SubjectId");
        VerifyNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_InactivePackage_ReturnsPackageNotAvailable()
    {
        var result = await Service().CreateAsync(Form(packageId: _retired.Id));

        ResultAssert.Failed(result, ErrorType.Conflict, "Package.NotAvailable");
        VerifyNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_EmptyNameAndBadEmail_ReturnsValidationErrors()
    {
        var result = await Service().CreateAsync(Form(fullName: "", email: "customer.gmail.com"));

        ResultAssert.Invalid(result, "FullName");
        ResultAssert.Invalid(result, "Email");
        Registrations.Verify(r => r.ListOfferedSubjectsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("091234567")]   // 9 digits
    [InlineData("09123456789")] // 11 digits
    [InlineData("1912345678")]  // does not start with 0
    public async Task CreateAsync_MobileNotTenDigitsFromZero_ReturnsValidationError(string mobile)
    {
        var result = await Service().CreateAsync(Form(mobile: mobile));

        ResultAssert.Invalid(result, "Mobile");
    }

    [Fact]
    public async Task CreateAsync_FullNameOfTwoCharacters_IsAccepted()
    {
        var result = await Service().CreateAsync(Form(fullName: "An"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_NotSignedIn_ReturnsUnauthorized()
    {
        var result = await Service(MockUser.Guest()).CreateAsync(Form());

        ResultAssert.Failed(result, ErrorType.Unauthorized, "Auth.Required");
        VerifyNothingSaved();
    }

    // ------------------------------------------------------------------ Update

    [Fact]
    public async Task UpdateAsync_ByItsRecorderWhileSubmitted_SavesTheNewPackage()
    {
        var registration = Stored(TestData.Registration(7, package: _sixMonths, customer: _customer, createdById: TestData.SaleId));

        var dto = ResultAssert.Succeeded(await Service().UpdateAsync(7, Form(packageId: _twelveMonths.Id)));

        Assert.Equal(_twelveMonths.Id, registration.PackageId);
        Assert.Equal(500_000m, registration.TotalCost);
        Assert.Equal(TestData.SaleId, registration.UpdatedById);
        Assert.Equal(RegistrationStatus.Submitted, dto.Status);
        UnitOfWork.Verify(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RecordedByAnotherSaleUser_ReturnsNotEditable()
    {
        Stored(TestData.Registration(7, customer: _customer, createdById: TestData.OtherSaleId));

        var result = await Service().UpdateAsync(7, Form());

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.NotEditable");
        VerifyNothingSaved();
    }

    [Fact]
    public async Task UpdateAsync_OnlineRegistration_ReturnsNotEditable()
    {
        Stored(TestData.Registration(7, customer: _customer, createdById: null));

        var result = await Service().UpdateAsync(7, Form());

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.NotEditable");
    }

    [Fact]
    public async Task UpdateAsync_PaidRegistration_ReturnsNotEditable()
    {
        Stored(TestData.Registration(7, RegistrationStatus.Paid, customer: _customer, createdById: TestData.SaleId));

        var result = await Service().UpdateAsync(7, Form());

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.NotEditable");
    }

    [Fact]
    public async Task UpdateAsync_ChangedByAnotherUserMeanwhile_ReturnsChanged()
    {
        Stored(TestData.Registration(7, package: _sixMonths, customer: _customer, createdById: TestData.SaleId));
        UnitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Service().UpdateAsync(7, Form());

        ResultAssert.Failed(result, ErrorType.Conflict, "Registration.Changed");
    }

    [Fact]
    public async Task UpdateAsync_UnknownRegistration_ReturnsNotFound()
    {
        var result = await Service().UpdateAsync(404, Form());

        ResultAssert.Failed(result, ErrorType.NotFound, "Registration.NotFound");
    }
}
