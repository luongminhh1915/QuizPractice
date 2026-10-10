using System.Text;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Application.Validators.Marketing;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Infrastructure.Security;

namespace QuizPractice.Tests.Application;

public class PostServiceTests
{
    private static readonly DateTime Loaded = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakePostRepository _repo = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeFileStorage _files = new();
    private readonly Setting _tips = new() { Id = 21, Type = SettingType.PostCategory, Value = "Study Tips" };
    private readonly Setting _news = new() { Id = 22, Type = SettingType.PostCategory, Value = "News" };
    private readonly Setting _old = new() { Id = 23, Type = SettingType.PostCategory, Value = "Old", Status = ActiveStatus.Inactive };
    private readonly User _marketing = new() { Id = 3, FullName = "Phạm Quốc Bảo", Email = "marketing@qps.vn" };

    public PostServiceTests()
    {
        _repo.Categories.AddRange([_tips, _news, _old]);
        _repo.Users.Add(_marketing);
    }

    private PostService Service(FakeCurrentUser? user = null) =>
        new(_repo, _uow, _files, new HtmlContentSanitizer(), user ?? new FakeCurrentUser(),
            new SavePostRequestValidator(), new PostListRequestValidator(), new ImageUploadValidator());

    private static SavePostRequest NewPost(int categoryId = 21, string? content = "<p>Body</p>", FileUpload? thumbnail = null) =>
        new("Five tips for the SQL exam", categoryId, "Short summary.", content, Thumbnail: thumbnail ?? Png());

    // ------------------------------------------------------------------- UC-36

    [Fact]
    public async Task Create_stores_a_hidden_post_by_the_signed_in_user_and_commits_once()
    {
        var result = await Service().CreateAsync(NewPost());

        Assert.True(result.IsSuccess);
        var post = result.Value;
        Assert.Equal(VisibilityStatus.Hidden, post.Status); // BR-23
        Assert.Equal(_marketing.Id, post.AuthorId);
        Assert.Equal("Phạm Quốc Bảo", post.Author);
        Assert.Equal("Study Tips", post.Category);
        Assert.Equal("/uploads/posts/file1.png", post.Thumbnail);
        Assert.Equal(1, _uow.Commits);
    }

    [Fact]
    public async Task Create_without_a_thumbnail_is_refused()
    {
        var result = await Service().CreateAsync(NewPost() with { Thumbnail = null });

        AssertInvalid(result, "Thumbnail");
        Assert.Equal(0, _uow.Commits);
    }

    [Fact]
    public async Task Create_with_an_empty_title_and_brief_marks_both_fields()
    {
        // UC-36 E1
        var result = await Service().CreateAsync(NewPost() with { Title = " ", Brief = "" });

        AssertInvalid(result, "Title");
        AssertInvalid(result, "Brief");
        Assert.Empty(_files.Saved);
    }

    [Theory]
    [InlineData(99)] // no such setting, or a setting of another type
    [InlineData(23)] // a Post Category that is Inactive
    public async Task Create_refuses_a_category_that_cannot_be_chosen(int categoryId)
    {
        var result = await Service().CreateAsync(NewPost(categoryId));

        AssertInvalid(result, "CategoryId");
        Assert.Empty(_files.Saved);
    }

    [Fact]
    public async Task Create_refuses_an_svg_renamed_to_png()
    {
        // UC-36 E2
        var svg = new MemoryStream(Encoding.UTF8.GetBytes("<svg onload=\"alert(1)\"></svg>"));

        var result = await Service().CreateAsync(NewPost(thumbnail: new FileUpload(svg, "cover.png", svg.Length)));

        AssertInvalid(result, "Thumbnail.Content");
    }

    [Fact]
    public async Task Create_cleans_the_body_before_storing_it()
    {
        const string dirty = "<h2>Plan</h2><script>alert(1)</script><p onclick=\"steal()\">Read <a href=\"javascript:alert(1)\">this</a></p>";

        var result = await Service().CreateAsync(NewPost(content: dirty));

        Assert.True(result.IsSuccess);
        Assert.Equal("<h2>Plan</h2><p>Read <a>this</a></p>", _repo.Rows[0].Content);
    }

    [Fact]
    public async Task A_guest_cannot_write_a_post()
    {
        var result = await Service(new FakeCurrentUser(id: null)).CreateAsync(NewPost());

        Assert.Equal(ErrorType.Forbidden, result.Error?.Type);
        Assert.Empty(_repo.Rows);
    }

