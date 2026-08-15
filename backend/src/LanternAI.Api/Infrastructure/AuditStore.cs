using System.Collections.Concurrent;

namespace LanternAI.Api.Infrastructure;

public sealed record AuditEvent(DateTimeOffset OccurredAt, string Action, string CorrelationId, string TenantId, string SubjectId, string? Question, int? RowCount, double? DurationMs);

public interface IAuditStore
{
    void Append(AuditEvent auditEvent);
    IReadOnlyList<AuditEvent> GetRecent(int limit = 100);
}

public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();

    public void Append(AuditEvent auditEvent)
    {
        _events.Enqueue(auditEvent);
        while (_events.Count > 1000 && _events.TryDequeue(out _)) { }
    }

    public IReadOnlyList<AuditEvent> GetRecent(int limit = 100) => _events.Reverse().Take(Math.Clamp(limit, 1, 1000)).ToList();
}