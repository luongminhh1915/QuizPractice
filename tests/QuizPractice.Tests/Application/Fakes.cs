using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
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
