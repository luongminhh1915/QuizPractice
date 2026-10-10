using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Marketing;

public class PostService(
    IPostRepository posts,
    IUnitOfWork unitOfWork,
    IFileStorage files,
    IHtmlSanitizer sanitizer,
    ICurrentUser currentUser,
    IValidator<SavePostRequest> saveValidator,
    IValidator<PostListRequest> listValidator,
    IValidator<FileUpload> imageValidator) : IPostService
{
    private const string ThumbnailFolder = "posts";

    /// <summary>Must match HtmlContentSanitizer.ImagePathPrefix, which only lets these images through.</summary>
    private const string ContentImageFolder = "post-content";

    public async Task<Result<Page<PostSummaryDto>>> ListAsync(PostListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var search = new PostSearch(request.Search, request.CategoryId, request.Status, request.SortBy, request.Descending);
        var page = await posts.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(PostSummaryDto.From);
    }

    public async Task<Result<PostDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var post = await posts.GetByIdAsync(id, ct);
        return post is null ? Error.NotFound("Post", id) : PostDto.From(post);
    }

    public async Task<Result<IReadOnlyList<PostCategoryDto>>> GetCategoriesAsync(CancellationToken ct = default) =>
        (await posts.GetCategoriesAsync(ct)).Select(PostCategoryDto.From).ToList();

    public async Task<Result<PostDto>> CreateAsync(SavePostRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } authorId)
        {
            return NotSignedIn;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        if (request.Thumbnail is null)
        {
            return Error.Invalid(nameof(request.Thumbnail), "A thumbnail is required for a new post.");
        }

        var category = await posts.GetCategoryAsync(request.CategoryId, ct);
        if (CheckCategory(category, keptCategoryId: null) is { } badCategory)
        {
            return badCategory;
        }

        var content = sanitizer.Sanitize(request.Content ?? string.Empty);
        if (CheckContent(content) is { } badContent)
        {
            return badContent;
        }

        var post = new Post
        {
            AuthorId = authorId,
            Category = category!,
            Title = request.Title.Trim(),
            Brief = request.Brief.Trim(),
            Content = content,
            IsFeatured = request.IsFeatured,
            Status = request.Status,
            UpdatedAt = PostVersion.Now(),
            Thumbnail = await files.SaveAsync(request.Thumbnail.Content, request.Thumbnail.Extension, ThumbnailFolder, ct),
        };
        posts.Add(post);
        await unitOfWork.SaveChangesAsync(ct);

        // Read back for the author name the form shows.
        return PostDto.From(await posts.GetByIdAsync(post.Id, ct) ?? post);
    }

    public async Task<Result<PostDto>> UpdateAsync(int id, SavePostRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is null)
        {
            return NotSignedIn;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var post = await posts.GetByIdAsync(id, ct);
        if (post is null)
        {
            return Error.NotFound("Post", id);
        }

        // UC-36 E3. Checked before anything else changes, the upload included.
        if (request.Version is null)
        {
            return Error.Invalid(nameof(request.Version), "The version the form was loaded with is missing.");
        }

        if (request.Version != PostVersion.Of(post))
        {
            return Error.Conflict("Post.Changed",
                "Another user has changed this post since you opened it. The current values are loaded; make your change again.");
        }

        var category = post.CategoryId == request.CategoryId ? post.Category : await posts.GetCategoryAsync(request.CategoryId, ct);
        if (CheckCategory(category, keptCategoryId: post.CategoryId) is { } badCategory)
        {
            return badCategory;
        }

        var content = sanitizer.Sanitize(request.Content ?? string.Empty);
        if (CheckContent(content) is { } badContent)
        {
            return badContent;
        }

        string? replacedThumbnail = null;
        if (request.Thumbnail is not null)
        {
            replacedThumbnail = post.Thumbnail;
            post.Thumbnail = await files.SaveAsync(request.Thumbnail.Content, request.Thumbnail.Extension, ThumbnailFolder, ct);
        }

        post.Category = category!;
        post.Title = request.Title.Trim();
        post.Brief = request.Brief.Trim();
        post.Content = content;
        post.IsFeatured = request.IsFeatured;
        post.Status = request.Status;
        post.UpdatedAt = PostVersion.Now();
        await unitOfWork.SaveChangesAsync(ct);

        // Only after the commit: if it had failed, the row would still point at the old file.
        if (replacedThumbnail is not null)
        {
            files.Delete(replacedThumbnail);
        }

        return PostDto.From(post);
    }

    public async Task<Result<PostSummaryDto>> ChangeStatusAsync(int id, ChangePostStatusRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is null)
        {
            return NotSignedIn;
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Error.Invalid(nameof(request.Status), "Status must be Shown or Hidden.");
        }

        var post = await posts.GetByIdAsync(id, ct);
        if (post is null)
        {
            return Error.NotFound("Post", id);
        }

        // UC-37 A1 and E1: already in the chosen state (perhaps set by another Marketing
        // user meanwhile), so nothing is written and the list shows the current state.
        if (post.Status != request.Status)
        {
            post.Status = request.Status;
            post.UpdatedAt = PostVersion.Now();
            await unitOfWork.SaveChangesAsync(ct);
        }

        return PostSummaryDto.From(post);
    }

    public async Task<Result<ContentImageDto>> UploadContentImageAsync(FileUpload image, CancellationToken ct = default)
    {
        if (currentUser.Id is null)
        {
            return NotSignedIn;
        }

        if (await imageValidator.CheckAsync(image, ct) is { } invalid)
        {
            return invalid;
        }

        return new ContentImageDto(await files.SaveAsync(image.Content, image.Extension, ContentImageFolder, ct));
    }

    private static readonly Error NotSignedIn =
        new(ErrorType.Forbidden, "Auth.Required", "Sign in as a Marketing user to manage posts.");

    /// <summary>
    /// A new choice must be an active Post Category. A post may keep a category that
    /// became Inactive after it was written, so editing it does not force a change.
    /// </summary>
    private static Error? CheckCategory(Setting? category, int? keptCategoryId)
    {
        if (category is null)
        {
            return Error.Invalid(nameof(SavePostRequest.CategoryId), "Choose a post category.");
        }

        if (category.Status == ActiveStatus.Inactive && category.Id != keptCategoryId)
        {
            return Error.Invalid(nameof(SavePostRequest.CategoryId), $"The category \"{category.Value}\" is no longer in use.");
        }

        return null;
    }

    /// <summary>The limit of BR-17 applies to what is stored, after cleaning.</summary>
    private static Error? CheckContent(string content) =>
        content.Length > FieldLength.LongText
            ? Error.Invalid(nameof(SavePostRequest.Content), $"The description must be {FieldLength.LongText:N0} characters or fewer.")
            : null;
}
