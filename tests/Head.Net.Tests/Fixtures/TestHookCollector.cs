namespace Head.Net.Tests.Fixtures;

public sealed class TestHookCollector
{
    private readonly List<string> _executedHooks = new();

    public IReadOnlyList<string> ExecutedHooks => _executedHooks.AsReadOnly();

    public void Record(string hookName) => _executedHooks.Add(hookName);

    public void Clear() => _executedHooks.Clear();

    public bool WasHookCalled(string hookName) => _executedHooks.Contains(hookName);

    public IReadOnlyList<string> GetExecutionOrder() => _executedHooks.AsReadOnly();
}
