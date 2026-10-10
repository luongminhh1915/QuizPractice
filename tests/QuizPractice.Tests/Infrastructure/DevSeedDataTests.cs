using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Infrastructure.Persistence.Seed;
using QuizPractice.Infrastructure.Security;

namespace QuizPractice.Tests.Infrastructure;

/// <summary>
/// The sample data must obey the rules the services enforce, or a screen tested on
/// it shows what a real user could never create. The database CHECKs catch only part.
/// </summary>
public class DevSeedDataTests
{
    private static readonly PasswordHasher Hasher = new();
    private readonly DevSeedData _data = DevSeedData.Build(Hasher, new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Every_setting_type_has_values_with_unique_names_and_orders_from_1()
    {
        foreach (var type in Enum.GetValues<SettingType>())
        {
            var values = _data.Settings.Where(s => s.Type == type).ToList();
            Assert.NotEmpty(values);
            Assert.Equal(values.Count, values.Select(s => s.Value).Distinct().Count());
            Assert.Equal(Enumerable.Range(1, values.Count), values.Select(s => s.DisplayOrder).Order());
            Assert.All(values, s => Assert.InRange(s.Value.Length, 1, FieldLength.Name));
        }
    }

    [Fact]
    public void The_values_the_code_reads_are_seeded()
    {
        Assert.Equal(Roles.All.Order(), _data.Settings.Where(s => s.Type == SettingType.UserRole).Select(s => s.Value).Order());
        _data.Find(SettingType.LessonType, SettingValues.LessonTypes.SubjectTopic);
        _data.Find(SettingType.LessonType, SettingValues.LessonTypes.Lesson);
        _data.Find(SettingType.LessonType, SettingValues.LessonTypes.Quiz);
        _data.Find(SettingType.TestType, SettingValues.TestTypes.Simulation);
        _data.Find(SettingType.TestType, SettingValues.TestTypes.LessonQuiz);
    }

    [Fact]
    public void There_is_one_active_account_per_role_and_the_dev_password_opens_each()
    {
        Assert.Equal(Roles.All.Order(), _data.Users.Select(u => u.Role.Value).Order());
        Assert.Equal(_data.Users.Count, _data.Users.Select(u => u.Email).Distinct().Count());
        Assert.All(_data.Users, u =>
        {
            Assert.Equal(UserStatus.Active, u.Status);
            Assert.Matches("^0[0-9]{9}$", u.Mobile);
            Assert.InRange(u.FullName.Length, FieldLength.FullNameMin, FieldLength.FullName);
            Assert.True(Hasher.Verify(u.PasswordHash!, DevSeedData.Password));
            Assert.False(Hasher.Verify(u.PasswordHash!, "wrong-password1"));
        });
    }

    [Fact]
    public void The_dev_password_follows_the_password_rule_of_BR17()
    {
        Assert.InRange(DevSeedData.Password.Length, FieldLength.PasswordMin, FieldLength.PasswordMax);
        Assert.Contains(DevSeedData.Password, char.IsLetter);
        Assert.Contains(DevSeedData.Password, char.IsDigit);
    }

    [Fact]
    public void One_subject_belongs_to_the_Expert_and_one_to_nobody()
    {
        // Lets the Expert scoping of Questions and Quizzes be tried on real rows.
        var expert = _data.Users.Single(u => u.Role.Value == Roles.Expert);
        Assert.Single(_data.Subjects, s => s.Expert == expert);
        Assert.Single(_data.Subjects, s => s.Expert is null);
        Assert.All(_data.Subjects, s => Assert.Equal(SettingType.SubjectCategory, s.Category.Type));
    }

    [Fact]
    public void Price_packages_follow_D11_and_never_sell_above_the_list_price()
    {
        var packages = _data.Subjects.SelectMany(s => s.PricePackages).ToList();
        Assert.NotEmpty(packages);
        Assert.All(packages, p =>
        {
            Assert.InRange(p.AccessMonths, 1, 24);
            Assert.InRange(p.SalePrice, 0, p.ListPrice);
        });
    }

    [Fact]
    public void Lessons_sit_under_a_topic_of_the_same_subject()
    {
        foreach (var lesson in _data.Lessons)
        {
            if (lesson.Type.Value == SettingValues.LessonTypes.SubjectTopic)
            {
                Assert.Null(lesson.Topic);
                continue;
            }

            Assert.NotNull(lesson.Topic);
            Assert.Equal(SettingValues.LessonTypes.SubjectTopic, lesson.Topic.Type.Value);
            Assert.Same(lesson.Subject, lesson.Topic.Subject);
        }
    }

    [Fact]
    public void Every_question_has_exactly_one_correct_option_and_stays_inside_its_subject()
    {
        Assert.Contains(_data.Questions, q => q.Status == VisibilityStatus.Hidden);

        foreach (var question in _data.Questions)
        {
            Assert.Single(question.Options, o => o.IsCorrect);
            Assert.InRange(question.Options.Count, 2, 6);
            Assert.Equal(Enumerable.Range(1, question.Options.Count), question.Options.Select(o => (int)o.DisplayOrder));
            Assert.All(question.Options, o => Assert.InRange(o.Content.Length, 1, FieldLength.AnswerOption));

            Assert.Equal(SettingType.QuestionLevel, question.Level.Type);
            Assert.Same(question.Subject, question.Lesson!.Subject);
            Assert.Equal(SettingValues.LessonTypes.Lesson, question.Lesson.Type.Value);
            Assert.NotEmpty(question.Dimensions);
            Assert.All(question.Dimensions, d => Assert.Same(question.Subject, d.Dimension.Subject));
        }
    }

    [Fact]
    public void The_correct_option_is_not_always_in_the_same_place()
    {
        var places = _data.Questions.Select(q => q.Options.Single(o => o.IsCorrect).DisplayOrder).Distinct();
        Assert.True(places.Count() > 1);
    }

    [Fact]
    public void Each_subject_has_a_dimension_of_both_types_for_UC57()
    {
        foreach (var subject in _data.Subjects)
        {
            var types = _data.Dimensions.Where(d => d.Subject == subject).Select(d => d.Type.Value).Distinct();
            Assert.Equal(["Domain", "Group"], types.Order());
        }
    }

    [Fact]
    public void The_bank_account_is_the_single_row_and_fits_its_columns()
    {
        var account = _data.BankAccount;
        Assert.Equal(BankAccount.SingleId, account.Id);
        Assert.Matches("^[0-9]{6}$", account.BankBin);
        Assert.Matches("^[0-9]{1,19}$", account.AccountNumber);
    }
}
