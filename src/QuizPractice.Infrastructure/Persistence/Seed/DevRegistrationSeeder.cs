using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Seed;

/// <summary>
/// Sample customers and registrations over the last two weeks, for the Dashboard (UC-34)
/// and the Sale screens. Written while the Registrations table is empty, so a database
/// seeded earlier gets them too; subjects, packages and the Sale user are found by name.
/// Days follow Vietnam time (UTC+7), as the Dashboard counts them. The sample customer
/// gets a paid Java registration, the one behind the seeded exam attempt.
/// </summary>
internal static class DevRegistrationSeeder
{
    private const string Java = "Java Object-Oriented Programming";
    private const string Sql = "SQL Fundamentals";

    /// <summary>New customers: email, name, gender, mobile, days ago the account was created.</summary>
    private static readonly (string Email, string Name, Gender Gender, string Mobile, int DaysAgo)[] Customers =
    [
        ("an.tran@qps.vn", "Trần Bình An", Gender.Male, "0901000006", 13),
        ("linh.vu@qps.vn", "Vũ Khánh Linh", Gender.Female, "0901000007", 11),
        ("huy.hoang@qps.vn", "Hoàng Gia Huy", Gender.Male, "0901000008", 8),
        ("thao.ngo@qps.vn", "Ngô Phương Thảo", Gender.Female, "0901000009", 7),
        ("khoa.dang@qps.vn", "Đặng Anh Khoa", Gender.Male, "0901000010", 2),
    ];

    /// <summary>
    /// Days ago and local hour it was registered, customer (index in Customers), subject,
    /// package months, status, days after registering it became Paid, recorded by Sale.
    /// </summary>
    private static readonly (int DaysAgo, int Hour, int Customer, string Subject, int Months, RegistrationStatus Status, int PaidAfter, bool BySale)[] Rows =
    [
        (13, 9, 0, Java, 3, RegistrationStatus.Paid, 0, false),
        (13, 15, 0, Sql, 3, RegistrationStatus.Paid, 1, false),
        (12, 10, 0, Java, 6, RegistrationStatus.Cancelled, 0, false),
        (11, 20, 1, Java, 3, RegistrationStatus.Paid, 0, true),
        (10, 8, 1, Sql, 12, RegistrationStatus.Paid, 1, false),
        (9, 14, 1, Sql, 3, RegistrationStatus.Submitted, 0, false),
        (8, 11, 2, Java, 12, RegistrationStatus.Paid, 0, false),
        (7, 19, 2, Sql, 3, RegistrationStatus.Cancelled, 0, false),
        (6, 9, 3, Java, 3, RegistrationStatus.Paid, 1, true),
        (6, 16, 0, Java, 12, RegistrationStatus.Paid, 0, false),
        (5, 10, 3, Sql, 12, RegistrationStatus.Submitted, 0, false),
        (4, 13, 1, Java, 6, RegistrationStatus.Paid, 1, false),
        (3, 9, 2, Java, 12, RegistrationStatus.Cancelled, 0, false),
        (3, 18, 2, Sql, 12, RegistrationStatus.Paid, 0, true),
        (2, 10, 4, Java, 3, RegistrationStatus.Paid, 0, false),
        (1, 8, 4, Sql, 3, RegistrationStatus.Submitted, 0, false),
        (1, 21, 3, Java, 6, RegistrationStatus.Paid, 0, false),
        (0, 8, 0, Sql, 12, RegistrationStatus.Submitted, 0, false),
        (0, 9, 1, Java, 3, RegistrationStatus.Submitted, 0, true),
    ];

