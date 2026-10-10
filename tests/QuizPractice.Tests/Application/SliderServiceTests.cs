using System.Text;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Application.Validators.Marketing;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Tests.Application;

public class SliderServiceTests
{
    private readonly FakeSliderRepository _repo = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeFileStorage _files = new();
    private readonly SliderService _service;

    public SliderServiceTests() =>
        _service = new SliderService(_repo, _uow, _files, new SaveSliderRequestValidator(), new SliderListRequestValidator());

    [Fact]
    public async Task Create_stores_the_slider_after_the_last_one_and_commits_once()
    {
        _repo.Add(new Slider { Title = "Old", Image = "/uploads/sliders/old.png", DisplayOrder = 4 });

        var result = await _service.CreateAsync(new SaveSliderRequest("Spring sale", "https://example.com/sale", Image: Png()));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.DisplayOrder);
        Assert.Equal(VisibilityStatus.Hidden, result.Value.Status);
        Assert.Equal("/uploads/sliders/file1.png", result.Value.Image);
        Assert.Equal(1, _uow.Commits);
    }

    [Fact]
    public async Task Create_without_an_image_is_refused()
    {
        var result = await _service.CreateAsync(new SaveSliderRequest("No image", null));

        AssertInvalid(result, "Image");
        Assert.Equal(0, _uow.Commits);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("example.com")]
    [InlineData("javascript:alert(1)")]
    public async Task Create_refuses_a_backlink_that_is_not_https(string backlink)
    {
        // UC-39 E1
        var result = await _service.CreateAsync(new SaveSliderRequest("Banner", backlink, Image: Png()));

        AssertInvalid(result, "Backlink");
        Assert.Empty(_files.Saved);
    }

    [Fact]
    public async Task Create_refuses_an_svg_renamed_to_png()
    {
        // UC-39 E2: the extension says PNG, the bytes say otherwise.
        var svg = new MemoryStream(Encoding.UTF8.GetBytes("<svg onload=\"alert(1)\"></svg>"));

        var result = await _service.CreateAsync(new SaveSliderRequest("Banner", null, Image: new FileUpload(svg, "banner.png", svg.Length)));

        AssertInvalid(result, "Image.Content");
        Assert.Empty(_files.Saved);
    }

    [Fact]
    public async Task Create_refuses_an_image_over_2_MB()
    {
        var big = Png(extraBytes: 2 * 1024 * 1024);

        var result = await _service.CreateAsync(new SaveSliderRequest("Banner", null, Image: big));

        AssertInvalid(result, "Image.Length");
    }

    [Fact]
    public async Task Update_with_a_new_image_deletes_the_old_file_after_the_commit()
    {
        _repo.Add(new Slider { Title = "Old", Image = "/uploads/sliders/old.png", DisplayOrder = 1 });

        var result = await _service.UpdateAsync(1, new SaveSliderRequest("New title", null, VisibilityStatus.Shown, Image: Png()));

        Assert.True(result.IsSuccess);
        Assert.Equal("New title", _repo.Rows[0].Title);
        Assert.Equal(["/uploads/sliders/old.png"], _files.Deleted);
        Assert.Equal(1, _uow.Commits);
    }

    [Fact]
    public async Task Update_without_an_image_keeps_the_stored_one()
    {
        _repo.Add(new Slider { Title = "Old", Image = "/uploads/sliders/old.png", DisplayOrder = 1 });

        await _service.UpdateAsync(1, new SaveSliderRequest("New title", null));

        Assert.Equal("/uploads/sliders/old.png", _repo.Rows[0].Image);
        Assert.Empty(_files.Deleted);
    }

    [Fact]
    public async Task An_unknown_slider_is_not_found()
    {
        var result = await _service.GetAsync(42);

        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task The_list_refuses_an_unknown_sort_column()
    {
        var result = await _service.ListAsync(new SliderListRequest(SortBy: "image"));

        AssertInvalid(result, "SortBy");
    }

    private static FileUpload Png(int extraBytes = 16)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var stream = new MemoryStream([.. signature, .. new byte[extraBytes]]);
        return new FileUpload(stream, "banner.png", stream.Length);
    }

    private static void AssertInvalid(Result result, string field)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains(field, result.Error.Fields!.Keys);
    }
}
