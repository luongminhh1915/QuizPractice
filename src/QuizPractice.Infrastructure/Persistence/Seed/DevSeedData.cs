using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Seed;

/// <summary>
/// Sample data for development: the settings of the 8 types, one account per role,
/// two subjects with topics, lessons, dimensions, price packages and questions, and
/// the receiving bank account. Built in memory so the tests can check it without a
/// database; <see cref="DevDataSeeder"/> writes it.
/// </summary>
public sealed class DevSeedData
{
    /// <summary>Password of every sample account. Development only; also in the README.</summary>
    public const string Password = "Qps@12345";

    public const string AdminEmail = "admin@qps.vn";
    public const string ExpertEmail = "expert@qps.vn";
    public const string MarketingEmail = "marketing@qps.vn";
    public const string SaleEmail = "sale@qps.vn";
    public const string CustomerEmail = "customer@qps.vn";

    public List<Setting> Settings { get; } = [];
    public List<User> Users { get; } = [];
    public List<Subject> Subjects { get; } = [];
    public List<Lesson> Lessons { get; } = [];
    public List<SubjectDimension> Dimensions { get; } = [];
    public List<Question> Questions { get; } = [];
    public BankAccount BankAccount { get; private set; } = null!;

    private readonly DateTime _now;

    private DevSeedData(DateTime now) => _now = now;

    public static DevSeedData Build(IPasswordHasher hasher, DateTime utcNow)
    {
        var data = new DevSeedData(utcNow);
        data.AddSettings();
        data.AddUsers(hasher.Hash(Password));
        data.AddJava();
        data.AddSql();
        data.BankAccount = new BankAccount
        {
            // Placeholder: no real account. The Admin sets the real one (UC-70).
            BankName = "Vietcombank",
            BankBin = "970436",
            AccountNumber = "0000000000",
            AccountName = "QUIZ PRACTICE DEMO",
            UpdatedAt = utcNow,
        };
        return data;
    }

    public Setting Find(SettingType type, string value) =>
        Settings.Single(s => s.Type == type && s.Value == value);

    // ---------------------------------------------------------------- settings

    private void AddSettings()
    {
        Add(SettingType.UserRole,
            (Roles.Customer, "Learner who registers for subjects and practises."),
            (Roles.Expert, "Prepares the content of the subjects assigned to them."),
            (Roles.Marketing, "Manages posts, sliders and the marketing dashboard."),
            (Roles.Sale, "Handles registrations and confirms invoice payments."),
            (Roles.Admin, "Manages subjects, users and settings."));

        // Labels only; who sees an item is the authorization map of the configuration (D23).
        Add(SettingType.SystemMenu,
            Menu("Dashboard", "the dashboard of the role"),
            Menu("Posts", "Posts List"),
            Menu("Sliders", "Sliders List"),
            Menu("Registrations", "Registrations List"),
            Menu("Invoices", "Invoices List"),
            Menu("Subjects", "Subject Management List"),
            Menu("Questions", "Questions List"),
            Menu("Quizzes", "Quizzes List"),
            Menu("Users", "Users List"),
            Menu("Settings", "Settings List"),
            Menu("My Registrations", "My Registrations"),
            Menu("Practices", "Practices List"),
            Menu("Simulation Exams", "Simulation Exams"),
            Menu("My Invoices", "My Invoices"));

        Add(SettingType.PostCategory,
            ("Study Tips", null), ("Exam Guides", null), ("News", null), ("Career", null));

        Add(SettingType.SubjectCategory,
            ("Programming", null), ("Database", null), ("Networking", null), ("Mathematics", null),
            ("Soft Skills", "Kept for later; not offered yet."));
        Find(SettingType.SubjectCategory, "Soft Skills").Status = ActiveStatus.Inactive;

        Add(SettingType.TestType,
            (SettingValues.TestTypes.Simulation, "Draws a new set of questions on every attempt."),
            (SettingValues.TestTypes.LessonQuiz, "Draws one fixed set when the quiz is saved."));

        Add(SettingType.QuestionLevel, ("Easy", null), ("Medium", null), ("Hard", null));

        Add(SettingType.LessonType,
            (SettingValues.LessonTypes.SubjectTopic, "A chapter; lessons sit under it."),
            (SettingValues.LessonTypes.Lesson, "Video link and HTML content."),
            (SettingValues.LessonTypes.Quiz, "Opens a lesson quiz of the subject."));

        // The two types the assignment names (D21).
        Add(SettingType.DimensionType, ("Domain", null), ("Group", null));
    }

    private static (string, string?) Menu(string label, string screen) => (label, $"Menu item that opens {screen}.");