    private static readonly TimeSpan Vietnam = TimeSpan.FromHours(7);

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, ILogger logger, DateTime utcNow, CancellationToken ct = default)
    {
        if (await db.Registrations.AnyAsync(ct))
        {
            return;
        }

        var subjects = await db.Subjects.Include(s => s.PricePackages)
            .Where(s => s.Name == Java || s.Name == Sql).ToDictionaryAsync(s => s.Name, ct);
        var sale = await db.Users.FirstOrDefaultAsync(u => u.Email == DevSeedData.SaleEmail, ct);
        var sample = await db.Users.FirstOrDefaultAsync(u => u.Email == DevSeedData.CustomerEmail, ct);
        var customerRole = await db.Settings.FirstOrDefaultAsync(s => s.Type == SettingType.UserRole && s.Value == Roles.Customer, ct);
        if (subjects.Count < 2 || sale is null || sample is null || customerRole is null)
        {
            logger.LogWarning("Sample registrations not seeded: a sample subject, the Sale or customer account, or the Customer role is missing.");
            return;
        }

        var todayLocal = (utcNow + Vietnam).Date;
        DateTime LocalToUtc(DateTime local) => local - Vietnam;

        // New customers, or the ones a previous run left (emails are unique).
        var passwordHash = hasher.Hash(DevSeedData.Password);
        var customers = new List<User>();
        foreach (var c in Customers)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == c.Email, ct);
            if (user is null)
            {
                user = new User
                {
                    Email = c.Email,
                    PasswordHash = passwordHash,
                    FullName = c.Name,
                    Gender = c.Gender,
                    Mobile = c.Mobile,
                    Role = customerRole,
                    Status = UserStatus.Active,
                    CreatedAt = LocalToUtc(todayLocal.AddDays(-c.DaysAgo).AddHours(7)),
                };
                db.Users.Add(user);
            }

            customers.Add(user);
        }

        var registrations = new List<Registration>();

        // The sample customer's Java access, just after the account was created: it covers
        // the exam attempt DevQuizSeeder wrote for that customer.
        var sampleAt = sample.CreatedAt.AddMinutes(10);
        registrations.Add(NewRegistration(sample, subjects[Java], 6, RegistrationStatus.Paid, sampleAt, 0, sale, bySale: false));

        foreach (var row in Rows)
        {
            var customer = customers[row.Customer];
            var at = LocalToUtc(todayLocal.AddDays(-row.DaysAgo).AddHours(row.Hour));
            // Never before the account existed, never in the future.
            if (at < customer.CreatedAt)
            {
                at = customer.CreatedAt.AddMinutes(30);
            }

            if (at > utcNow)
            {
                at = utcNow.AddMinutes(-5);
            }

            registrations.Add(NewRegistration(customer, subjects[row.Subject], row.Months, row.Status, at, row.PaidAfter, sale, row.BySale));
        }

        // The Paid day of BR-14 must not pass today either.
        var today = DateOnly.FromDateTime(todayLocal);
        foreach (var r in registrations.Where(r => r.ValidFrom > today))
        {
            var months = r.ValidTo!.Value.Month - r.ValidFrom!.Value.Month + 12 * (r.ValidTo.Value.Year - r.ValidFrom.Value.Year);
            r.ValidFrom = today;
            r.ValidTo = today.AddMonths(months);
        }

        db.Registrations.AddRange(registrations);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Sample registrations seeded: {Customers} customers and {Registrations} registrations.",
            customers.Count, registrations.Count);
    }

    private static Registration NewRegistration(User customer, Subject subject, int months, RegistrationStatus status,
        DateTime registeredAtUtc, int paidAfter, User sale, bool bySale)
    {
        var package = subject.PricePackages
            .Where(p => p.Status == ActiveStatus.Active)
            .OrderBy(p => Math.Abs(p.AccessMonths - months))
            .First();
        var paidOn = DateOnly.FromDateTime((registeredAtUtc + Vietnam).Date).AddDays(paidAfter);
        return new Registration
        {
            Subject = subject,
            Package = package,
            User = customer,
            CreatedBy = bySale ? sale : null,
            UpdatedBy = sale,
            FullName = customer.FullName,
            Email = customer.Email,
            Mobile = customer.Mobile,
            Gender = customer.Gender,
            TotalCost = package.SalePrice,
            Status = status,
            RegisteredAt = registeredAtUtc,
            ValidFrom = status == RegistrationStatus.Paid ? paidOn : null,
            ValidTo = status == RegistrationStatus.Paid ? paidOn.AddMonths(package.AccessMonths) : null,
            Notes = status == RegistrationStatus.Cancelled ? "Customer changed their mind (sample data)." : null,
        };
    }
}
