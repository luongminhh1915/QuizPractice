using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Application.Validators.Marketing;

namespace QuizPractice.Tests.Application;

public class SliderServiceTests
{
    private readonly FakeSliderRepository _repo = new();
    private readonly SliderService _service;

    public SliderServiceTests() =>
        _service = new SliderService(_repo, new SliderListRequestValidator());

    [Fact]
    public async Task The_list_refuses_an_unknown_sort_column()
    {
        var result = await _service.ListAsync(new SliderListRequest(SortBy: "image"));

        AssertInvalid(result, "SortBy");
    }

    private static void AssertInvalid(Result result, string field)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains(field, result.Error.Fields!.Keys);
    }
}
