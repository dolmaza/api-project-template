namespace ProjectName.Domain.Common.ResultPattern;

public sealed class Result : BaseResult
{
    private Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) || (!isSuccess && error == Error.None))
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success()=> new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T> : BaseResult
{
    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
        Value = default!;
    }

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
        Error = Error.None;
    }

    public T Value { get; }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Result result) => result.IsSuccess ? Success(default!) : Failure(result.Error);

    public static implicit operator T(Result<T> result)
    {
        return !result.IsSuccess ? throw new InvalidOperationException("Cannot convert a failed result to it's value.") : result.Value!;
    }

}