    [Fact]
    public async Task Update_with_the_loaded_version_saves_and_moves_the_version_on()
    {
        var stored = AddStored();

        var result = await Service().UpdateAsync(stored.Id, Edit(stored) with { Title = "New title", CategoryId = 22 });

        Assert.True(result.IsSuccess);
        Assert.Equal("New title", stored.Title);
        Assert.Equal("News", result.Value.Category);
        Assert.True(result.Value.Version > new DateTimeOffset(Loaded).ToUnixTimeSeconds());
        Assert.Equal(1, _uow.Commits);
    }

    [Fact]
    public async Task Update_after_another_user_saved_is_a_conflict_and_changes_nothing()
    {
        // UC-36 E3: the form was loaded before the stored row was changed again.
        var stored = AddStored();
        var staleForm = Edit(stored) with { Version = PostVersion.Of(stored) - 60, Thumbnail = Png() };

        var result = await Service().UpdateAsync(stored.Id, staleForm with { Title = "Mine" });

        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal("Stored title", stored.Title);
        Assert.Empty(_files.Saved);
        Assert.Equal(0, _uow.Commits);
    }

    [Fact]
    public async Task Update_without_a_version_is_refused()
    {
        var stored = AddStored();

        var result = await Service().UpdateAsync(stored.Id, Edit(stored) with { Version = null });

        AssertInvalid(result, "Version");
    }

    [Fact]
    public async Task A_post_may_keep_a_category_that_became_inactive_but_not_move_to_one()
    {
        var stored = AddStored(_old);
        Assert.True((await Service().UpdateAsync(stored.Id, Edit(stored) with { Title = "Still old" })).IsSuccess);

        var other = AddStored(_tips);
        AssertInvalid(await Service().UpdateAsync(other.Id, Edit(other) with { CategoryId = _old.Id }), "CategoryId");
    }

    [Fact]
    public async Task Update_with_a_new_thumbnail_deletes_the_old_file_after_the_commit()
    {
        var stored = AddStored();

        await Service().UpdateAsync(stored.Id, Edit(stored) with { Thumbnail = Png() });

        Assert.Equal("/uploads/posts/file1.png", stored.Thumbnail);
        Assert.Equal(["/uploads/posts/old.png"], _files.Deleted);
    }

    [Fact]
    public async Task An_unknown_post_is_not_found()
    {
        Assert.Equal(ErrorType.NotFound, (await Service().GetAsync(42)).Error?.Type);
    }

    // ------------------------------------------------------------ editor and UC-35

    [Fact]
    public async Task The_list_refuses_an_unknown_sort_column()
    {
        AssertInvalid(await Service().ListAsync(new PostListRequest(SortBy: "thumbnail")), "SortBy");
    }

    [Fact]
    public async Task A_body_image_goes_to_the_folder_the_sanitizer_lets_through()
    {
        var result = await Service().UploadContentImageAsync(Png());

        Assert.StartsWith(HtmlContentSanitizer.ImagePathPrefix, result.Value.Url);
    }

    [Fact]
    public async Task A_body_image_must_be_a_real_image()
    {
        var html = new MemoryStream(Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"));

        var result = await Service().UploadContentImageAsync(new FileUpload(html, "x.png", html.Length));

        AssertInvalid(result, "Content");
    }

    private Post AddStored(Setting? category = null)
    {
        var post = new Post
        {
            AuthorId = _marketing.Id,
            Category = category ?? _tips,
            Title = "Stored title",
            Brief = "Brief",
            Content = "<p>Body</p>",
            Thumbnail = "/uploads/posts/old.png",
            UpdatedAt = Loaded,
        };
        _repo.Add(post);
        return post;
    }

    /// <summary>The form as Post Details sends it back after loading the post.</summary>
    private static SavePostRequest Edit(Post post) =>
        new(post.Title, post.CategoryId, post.Brief, post.Content, post.IsFeatured, post.Status, PostVersion.Of(post));

    private static FileUpload Png()
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var stream = new MemoryStream([.. signature, .. new byte[16]]);
        return new FileUpload(stream, "cover.png", stream.Length);
    }

    private static void AssertInvalid(Result result, string field)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains(field, result.Error.Fields!.Keys);
    }
}
