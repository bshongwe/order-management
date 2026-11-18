namespace OrderManagement.Infrastructure.Persistence;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
        // Required by EF Core.
    }

    private IdempotencyRecord(
        string key,
        string requestFingerprint,
        string responseType,
        string responseJson)
    {
        Id = Guid.NewGuid();
        Key = key;
        RequestFingerprint = requestFingerprint;
        ResponseType = responseType;
        ResponseJson = responseJson;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string RequestFingerprint { get; private set; } = string.Empty;

    public string ResponseType { get; private set; } = string.Empty;

    public string ResponseJson { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static IdempotencyRecord Create(
        string key,
        string requestFingerprint,
        string responseType,
        string responseJson) =>
        new(key, requestFingerprint, responseType, responseJson);
}
