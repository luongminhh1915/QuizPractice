using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Services.Sale;
using QuizPractice.Application.Validators.Sale;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;
using QuizPractice.UnitTests.Common;

namespace QuizPractice.UnitTests.Sale;

/// <summary>The mocks every RegistrationService test starts from (UC-59 to UC-61).</summary>
public abstract class RegistrationServiceFixture
{
    protected readonly Mock<IRegistrationRepository> Registrations = new();
    protected readonly Mock<IQuizRepository> Quizzes = new();
    protected readonly Mock<IUserRepository> Users = new();
    protected readonly Mock<ISettingRepository> Settings = new();
    protected readonly Mock<IPasswordHasher> Hasher = new();
    protected readonly Mock<IEmailSender> Mail = new();
    protected readonly Mock<IUnitOfWork> UnitOfWork = new();

    protected RegistrationServiceFixture()
    {
        UnitOfWork.Setup(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    protected RegistrationService Service(Mock<ICurrentUser>? user = null) =>
        new(Registrations.Object, Quizzes.Object, Users.Object, Settings.Object, Hasher.Object, Mail.Object,
            UnitOfWork.Object, (user ?? MockUser.Sale()).Object,
            new RegistrationListRequestValidator(), new SaveRegistrationRequestValidator(),
            new SaveNotesRequestValidator(), new ChangeRegistrationStatusRequestValidator());

    /// <summary>The registration GetForSaleAsync(id) returns, before and after the save.</summary>
    protected Registration Stored(Registration registration)
    {
        Registrations.Setup(r => r.GetForSaleAsync(registration.Id, It.IsAny<CancellationToken>())).ReturnsAsync(registration);
        return registration;
    }

    protected void Offered(params Subject[] subjects) =>
        Registrations.Setup(r => r.ListOfferedSubjectsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(subjects);

    protected void Account(User user) =>
        Users.Setup(u => u.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    protected void VerifyNothingSaved()
    {
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        UnitOfWork.Verify(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Project rule: settling a registration never creates an account or mails a password.</summary>
    protected void VerifyNoAccountCreated()
    {
        Users.Verify(u => u.Add(It.IsAny<User>()), Times.Never);
        Mail.Verify(m => m.SendAccountCreatedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
