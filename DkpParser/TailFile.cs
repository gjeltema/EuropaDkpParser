﻿// -----------------------------------------------------------------------
// TailFile.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;
using System.Threading;
using Gjeltema.Logging;

public sealed class TailFile : IMessageProvider
{
    private const string LogPrefix = $"[{nameof(TailFile)}]";
    private const int Timeout = 500;
    private CancellationTokenSource _cancellationTokenSource;
    private string _filePath;
    private Thread _fileReaderThread;
    private Action<string> _lineHandler;

    public void StartMessages(string filePath, Action<string> lineHandler)
    {
        Log.Info($"{LogPrefix} Entering {nameof(StartMessages)} with file: {filePath}");

        if (string.IsNullOrWhiteSpace(filePath))
            return;

        StopMessages();

        _lineHandler = lineHandler;

        _filePath = filePath;

        _cancellationTokenSource = new CancellationTokenSource();
        _fileReaderThread = new(() => ReadFile(_cancellationTokenSource.Token));
        _fileReaderThread.IsBackground = true;
        _fileReaderThread.Start();
    }

    public void StopMessages()
    {
        Log.Info($"{LogPrefix} Entering {nameof(StopMessages)}.");

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _fileReaderThread = null;
    }

    private void ReadFile(CancellationToken cancelToken)
    {
        string filePath = _filePath;
        try
        {
            Log.Info($"{LogPrefix} Starting {nameof(ReadFile)} with filepath: {filePath}.");

            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fileStream);

            long lastOffset = reader.BaseStream.Length;

            while (!cancelToken.IsCancellationRequested)
            {
                Thread.Sleep(Timeout);

                if (cancelToken.IsCancellationRequested)
                {
                    Log.Info($"{LogPrefix} Cancellation Token cancel requested.");
                    break;
                }

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
                        Log.Error($"{LogPrefix} Error encountered when processing line: {line}{Environment.NewLine}Error: {e.ToLogMessage()}");
                    }
                }

                lastOffset = reader.BaseStream.Position;
            }

            reader.Close();
            reader.DiscardBufferedData();
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error encountered when processing messages: Error: {ex.ToLogMessage()}");
        }

        Log.Info($"{LogPrefix} Leaving {nameof(ReadFile)} for filepath: {filePath}");
    }
}

public interface IMessageProvider
{
    void StartMessages(string filePath, Action<string> lineHandler);

    void StopMessages();
}
