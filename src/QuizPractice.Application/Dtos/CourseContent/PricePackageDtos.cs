using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.CourseContent;

/// <summary>Package form of the Price Package tab (UC-47 step 3). A new package starts Active; the status changes from the list.</summary>
public sealed record SavePricePackageRequest(string Name, int AccessMonths, decimal ListPrice, decimal SalePrice, string? Description);

/// <summary>Activate or Deactivate on the Price Package tab (UC-47 A2).</summary>
public sealed record ChangePricePackageStatusRequest(ActiveStatus Status);

public sealed record PricePackageDto(
    int Id,
    string Name,
    byte AccessMonths,
    decimal ListPrice,
    decimal SalePrice,
    string? Description,
    ActiveStatus Status)
{
    public static PricePackageDto From(PricePackage p) =>
        new(p.Id, p.Name, p.AccessMonths, p.ListPrice, p.SalePrice, p.Description, p.Status);
}

/// <summary>The Price Package tab. CanEdit: the Admin; the assigned Expert only views (UC-48).</summary>
public sealed record PricePackageTabDto(bool CanEdit, IReadOnlyList<PricePackageDto> Packages);
