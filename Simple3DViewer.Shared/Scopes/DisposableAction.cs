namespace Simple3DViewer.Shared.Scopes;

public sealed class DisposableAction : IDisposable
{
    private Action? _onExit;

    public DisposableAction(Action onExit)
    {
        _onExit = onExit ?? (() => { });
    }

    public void Dispose()
    {
        Action? a = Interlocked.Exchange(ref _onExit, null);
        a?.Invoke();
    }
}