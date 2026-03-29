
namespace ExcelInsights.Application.Common;

public class Result<T>
{

    public bool IsSuccess { get; private init; }

    public T? Value { get; private init; }

    public string? Error { get; private init; }

    private Result() { }

    public static Result<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    public static Result<T> Failure(string error) =>
        new() { IsSuccess = false, Error = error };

    public static implicit operator Result<T>(T value) =>
        Success(value);
}