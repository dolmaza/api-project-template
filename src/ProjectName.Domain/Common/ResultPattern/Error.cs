namespace ProjectName.Domain.Common.ResultPattern;

public sealed record Error
{
    public string Code { get; }
    public ErrorType Type { get; }
    public string? Description { get; }
    public List<ValidationItem>? Validations { get; set; }


    public static readonly Error None = new(string.Empty, ErrorType.Failure);

    public static implicit operator Result(Error error) => Result.Failure(error);

    private Error(string code, ErrorType type, string? description = null, List<ValidationItem>? validations = null)
    {
        Code = code;
        Type = type;
        Description = description;
        Validations = validations;
    }

    public static Error Failure(string code, string description) => new(code, ErrorType.Failure, description);
    public static Error Validation(string code, string description, List<ValidationItem>? validations = null) => new(code, ErrorType.Validation, description, validations);
    public static Error NotFound(string code, string description) => new(code, ErrorType.NotFound, description);
    public static Error Conflict(string code, string description) => new(code, ErrorType.Conflict, description);
}