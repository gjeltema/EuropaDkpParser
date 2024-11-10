// -----------------------------------------------------------------------
// TailFile.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;
using System.Threading;

internal sealed class TailFile : IMessageProvider
{
    private const int Timeout = 500;
    private readonly Action<string> _errorMessage;
    private readonly Action<string> _lineHandler;
    private volatile bool _continueProcessing = true;
    private string _filePath;
    private Thread _fileReaderThread;

    public TailFile(Action<string> lineHandler, Action<string> errorMessage)
    {
        _lineHandler = lineHandler;
        _errorMessage = errorMessage;
    }

    public void StartMessages(string filePath)
    {
        _filePath = filePath;
        _continueProcessing = true;

        _fileReaderThread = new(ReadFile);
        _fileReaderThread.IsBackground = true;
        _fileReaderThread.Start();
    }

    public void StopMessages()
    {
        _continueProcessing = false;
        _fileReaderThread = null;
    }

    private void ReadFile()
    {
        try
        {
            using FileStream fileStream = new(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fileStream);

            long lastOffset = reader.BaseStream.Length;

            while (_continueProcessing)
            {
                Thread.Sleep(Timeout);

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
    void StartMessages(string filePath);

    void StopMessages();
}
