using System.Diagnostics.CodeAnalysis;

namespace Administrator.Core;

public sealed class Result<T>
    where T : class
{
    private Result(bool isSuccessful)
    {
        IsSuccessful = isSuccessful;
    }
    
    public T? Value { get; private init; }
    
    public string? ErrorMessage { get; private init; }
    
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(ErrorMessage))]
    public bool IsSuccessful { get; }

    public static Result<T> Success(T value) => new(true) { Value = value };
    public static Result<T> Failure(string errorMessage) => new(false) { ErrorMessage = errorMessage };
    public static Result<T> Failure(Exception exception) => Failure(exception.Message);
    
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string errorMessage) => Failure(errorMessage);
}