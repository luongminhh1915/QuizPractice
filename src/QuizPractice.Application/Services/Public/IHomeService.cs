using QuizPractice.Application.Dtos.Public;

namespace QuizPractice.Application.Services.Public;

/// <summary>UC-09 View Home Page: hot posts, featured subjects and the latest posts of the sider.</summary>
public interface IHomeService
{
    Task<HomeDto> GetAsync(CancellationToken ct = default);
}
