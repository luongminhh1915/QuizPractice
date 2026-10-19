using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.UnitTests.Common;

/// <summary>
/// Entity builders with every navigation property the DTO mappers read, so a test
/// fails on its assertion and not on a NullReferenceException.
/// </summary>
public static class TestData
{
    public const int CustomerId = 10;
    public const int OtherCustomerId = 11;
    public const int SaleId = 4;
    public const int OtherSaleId = 5;

    public static readonly Setting CustomerRole = new() { Id = 1, Type = SettingType.UserRole, Value = Roles.Customer };
    public static readonly Setting SaleRole = new() { Id = 4, Type = SettingType.UserRole, Value = Roles.Sale };
    public static readonly Setting ExpertRole = new() { Id = 2, Type = SettingType.UserRole, Value = Roles.Expert };

    public static User Customer(int id = CustomerId, string email = "customer@gmail.com") => new()
    {
        Id = id,
        Email = email,
        FullName = "Nguyen Van An",
        Gender = Gender.Male,
        Mobile = "0912345678",
        RoleId = CustomerRole.Id,
        Role = CustomerRole,
        Status = UserStatus.Active,
    };

    public static User SaleUser(int id = SaleId, string email = "sale@gmail.com") => new()
    {
        Id = id,
        Email = email,
        FullName = "Tran Thi Sale",
        Gender = Gender.Female,
        Mobile = "0987654321",
        RoleId = SaleRole.Id,
        Role = SaleRole,
        Status = UserStatus.Active,
    };

    public static User Expert(int id = 2, string email = "expert@gmail.com") => new()
    {
        Id = id,
        Email = email,
        FullName = "Le Van Expert",
        Mobile = "0911111111",
        RoleId = ExpertRole.Id,
        Role = ExpertRole,
        Status = UserStatus.Active,
    };

    public static PricePackage Package(int id = 100, byte months = 6, decimal salePrice = 300_000m,
        ActiveStatus status = ActiveStatus.Active, int subjectId = 1) => new()
    {
        Id = id,
        SubjectId = subjectId,
        Name = $"{months} months",
        AccessMonths = months,
        ListPrice = salePrice + 100_000m,
        SalePrice = salePrice,
        Status = status,
    };

    public static Subject Subject(int id = 1, string name = "Java Object-Oriented Programming", params PricePackage[] packages)
    {
        var subject = new Subject
        {
            Id = id,
            Name = name,
            Status = SubjectStatus.Published,
            PricePackages = packages.Length > 0 ? [.. packages] : [Package(subjectId: id)],
        };
        foreach (var p in subject.PricePackages)
        {
            p.SubjectId = id;
            p.Subject = subject;
        }

        return subject;
    }

    /// <summary>A registration of a customer who has an account (the project rule: no guest buys).</summary>
    public static Registration Registration(
        int id = 7,
        RegistrationStatus status = RegistrationStatus.Submitted,
        PricePackage? package = null,
        User? customer = null,
        int? createdById = SaleId,
        Invoice? invoice = null)
    {
        package ??= Package();
        customer ??= Customer();
        var subject = package.Subject ?? Subject(package.SubjectId, packages: package);
        var updatedBy = createdById is { } c && c != customer.Id ? SaleUser(c) : customer;
        return new Registration
        {
            Id = id,
            SubjectId = subject.Id,
            Subject = subject,
            PackageId = package.Id,
            Package = package,
            UserId = customer.Id,
            User = customer,
            CreatedById = createdById,
            CreatedBy = createdById is { } by ? SaleUser(by, $"sale{by}@gmail.com") : null,
            UpdatedById = updatedBy.Id,
            UpdatedBy = updatedBy,
            InvoiceId = invoice?.Id,
            Invoice = invoice,
            FullName = customer.FullName,
            Email = customer.Email,
            Mobile = customer.Mobile,
            Gender = customer.Gender,
            TotalCost = package.SalePrice,
            Status = status,
            RegisteredAt = new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc),
        };
    }

    public static Invoice Invoice(int id = 50, InvoiceStatus status = InvoiceStatus.Unpaid, User? customer = null, string code = "QP7K2M9X")
    {
        customer ??= Customer();
        return new Invoice
        {
            Id = id,
            UserId = customer.Id,
            User = customer,
            Code = code,
            BankName = "Vietcombank",
            BankBin = "970436",
            AccountNumber = "0000000000",
            AccountName = "QUIZ PRACTICE",
            Status = status,
            CreatedAt = new DateTime(2026, 10, 2, 3, 0, 0, DateTimeKind.Utc),
        };
    }

    /// <summary>An invoice holding the given registrations, its total their sum.</summary>
    public static Invoice InvoiceWith(InvoiceStatus status, params Registration[] registrations)
    {
        var invoice = Invoice(status: status, customer: registrations[0].User);
        foreach (var r in registrations)
        {
            r.InvoiceId = invoice.Id;
            r.Invoice = invoice;
            invoice.Registrations.Add(r);
        }

        invoice.TotalAmount = registrations.Sum(r => r.TotalCost);
        return invoice;
    }

    public static BankAccount Bank() => new()
    {
        BankName = "Vietcombank",
        BankBin = "970436",
        AccountNumber = "0000000000",
        AccountName = "QUIZ PRACTICE",
    };
}
