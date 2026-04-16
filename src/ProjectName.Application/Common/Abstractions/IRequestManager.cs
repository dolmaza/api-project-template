namespace ProjectName.Application.Common.Abstractions;

public interface IRequestManager
{
    Task<bool> ExistAsync(Guid id);

    Task CreateOrUpdateClientRequestAsync(Guid id, string url);
}