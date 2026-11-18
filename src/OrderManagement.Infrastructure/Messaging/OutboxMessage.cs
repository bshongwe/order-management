namespace OrderManagement.Infrastructure.Messaging;
public sealed class OutboxMessage
{
    private OutboxMessage() { }
    public OutboxMessage(string eventType,string payload,DateTimeOffset occurredAtUtc)
    { Id=Guid.NewGuid(); EventType=eventType; Payload=payload; OccurredAtUtc=occurredAtUtc; NextAttemptAtUtc=occurredAtUtc; }
    public Guid Id { get; private set; }
    public string EventType { get; private set; }=string.Empty;
    public string Payload { get; private set; }=string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public Guid? LeaseId { get; private set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }
    public void AcquireLease(Guid leaseId,DateTimeOffset expiresAtUtc){LeaseId=leaseId;LeaseExpiresAtUtc=expiresAtUtc;}
    public void MarkPublished(DateTimeOffset now){PublishedAtUtc=now;LeaseId=null;LeaseExpiresAtUtc=null;LastError=null;}
    public void RecordFailure(DateTimeOffset now,Exception exception)
    {
        Attempts++; LastError=exception.Message.Length>2000?exception.Message[..2000]:exception.Message;
        LeaseId=null;LeaseExpiresAtUtc=null;
        if(Attempts>=10){DeadLetteredAtUtc=now;return;}
        var delaySeconds=Math.Min(300,Math.Pow(2,Attempts)*5);
        NextAttemptAtUtc=now.AddSeconds(delaySeconds);
    }
}
public sealed record OrderProcessingRequested(Guid OrderId,DateTimeOffset RequestedAtUtc);