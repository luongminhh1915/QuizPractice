using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Constants;

namespace QuizPractice.UnitTests.Common;

/// <summary>
/// ICurrentUser mocks. IsInRole and IsAuthenticated are default interface members, which
/// Moq does not run by itself, so they are set up here for every mock.
/// </summary>
public static class MockUser
{
    public static Mock<ICurrentUser> Guest() => Of(null, null, null);

    public static Mock<ICurrentUser> Customer(int id = TestData.CustomerId, string email = "customer@gmail.com") =>
        Of(id, email, Roles.Customer);

    public static Mock<ICurrentUser> Sale(int id = TestData.SaleId, string email = "sale@gmail.com") =>
        Of(id, email, Roles.Sale);

    public static Mock<ICurrentUser> Of(int? id, string? email, string? role)
    {
        var user = new Mock<ICurrentUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.Email).Returns(email);
        user.SetupGet(u => u.Role).Returns(role);
        user.SetupGet(u => u.IsAuthenticated).Returns(id is not null);
        user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(r => r == role);
        return user;
    }
}
