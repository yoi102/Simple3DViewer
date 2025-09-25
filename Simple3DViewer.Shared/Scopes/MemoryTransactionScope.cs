using ODA.Kernel.TD_RootIntegrated;

namespace Simple3DViewer.Shared.Scopes;

public sealed class MemoryTransactionScope : IDisposable
{
    private readonly MemoryManager _memoryManager;
    private readonly MemoryTransaction _transactionHandle;

    public MemoryTransactionScope()
    {
        _memoryManager = MemoryManager.GetMemoryManager();
        _transactionHandle = _memoryManager.StartTransaction();
    }

    public MemoryManager Manager => _memoryManager;

    public void Dispose()
    {
        _memoryManager.StopTransaction(_transactionHandle);
    }
}