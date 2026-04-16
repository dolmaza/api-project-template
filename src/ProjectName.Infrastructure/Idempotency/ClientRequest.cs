namespace ProjectName.Infrastructure.Idempotency;

public class ClientRequest
{
    private ClientRequest()
    {
    }

    public ClientRequest(Guid id, string url, DateTime time)
    {
        Id = id;
        Url = url;
        Time = time;
    }

    public Guid Id { get; private set; }
    public string? Url { get; private set; }
    public DateTime Time { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public TimeSpan? Duration { get; private set; }

    public void MarkAsFinished()
    {
        if (FinishedAt.HasValue)
        {
            return;
        }

        FinishedAt = DateTime.UtcNow;
        Duration = FinishedAt.Value - Time;
    }
}