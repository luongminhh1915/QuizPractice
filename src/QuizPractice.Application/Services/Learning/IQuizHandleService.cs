using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Learning;

namespace QuizPractice.Application.Services.Learning;

/// <summary>Quiz Handle (F#17; UC-24 to UC-30): a running attempt of the signed-in customer.</summary>
public interface IQuizHandleService
{
    /// <summary>UC-24 step 1: the attempt with every question, the recorded answers and marks.</summary>
    Task<Result<QuizHandleDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-24 steps 2 and 3, A1: records or replaces the answer of one question.</summary>
    Task<Result<HandleProgressDto>> AnswerAsync(int id, int order, AnswerRequest request, CancellationToken ct = default);

    /// <summary>The image, audio or video of a question in an attempt of the customer (BR-05).</summary>
    Task<Result<MediaFile>> GetMediaAsync(int id, int order, CancellationToken ct = default);
}
