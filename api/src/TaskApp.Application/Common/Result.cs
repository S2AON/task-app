namespace TaskApp.Application.Common;

public class Result<T>
{
    private Result(bool isSuccess, T? data, string? message, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    public bool IsSuccess { get; private set; }
    public string? Message { get; private set; }
    public T? Data { get; private set; }
    public List<string> Errors { get; private set; } = new();

    // Success con datos
    public static Result<T> Success(T data, string? message = null)
    {
        return new Result<T>(true, data, message);
    }

    // Failure simple
    public static Result<T> Failure(string message)
    {
        return new Result<T>(false, default, message);
    }

    // Failure con lista
    public static Result<T> Failure(IEnumerable<string> errors)
    {
        return new Result<T>(false, default, "Validation errors occurred", errors.ToList());
    }
}

public class Result
{
    private Result(bool isSuccess, string? message, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    public bool IsSuccess { get; private set; }
    public string? Message { get; private set; }
    public List<string> Errors { get; private set; } = new();

    // Success sin data
    public static Result Success(string? message = null)
    {
        return new Result(true, message);
    }

    // Failure simple
    public static Result Failure(string message)
    {
        return new Result(false, message);
    }

    // Failure con lista
    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, "Validation errors occurred", errors.ToList());
    }
}
