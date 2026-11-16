using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Web_prog_server.Services.Items;

public sealed class ItemIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public async Task<ItemIdempotencyResult> ExecuteAsync(
        string key,
        string fingerprint,
        Func<Task<ItemResponse>> operation,
        CancellationToken cancellationToken)
    {
        var proposedEntry = new Entry(fingerprint);
        var entry = _entries.GetOrAdd(key, proposedEntry);

        if (!ReferenceEquals(entry, proposedEntry) &&
            !string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            return ItemIdempotencyResult.Conflict();
        }

        if (ReferenceEquals(entry, proposedEntry))
        {
            try
            {
                var response = await operation();
                entry.Completion.TrySetResult(new Outcome(response, null));
                return ItemIdempotencyResult.Success(response);
            }
            catch (Exception exception)
            {
                _entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
                entry.Completion.TrySetResult(new Outcome(null, exception));
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }
        }

        var outcome = await entry.Completion.Task.WaitAsync(cancellationToken);
        if (outcome.Exception is not null)
        {
            ExceptionDispatchInfo.Capture(outcome.Exception).Throw();
        }

        return ItemIdempotencyResult.Success(outcome.Response!);
    }

    private sealed class Entry(string fingerprint)
    {
        public string Fingerprint { get; } = fingerprint;
        public TaskCompletionSource<Outcome> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed record Outcome(ItemResponse? Response, Exception? Exception);
}

public sealed record ItemIdempotencyResult(ItemResponse? Response, bool IsConflict)
{
    public static ItemIdempotencyResult Success(ItemResponse response) => new(response, false);

    public static ItemIdempotencyResult Conflict() => new(null, true);
}
