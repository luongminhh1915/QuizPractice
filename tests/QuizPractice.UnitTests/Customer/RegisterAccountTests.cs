using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Application.Services.Common;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Customer;

/// <summary>
/// CUS-08: UC-02 Register Account, UC-03 Verify Email (AccountService.RegisterAsync,
/// VerifyEmailAsync). Project rule: a customer needs this account before buying a subject.
/// </summary>
public class RegisterAccountTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISettingRepository> _settings = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IEmailSender> _mail = new();
    private User? _added;

    public RegisterAccountTests()
    {
        _settings.Setup(s => s.ListActiveAsync(SettingType.UserRole, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.CustomerRole, TestData.SaleRole]);
        _users.Setup(u => u.Add(It.IsAny<User>())).Callback<User>(u => { u.Id = 42; _added = u; });
        _tokens.Setup(t => t.Create(LinkPurpose.VerifyEmail, 42, string.Empty)).Returns("verify-token");
        _mail.Setup(m => m.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mail.Setup(m => m.SendLoginPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
    }

    private AccountService Service() =>
        new(_users.Object, _settings.Object, _unitOfWork.Object, Mock.Of<IFileStorage>(), MockUser.Guest().Object,
            Mock.Of<IPermissionTable>(), _hasher.Object, _tokens.Object, _mail.Object,
            new LoginRequestValidator(), new RegisterRequestValidator(), new PasswordResetLinkRequestValidator(),
            new ResetPasswordRequestValidator(), new SaveProfileRequestValidator(), new ChangePasswordRequestValidator(),
            new SendVerificationLinkRequestValidator());

    private static RegisterRequest Form(string email = "new.customer@gmail.com", string fullName = "Pham Minh Khoa", string mobile = "0912345678") =>
        new(fullName, Gender.Male, email, mobile);

    // ------------------------------------------------------------------ UC-02

    [Fact]
    public async Task RegisterAsync_NewEmail_StoresUnverifiedCustomerAndMailsTheLink()
    {
        var dto = ResultAssert.Succeeded(await Service().RegisterAsync(Form(email: " new.customer@gmail.com ")));

        Assert.Equal("new.customer@gmail.com", dto.Email);
        Assert.Equal(UserStatus.Unverified, _added!.Status);
        Assert.Equal(TestData.CustomerRole.Id, _added.RoleId);
        Assert.Null(_added.PasswordHash); // the password comes with verification (UC-03)
        _mail.Verify(m => m.SendVerificationLinkAsync("new.customer@gmail.com", "Pham Minh Khoa", "verify-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyRegistered_ReturnsEmailTaken()
    {
        _users.Setup(u => u.GetByEmailAsync("customer@gmail.com", It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());

        var result = await Service().RegisterAsync(Form(email: "customer@gmail.com"));

        ResultAssert.Failed(result, ErrorType.Conflict, "Account.EmailTaken");
        _users.Verify(u => u.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_InvalidFields_ReturnsValidationErrors()
    {
        var result = await Service().RegisterAsync(new RegisterRequest("", null, "not-an-email", "12345"));

        ResultAssert.Invalid(result, "FullName");
        ResultAssert.Invalid(result, "Gender");
        ResultAssert.Invalid(result, "Email");
        ResultAssert.Invalid(result, "Mobile");
    }

    [Fact]
    public async Task RegisterAsync_EmailOf101Characters_ReturnsValidationError()
    {
        var email = new string('a', 91) + "@gmail.com"; // 101 characters

        var result = await Service().RegisterAsync(Form(email: email));

        ResultAssert.Invalid(result, "Email");
    }

    [Fact]
    public async Task RegisterAsync_MailCannotBeSent_KeepsTheAccountAndReportsMailNotSent()
    {
        _mail.Setup(m => m.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Service().RegisterAsync(Form());

        ResultAssert.Failed(result, ErrorType.ExternalFailure, "Mail.NotSent");
        Assert.NotNull(_added);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
