namespace ProjectName.Domain.Common.Abstractions;

public interface ICurrentStateService
{
    string GetAuthorizedId();

    bool IsInRole(string roleName);

    Guid GetCorrelationIdFromHeader();
}