namespace Simple3DViewer.wpf.Misc;

/// <summary>
/// 执行一个作用域结束时的回调操作，常用于清理窗口或资源。
/// </summary>
public sealed class DeferredScope(Action onDispose) : IDisposable
{
    private readonly Action _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _onDispose.Invoke();
    }
}
