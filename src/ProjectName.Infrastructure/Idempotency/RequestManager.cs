using Microsoft.EntityFrameworkCore;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Infrastructure.Database;

namespace ProjectName.Infrastructure.Idempotency;

public class RequestManager(ApplicationDbContext context) : IRequestManager
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<bool> ExistAsync(Guid id)
    {
        var exists = await _context.Set<ClientRequest>().AnyAsync(r => r.Id == id && !r.FinishedAt.HasValue);

        return exists;
    }

    public async Task CreateOrUpdateClientRequestAsync(Guid id, string url)
    {
        var existing = await _context.Set<ClientRequest>().FirstOrDefaultAsync(r => r.Id == id);

        if (existing is null)
        {
            var request = new ClientRequest(id, url, DateTime.UtcNow);
            await _context.AddAsync(request);
        }
        else
        {
            existing.MarkAsFinished();
            _context.Update(existing);
        }

        await _context.SaveChangesAsync();
    }
}