using QuizPractice.Application.Common;

namespace QuizPractice.UnitTests.Common;

public static class ResultAssert
{
    /// <summary>The result failed with this kind and code.</summary>
    public static Error Failed(Result result, ErrorType type, string code)
    {
        Assert.False(result.IsSuccess, "The result succeeded but a failure was expected.");
        Assert.Equal(type, result.Error!.Type);
        Assert.Equal(code, result.Error.Code);
        return result.Error;
    }

    /// <summary>The result failed validation on this field.</summary>
    public static void Invalid(Result result, string field)
    {
        var error = Failed(result, ErrorType.Validation, "Validation");
        Assert.NotNull(error.Fields);
        Assert.Contains(field, error.Fields!.Keys);
    }

    public static T Succeeded<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error is { } e ? $"{e.Code}: {e.Message}" : null);
        return result.Value;
    }
}
