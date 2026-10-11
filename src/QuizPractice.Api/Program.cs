using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using QuizPractice.Api.Auth;
using QuizPractice.Application;
using QuizPractice.Infrastructure;
using QuizPractice.Infrastructure.Email;
using QuizPractice.Infrastructure.Persistence.Seed;
using QuizPractice.Infrastructure.Security;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("QuizPracticeDB")
    ?? throw new InvalidOperationException(
        "Connection string 'QuizPracticeDB' is missing. Copy appsettings.Development.example.json "
        + "to appsettings.Development.json and set your SQL Server (see README).");

builder.Services
    // Every POST, PUT and DELETE must carry the antiforgery header (CookieSignIn).
    .AddControllers(o => o.Filters.Add<AntiforgeryCheck>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString,
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.Section));
builder.Services.Configure<MailLinkOptions>(builder.Configuration.GetSection(MailLinkOptions.Section));
builder.Services.AddAccountRateLimit();

builder.Services.AddCookieSignIn();
builder.Services.AddPermissionTable(builder.Configuration);

// Signs the sign-in cookie and the links of the account mails. Kept on disk: with
// keys only in memory, every restart would sign everyone out and kill every link
// already sent (SDS, stateless links).
builder.Services.AddDataProtection()
    .SetApplicationName("QuizPractice")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Sample accounts share one known password: never outside Development. Not before
    // the migrations are applied: the app still starts and the log says what to run.
    await DevDataSeeder.SeedAsync(app.Services);
}

// Unhandled exceptions become a ProblemDetails 500 without the stack trace.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Serves /uploads; the React build will be served from here too.
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
