using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>UC-46 Add or Edit Subject Dimension, for the Admin and the assigned Expert (BR-11).</summary>
public interface ISubjectDimensionService
{
    Task<Result<DimensionTabDto>> ListAsync(int subjectId, CancellationToken ct = default);

    Task<Result<DimensionDto>> CreateAsync(int subjectId, SaveDimensionRequest request, CancellationToken ct = default);

    Task<Result<DimensionDto>> UpdateAsync(int subjectId, int dimensionId, SaveDimensionRequest request, CancellationToken ct = default);

    /// <summary>A2: only while no question, practice or quiz rule uses the dimension (E3).</summary>
    Task<Result> DeleteAsync(int subjectId, int dimensionId, CancellationToken ct = default);
}
