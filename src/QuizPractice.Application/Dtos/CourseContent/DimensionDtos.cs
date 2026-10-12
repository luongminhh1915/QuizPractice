using QuizPractice.Domain.Entities;

namespace QuizPractice.Application.Dtos.CourseContent;

/// <summary>Dimension form of the Dimension tab (UC-46 step 3). TypeId: a Dimension Type setting.</summary>
public sealed record SaveDimensionRequest(int TypeId, string Name, string? Description);

public sealed record DimensionDto(int Id, int TypeId, string Type, string Name, string? Description)
{
    public static DimensionDto From(SubjectDimension d) => new(d.Id, d.TypeId, d.Type.Value, d.Name, d.Description);
}

/// <summary>The Dimension tab: the dimension types the form offers (BR-16) and the stored dimensions.</summary>
public sealed record DimensionTabDto(IReadOnlyList<LookupDto> Types, IReadOnlyList<DimensionDto> Dimensions);
