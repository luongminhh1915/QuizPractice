using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Tests.Application;

// Hand-written fakes of the Application interfaces (SDS I.2: unit tests run without a
// database). Each one records what the service did so a test can assert on it.

public sealed class FakeSliderRepository : ISliderRepository
{
    public List<Slider> Rows { get; } = [];

    public Task<Page<Slider>> SearchAsync(SliderSearch search, PageRequest page, CancellationToken ct = default) =>
        Task.FromResult(new Page<Slider>(Rows.Skip(page.Skip).Take(page.PageSize).ToList(), Rows.Count, page.PageNumber, page.PageSize));
}
