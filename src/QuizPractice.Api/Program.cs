using System.Text.Json.Serialization;
using QuizPractice.Api.Auth;
using QuizPractice.Application;
using QuizPractice.Application.Abstractions;
using QuizPractice.Infrastructure;
using QuizPractice.Infrastructure.Persistence.Seed;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("QuizPracticeDB")
    ?? throw new InvalidOperationException(
        "Connection string 'QuizPracticeDB' is missing. Copy appsettings.Development.example.json "
        + "to appsettings.Development.json and set your SQL Server (see README).");

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString,
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

// TODO(auth): until feature/kiet-common adds sign-in, the current user is the
// DevUser of appsettings.Development.json, and only Development has one.
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<DevUserOptions>(builder.Configuration.GetSection(DevUserOptions.Section));
    builder.Services.AddSingleton(DevCurrentUser.Resolve);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Sample accounts share one known password: never outside Development.
    // Resolved now so the start-up log says who the requests run as. Not before the
    // migrations are applied: the app still starts and the log says what to run.
    if (await DevDataSeeder.SeedAsync(app.Services))
    {
        app.Services.GetRequiredService<ICurrentUser>();
    }
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

app.UseAuthorization();
app.MapControllers();

app.Run();