    private void Add(SettingType type, params (string Value, string? Description)[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            Settings.Add(new Setting
            {
                Type = type,
                Value = values[i].Value,
                Description = values[i].Description,
                DisplayOrder = i + 1,
            });
        }
    }

    // ------------------------------------------------------------------- users

    private void AddUsers(string passwordHash)
    {
        AddUser(AdminEmail, "Trần Minh Quân", Gender.Male, "0901000001", Roles.Admin);
        AddUser(ExpertEmail, "Lê Thu Hà", Gender.Female, "0901000002", Roles.Expert);
        AddUser(MarketingEmail, "Phạm Quốc Bảo", Gender.Male, "0901000003", Roles.Marketing);
        AddUser(SaleEmail, "Nguyễn Thanh Tâm", Gender.Female, "0901000004", Roles.Sale);
        AddUser(CustomerEmail, "Đỗ Hoàng Nam", Gender.Male, "0901000005", Roles.Customer, "12 Nguyễn Trãi, Hà Nội");

        void AddUser(string email, string name, Gender gender, string mobile, string role, string? address = null) =>
            Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHash,
                FullName = name,
                Gender = gender,
                Mobile = mobile,
                Address = address,
                Role = Find(SettingType.UserRole, role),
                Status = UserStatus.Active,
                CreatedAt = _now,
            });
    }

    private User UserOf(string email) => Users.Single(u => u.Email == email);

    // ---------------------------------------------------------------- subjects

    /// <summary>Assigned to the Expert, featured on the home page.</summary>
    private void AddJava()
    {
        var java = AddSubject(
            "Java Object-Oriented Programming", "Programming", UserOf(ExpertEmail), featured: true, passRate: 50,
            "/seed/subjects/java-oop.png",
            tagline: "Classes, inheritance and interfaces through short practices.",
            brief: "Learn how Java models a program as objects, then check yourself with practice sets and simulation exams.",
            description: "<p>The subject covers the core of object-oriented programming in Java: classes and objects, "
                + "constructors, inheritance, method overriding, abstract classes and interfaces.</p>"
                + "<p>Each chapter ends with practice questions grouped by domain and by kind of question.</p>");

        AddPackage(java, "1-month trial", 1, 120_000, 99_000, ActiveStatus.Inactive);
        AddPackage(java, "3 months", 3, 300_000, 250_000);
        AddPackage(java, "6 months", 6, 500_000, 400_000);
        AddPackage(java, "12 months", 12, 900_000, 700_000);

        var ch1 = AddTopic(java, 1, "Chapter 1: Classes and objects");
        var l11 = AddLesson(java, ch1, 1, "1.1 Declaring a class");
        var l12 = AddLesson(java, ch1, 2, "1.2 Constructors");
        var ch2 = AddTopic(java, 2, "Chapter 2: Inheritance");
        var l21 = AddLesson(java, ch2, 1, "2.1 extends and super");
        var l22 = AddLesson(java, ch2, 2, "2.2 Overriding");
        var ch3 = AddTopic(java, 3, "Chapter 3: Abstract classes and interfaces");
        var l31 = AddLesson(java, ch3, 1, "3.1 Abstract classes");
        var l32 = AddLesson(java, ch3, 2, "3.2 Interfaces");

        var encapsulation = AddDimension(java, "Domain", "Encapsulation");
        var inheritance = AddDimension(java, "Domain", "Inheritance");
        var polymorphism = AddDimension(java, "Domain", "Polymorphism");
        var abstraction = AddDimension(java, "Domain", "Abstraction");
        var concept = AddDimension(java, "Group", "Concept", "Knows what a rule says.");
        var reading = AddDimension(java, "Group", "Code reading", "Predicts what a piece of code does.");

        AddQuestion(java, l11, "Easy", [encapsulation, concept],
            "Which keyword creates a new object in Java?",
            "new", ["class", "this", "static"],
            "new allocates the object and runs one of its constructors.");
        AddQuestion(java, l11, "Easy", [encapsulation, concept],
            "Which access modifier makes a field visible only inside the class that declares it?",
            "private", ["protected", "public", "No modifier (package-private)"],
            "private limits access to the declaring class; package-private still opens it to the whole package.");
        AddQuestion(java, l12, "Medium", [encapsulation, reading],
            "Class Point declares only the constructor public Point(int x, int y). What happens with new Point()?",
            "It does not compile: Point has no constructor without arguments.",
            ["It creates the point (0, 0).", "It compiles but throws an exception at run time.", "It creates a point whose fields are null."],
            "The compiler adds a default constructor only when a class declares none.");
        AddQuestion(java, l12, "Easy", [encapsulation, concept],
            "What is the return type of a constructor?",
            "A constructor has no return type.", ["void", "The type of its class", "Object"],
            "A method named like the class but declared void is an ordinary method, not a constructor.");
        AddQuestion(java, l21, "Easy", [inheritance, concept],
            "How many classes can a Java class extend directly?",
            "One", ["None", "Two", "As many as it needs"],
            "Java has single inheritance of classes; a class may still implement many interfaces.");
        AddQuestion(java, l21, "Medium", [inheritance, reading],
            "Where must the call super(...) appear in a constructor of a subclass?",
            "As the first statement of the constructor.",
            ["As the last statement of the constructor.", "Anywhere in the constructor.", "In a separate init method."],
            "The parent part of the object is built before the subclass adds its own state.");
        AddQuestion(java, l22, "Medium", [polymorphism, reading],
            "Dog extends Animal and overrides speak(). Which method runs for: Animal a = new Dog(); a.speak();",
            "Dog.speak(), chosen at run time from the object.",
            ["Animal.speak(), chosen from the variable type.", "Both, Animal.speak() first.", "None: the code does not compile."],
            "An overridden instance method is bound dynamically to the class of the object.");
        AddQuestion(java, l22, "Hard", [polymorphism, concept],
            "Which method of a parent class cannot be overridden by a subclass?",
            "A final method", ["A protected method", "A public method", "An abstract method"],
            "final forbids overriding; an abstract method must be overridden by a concrete subclass.");
        AddQuestion(java, l22, "Medium", [polymorphism, concept],
            "A parent class declares public void draw(). Which access may an overriding draw() in a subclass have?",
            "public only", ["public or protected", "Any access", "private only"],
            "An overriding method may not reduce the visibility of the method it overrides.");
        AddQuestion(java, l31, "Easy", [abstraction, concept],
            "Can an abstract class be instantiated with new?",
            "No; only a concrete subclass of it can.",
            ["Yes, like any class.", "Yes, if it has a constructor.", "Only inside its own package."],
            "An abstract class may have constructors, but they run only as part of a subclass object.");
        AddQuestion(java, l32, "Medium", [abstraction, concept],
            "Since Java 8, which kind of interface method may have a body?",
            "A default method", ["An abstract method", "A constructor", "A protected method"],
            "default (and static) methods carry a body; interfaces have no constructors.");

        // Imported and not yet checked by the Expert (D17): for the Hidden filter.
        AddQuestion(java, l32, "Hard", [abstraction, reading],
            "Interfaces A and B both declare default void hello(). Class C implements A, B and does not declare hello(). What happens?",
            "It does not compile: C must override hello().",
            ["A.hello() runs, as A is listed first.", "B.hello() runs, as B is listed last.", "It throws an exception when hello() is called."],
            "With two inherited defaults of the same signature the class has to choose, e.g. A.super.hello().",
            VisibilityStatus.Hidden);
    }

    /// <summary>No Expert yet (UC-42 A2): an Expert must not see its questions.</summary>
    private void AddSql()
    {
        var sql = AddSubject(
            "SQL Fundamentals", "Database", expert: null, featured: false, passRate: 60,
            "/seed/subjects/sql-fundamentals.png",
            tagline: "Query, group and join relational data.",
            brief: "Write the SELECT statements a developer uses every day and practise reading their results.",
            description: "<p>The subject starts from SELECT and WHERE, moves to sorting, grouping and aggregate "
                + "functions, and ends with inner and outer joins.</p>");

        AddPackage(sql, "3 months", 3, 200_000, 150_000);
        AddPackage(sql, "12 months", 12, 600_000, 450_000);

        var ch1 = AddTopic(sql, 1, "Chapter 1: Querying data");
        var l11 = AddLesson(sql, ch1, 1, "1.1 SELECT and WHERE");
        var l12 = AddLesson(sql, ch1, 2, "1.2 Sorting and grouping");
        var ch2 = AddTopic(sql, 2, "Chapter 2: Joins");
        var l21 = AddLesson(sql, ch2, 1, "2.1 Inner join");
        var l22 = AddLesson(sql, ch2, 2, "2.2 Outer join");

        var query = AddDimension(sql, "Domain", "Query");
        var aggregation = AddDimension(sql, "Domain", "Aggregation");
        var join = AddDimension(sql, "Domain", "Join");
        var concept = AddDimension(sql, "Group", "Concept", "Knows what a clause does.");
        var reading = AddDimension(sql, "Group", "Code reading", "Predicts the result of a query.");

        AddQuestion(sql, l11, "Easy", [query, concept],
            "Which clause filters rows before they are grouped?",
            "WHERE", ["HAVING", "ORDER BY", "GROUP BY"],
            "WHERE works on single rows; HAVING works on groups.");
        AddQuestion(sql, l11, "Easy", [query, reading],
            "Which condition finds the rows whose email is NULL?",
            "email IS NULL", ["email = NULL", "email == NULL", "email = ''"],
            "Any comparison with NULL using = is unknown, so no row passes it.");
        AddQuestion(sql, l12, "Medium", [aggregation, concept],
            "Which clause keeps only the groups whose COUNT(*) is above 5?",
            "HAVING", ["WHERE", "ORDER BY", "LIMIT"],
            "Aggregate conditions go in HAVING, after GROUP BY.");
        AddQuestion(sql, l12, "Medium", [aggregation, reading],
            "What does COUNT(email) count?",
            "The rows where email is not NULL.",
            ["Every row of the table.", "The distinct emails.", "The rows where email is NULL."],
            "COUNT(*) counts rows; COUNT(column) skips NULLs; COUNT(DISTINCT column) removes duplicates.");
        AddQuestion(sql, l21, "Easy", [join, concept],
            "Which rows does an INNER JOIN return?",
            "Only the rows that have a match in both tables.",
            ["Every row of the left table.", "Every row of both tables.", "Only the rows without a match."],
            "Rows without a partner are dropped by an inner join.");
        AddQuestion(sql, l22, "Hard", [join, reading],
            "Customers LEFT JOIN Orders: what is returned for a customer who has no order?",
            "One row with the Orders columns set to NULL.",
            ["No row for that customer.", "One row per order of other customers.", "An error, because there is no match."],
            "A left join keeps every row of the left table and fills the missing side with NULL.");
    }

    private Subject AddSubject(string name, string category, User? expert, bool featured, byte passRate,
        string thumbnail, string tagline, string brief, string description)
    {
        var subject = new Subject
        {
            Name = name,
            Category = Find(SettingType.SubjectCategory, category),
            Expert = expert,
            Tagline = tagline,
            Thumbnail = thumbnail,
            Brief = brief,
            Description = description,
            IsFeatured = featured,
            PassRate = passRate,
            Status = SubjectStatus.Published,
            UpdatedAt = _now,
        };
        Subjects.Add(subject);
        return subject;
    }

    private static void AddPackage(Subject subject, string name, byte months, decimal listPrice, decimal salePrice,
        ActiveStatus status = ActiveStatus.Active) =>
        subject.PricePackages.Add(new PricePackage
        {
            Subject = subject,
            Name = name,
            AccessMonths = months,
            ListPrice = listPrice,
            SalePrice = salePrice,
            Description = $"Full access to every practice and simulation exam for {months} month{(months > 1 ? "s" : "")}.",
            Status = status,
        });

    private Lesson AddTopic(Subject subject, int order, string name)
    {
        var topic = new Lesson
        {
            Subject = subject,
            Type = Find(SettingType.LessonType, SettingValues.LessonTypes.SubjectTopic),
            Name = name,
            DisplayOrder = order,
        };
        Lessons.Add(topic);
        return topic;
    }

    private Lesson AddLesson(Subject subject, Lesson topic, int order, string name)
    {
        var lesson = new Lesson
        {
            Subject = subject,
            Topic = topic,
            Type = Find(SettingType.LessonType, SettingValues.LessonTypes.Lesson),
            Name = name,
            DisplayOrder = order,
            HtmlContent = $"<h2>{name}</h2><p>Sample content of the lesson.</p>",
        };
        Lessons.Add(lesson);
        return lesson;
    }

    private SubjectDimension AddDimension(Subject subject, string type, string name, string? description = null)
    {
        var dimension = new SubjectDimension
        {
            Subject = subject,
            Type = Find(SettingType.DimensionType, type),
            Name = name,
            Description = description,
        };
        Dimensions.Add(dimension);
        return dimension;
    }

    /// <summary>Four options; the correct one goes to a different place for each question.</summary>
    private void AddQuestion(Subject subject, Lesson lesson, string level, SubjectDimension[] dimensions,
        string content, string correct, string[] wrong, string explanation,
        VisibilityStatus status = VisibilityStatus.Shown)
    {
        var question = new Question
        {
            Subject = subject,
            Lesson = lesson,
            Level = Find(SettingType.QuestionLevel, level),
            Content = content,
            Explanation = explanation,
            Status = status,
        };

        var options = wrong.ToList();
        options.Insert(Questions.Count % (wrong.Length + 1), correct);
        for (var i = 0; i < options.Count; i++)
        {
            question.Options.Add(new AnswerOption
            {
                Question = question,
                Content = options[i],
                IsCorrect = options[i] == correct,
                DisplayOrder = (byte)(i + 1),
            });
        }

        foreach (var dimension in dimensions)
        {
            question.Dimensions.Add(new QuestionDimension { Question = question, Dimension = dimension });
        }

        Questions.Add(question);
    }
}
