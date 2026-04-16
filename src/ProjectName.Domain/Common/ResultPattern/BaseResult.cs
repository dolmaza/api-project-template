namespace ProjectName.Domain.Common.ResultPattern;

public abstract class BaseResult
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;

    public Error Error { get; protected set; } = Error.None;
}