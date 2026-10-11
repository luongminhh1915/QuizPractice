using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Tests.Application;

// Hand-written fakes of the Application interfaces (SDS I.2: unit tests run without a
// database). Each one records what the service did so a test can assert on it.

public sealed class FakeSliderRepository : ISliderRepository
{
    public List<Slider> Rows { get; } = [];

    public Task<Slider?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Rows.FirstOrDefault(s => s.Id == id));

    public Task<Page<Slider>> SearchAsync(SliderSearch search, PageRequest page, CancellationToken ct = default) =>
        Task.FromResult(new Page<Slider>(Rows.Skip(page.Skip).Take(page.PageSize).ToList(), Rows.Count, page.PageNumber, page.PageSize));

    public Task<int> NextDisplayOrderAsync(CancellationToken ct = default) =>
        Task.FromResult(Rows.Count == 0 ? 1 : Rows.Max(s => s.DisplayOrder) + 1);

    public void Add(Slider slider)
    {
        slider.Id = Rows.Count == 0 ? 1 : Rows.Max(s => s.Id) + 1;
        Rows.Add(slider);
    }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Commits { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        Commits++;
        return Task.FromResult(1);
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default) => work(ct);
}

public sealed class FakeFileStorage : IFileStorage
{
    public List<string> Saved { get; } = [];
    public List<string> Deleted { get; } = [];

    public Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct = default)
    {
        var path = $"/uploads/{folder}/file{Saved.Count + 1}{extension}";
        Saved.Add(path);
        return Task.FromResult(path);
    }

    public void Delete(string relativePath) => Deleted.Add(relativePath);
}

public sealed class FakePostRepository : IPostRepository
{
    public List<Post> Rows { get; } = [];
    public List<Setting> Categories { get; } = [];
    public List<User> Users { get; } = [];

    public Task<Post?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Rows.FirstOrDefault(p => p.Id == id));

    public Task<Page<Post>> SearchAsync(PostSearch search, PageRequest page, CancellationToken ct = default) =>
        Task.FromResult(new Page<Post>(Rows.Skip(page.Skip).Take(page.PageSize).ToList(), Rows.Count, page.PageNumber, page.PageSize));

    public Task<IReadOnlyList<Setting>> GetCategoriesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Setting>>(Categories);

    public Task<Setting?> GetCategoryAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Categories.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<Post>> ListShownAsync(bool featuredOnly, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Post>>(Rows
            .Where(p => p.Status == VisibilityStatus.Shown && (!featuredOnly || p.IsFeatured))
            .OrderByDescending(p => p.UpdatedAt).Take(take).ToList());

    public Task<Post?> GetShownAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Rows.FirstOrDefault(p => p.Id == id && p.Status == VisibilityStatus.Shown));

    public Task<Page<Post>> SearchShownAsync(ShownPostSearch search, PageRequest page, CancellationToken ct = default) =>
        throw new NotSupportedException("Not used by the tests of the Marketing services.");

    /// <summary>Gives the post an id and its author, as the database and Include would.</summary>
    public void Add(Post post)
    {
        post.Id = Rows.Count == 0 ? 1 : Rows.Max(p => p.Id) + 1;
        post.Author = Users.First(u => u.Id == post.AuthorId);
        post.CategoryId = post.Category.Id;
        Rows.Add(post);
    }
}

public sealed class FakeCurrentUser(int? id = 3, string? role = "Marketing") : ICurrentUser
{
    public int? Id { get; } = id;
    public string? Email { get; } = id is null ? null : "marketing@qps.vn";
    public string? Role { get; } = id is null ? null : role;
}
