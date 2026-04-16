namespace ProjectName.Domain.Common.ResultPattern;

public record ValidationItem(string PropertyName, string[] ErrorMessages);