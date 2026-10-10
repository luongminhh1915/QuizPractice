using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Application.Validators.Marketing;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Tests.Application;

public class PostServiceTests
{
    private readonly FakePostRepository _repo = new();

    private PostService Service() => new(_repo, new PostListRequestValidator());

    [Fact]
    public async Task The_list_refuses_an_unknown_sort_column()
    {
        AssertInvalid(await Service().ListAsync(new PostListRequest(SortBy: "thumbnail")), "SortBy");
    }

    [Fact]
    public async Task Categories_come_with_their_status_so_inactive_ones_can_label_old_posts()
    {
        _repo.Categories.Add(new Setting { Id = 23, Type = SettingType.PostCategory, Value = "Old", Status = ActiveStatus.Inactive });

        var result = await Service().GetCategoriesAsync();

        Assert.Equal(ActiveStatus.Inactive, Assert.Single(result.Value).Status);
    }

    private static void AssertInvalid(Result result, string field)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains(field, result.Error.Fields!.Keys);
    }
}
