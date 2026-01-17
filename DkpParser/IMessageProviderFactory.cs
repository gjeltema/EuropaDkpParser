// -----------------------------------------------------------------------
// IMessageProviderFactory.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class MessageProviderFactory : IMessageProviderFactory
{
    public IMessageProvider CreateTailFileProvider(string filePath, Action<string> lineHandler)
        => new TailFile(filePath, lineHandler);
}

public interface IMessageProviderFactory
{
    IMessageProvider CreateTailFileProvider(string filePath, Action<string> lineHandler);
}
