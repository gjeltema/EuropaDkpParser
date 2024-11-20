// -----------------------------------------------------------------------
// TailFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;
using System.Threading;

public sealed class TailFile : IMessageProvider
{
    private const int Timeout = 500;
    private CancellationTokenSource _cancellationTokenSource;
    private Action<string> _errorMessage;
    private string _filePath;
    private Thread _fileReaderThread;
    private Action<string> _lineHandler;

    public void StartMessages(string filePath, Action<string> lineHandler, Action<string> errorMessage)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        StopMessages();

        _lineHandler = lineHandler;
        _errorMessage = errorMessage;

        _filePath = filePath;

        _cancellationTokenSource = new CancellationTokenSource();
        _fileReaderThread = new(() => ReadFile(_cancellationTokenSource.Token));
        _fileReaderThread.IsBackground = true;
        _fileReaderThread.Start();
    }

    public void StopMessages()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _fileReaderThread = null;
    }

    private void ReadFile(CancellationToken cancelToken)
    {
        try
        {
            using FileStream fileStream = new(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fileStream);

            long lastOffset = reader.BaseStream.Length;

            while (!cancelToken.IsCancellationRequested)
            {
                Thread.Sleep(Timeout);

                if (cancelToken.IsCancellationRequested)
                    break;

                if (reader.BaseStream.Length == lastOffset)
                    continue;

                reader.BaseStream.Seek(lastOffset, SeekOrigin.Begin);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        _lineHandler(line);
                    }
                    catch (Exception e)
                    {
                        _errorMessage($"Error encountered when processing line: {line}{Environment.NewLine}Error: {e.Message}");
                    }
                }

                lastOffset = reader.BaseStream.Position;
            }

            reader.Close();
            reader.DiscardBufferedData();
        }
        catch (Exception ex)
        {
            _errorMessage($"Error encountered when processing messages: Error: {ex.Message}");
        }
    }
}

public interface IMessageProvider
{
    void StartMessages(string filePath, Action<string> lineHandler, Action<string> errorMessage);

    void StopMessages();
}
