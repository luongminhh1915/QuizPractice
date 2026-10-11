namespace QuizPractice.Application.Common;

/// <summary>Kinds of failure a service reports; the controller maps each to an HTTP status.</summary>
public enum ErrorType
{
    /// <summary>400: the input breaks a rule; Fields says which.</summary>
    Validation,

    /// <summary>404.</summary>
    NotFound,

    /// <summary>409: the data changed or a state rule forbids it (e.g. BR-22, BR-24).</summary>
    Conflict,

    /// <summary>403: signed in, but not allowed on this record (e.g. an Expert on another subject).</summary>
    Forbidden,

    /// <summary>401: who the caller is could not be proved, e.g. a wrong email or password (UC-01 E1).</summary>
    Unauthorized,

    /// <summary>502: a service outside the system failed, e.g. the mail server (UC-02 E3).</summary>
    ExternalFailure,
}

public sealed record Error(ErrorType Type, string Code, string Message, IReadOnlyDictionary<string, string[]>? Fields = null)
{
    public static Error NotFound(string what, int id) => new(ErrorType.NotFound, $"{what}.NotFound", $"{what} {id} does not exist.");

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);

    public static Error Invalid(string field, string message) =>
        new(ErrorType.Validation, "Validation", "One or more fields are invalid.", new Dictionary<string, string[]> { [field] = [message] });

    public static Error Invalid(IReadOnlyDictionary<string, string[]> fields) =>
        new(ErrorType.Validation, "Validation", "One or more fields are invalid.", fields);
}

/// <summary>Outcome of a use case: success, or one Error. Services never throw for an expected failure.</summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Error error) : base(error) { }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
