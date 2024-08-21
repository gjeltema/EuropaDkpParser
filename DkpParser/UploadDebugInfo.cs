// -----------------------------------------------------------------------
// UploadDebugInfo.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class UploadDebugInfo : IUploadDebugInfo
{
    private readonly List<string> _messages = new(1000);

    public void AddDebugMessage(string message)
    {
        if (message == null)
            return;

        _messages.Add($"[{DateTime.Now:yyyy MM dd HH:mm:ss.fff}] {message}");
    }

    public IEnumerable<string> GetFullDebugInfo()
        => _messages;
}

public sealed class NullUploadDebugInfo : IUploadDebugInfo
{
    public void AddDebugMessage(string message)
    { }

    public IEnumerable<string> GetFullDebugInfo()
        => [];
}

public interface IUploadDebugInfo
{
    void AddDebugMessage(string message);

    IEnumerable<string> GetFullDebugInfo();
}
