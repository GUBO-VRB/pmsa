namespace pmsa.Security;

/// <summary>
/// Serialises the operations that could violate the <em>At least one active Admin</em> invariant.
/// </summary>
/// <remarks>
/// EC-5 requires that two Admins deactivating each other at the same moment cannot leave the
/// system with zero active Admins. The check ("how many active Admins are there?") and the write
/// have to be one atomic step; SQLite has no way to express that as a constraint, so the pair is
/// serialised here and then committed in a transaction.
/// <para>
/// This holds for a single application instance, which is what a SQLite single-file store implies
/// anyway. Moving to a multi-instance deployment means moving this check into the database — see
/// arc42 section 09, ADR-004.
/// </para>
/// </remarks>
public sealed class AdminInvariantLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Release(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Release(SemaphoreSlim semaphore) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                semaphore.Release();
            }
        }
    }
}
