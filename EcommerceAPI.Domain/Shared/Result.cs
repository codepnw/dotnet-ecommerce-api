namespace EcommerceAPI.Domain.Shared;

public class Result<T>(bool isSuccess, T? data, string? errorMessage, ErrorCode errorCode)
{
    public readonly bool IsSuccess = isSuccess;
    public readonly T? Data = data;
    public readonly string? ErrorMessage = errorMessage;
    public readonly ErrorCode ErrorCode = errorCode;

    public static Result<T> Success(T data) =>
        new(true, data, null, ErrorCode.None);

    public static Result<T> Failure(string errorMessage, ErrorCode errorCode) =>
        new(false, default, errorMessage, errorCode);
}

public class Result(bool isSuccess, string? errorMessage, ErrorCode errorCode)
{
    public readonly bool IsSuccess = isSuccess;
    public readonly string? ErrorMessage = errorMessage;
    public readonly ErrorCode ErrorCode = errorCode;

    public static Result Success() =>
        new(true, null, 0);

    public static Result Failure(string errorMessage, ErrorCode errorCode) =>
        new(false, errorMessage, errorCode);
}