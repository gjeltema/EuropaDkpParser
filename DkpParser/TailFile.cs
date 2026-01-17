// -----------------------------------------------------------------------
// TailFile.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;
using System.Threading;
using Gjeltema.Logging;

public sealed class TailFile : IMessageProvider
{
    private const string LogPrefix = $"[{nameof(TailFile)}]";
    private const int Timeout = 500;
    private readonly string _filePath;
    private readonly object _lastUpdateLock = new();
    private readonly Action<string> _lineHandler;
    private CancellationTokenSource _cancellationTokenSource;
    private Thread _fileReaderThread;
    private DateTime _lastUpdate = DateTime.MinValue;

    public TailFile(string filePath, Action<string> lineHandler)
    {
        _filePath = filePath;
        _lineHandler = lineHandler;
    }

    public bool IsSendingMessages
    {
        get
        {
            bool sending = (_fileReaderThread?.IsAlive ?? false) && (DateTime.Now.AddSeconds(-40) < LastUpdate);
            if (!sending)
                Log.Debug($"{LogPrefix} In {nameof(IsSendingMessages)}, Last Update: {LastUpdate:T}, LastUpdate result: {DateTime.Now.AddSeconds(-30) < LastUpdate:T}, Thread is alive: '{_fileReaderThread?.IsAlive}'");
            return sending;
        }
    }

    private DateTime LastUpdate
    {
        get
        {
            lock (_lastUpdateLock)
                return _lastUpdate;
        }
        set
        {
            lock (_lastUpdateLock)
                _lastUpdate = value;
        }
    }

    private string ThreadIdText
        => $" Thread ID [{Environment.CurrentManagedThreadId}]";

    public void StartMessages()
    {
        Log.Info($"{LogPrefix}{ThreadIdText} Entering {nameof(StartMessages)} with file: {_filePath}.");

        if (IsSendingMessages)
        {
            Log.Info($"{LogPrefix} {nameof(IsSendingMessages)} is true, exiting.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_filePath))
            return;

        if (!File.Exists(_filePath))
        {
            Log.Info($"{LogPrefix} File {_filePath} does not exist.  Existing {nameof(StartMessages)}.");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _fileReaderThread = new(() => ReadFile(_cancellationTokenSource.Token));
        _fileReaderThread.IsBackground = true;
        _fileReaderThread.Start();

        LastUpdate = DateTime.Now;
    }

    public void StopMessages()
    {
        Log.Info($"{LogPrefix} Entering {nameof(StopMessages)}, for base thread ID: {Environment.CurrentManagedThreadId}.");

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _fileReaderThread = null;
        LastUpdate = DateTime.MinValue;
    }

    private void ReadFile(CancellationToken cancelToken)
    {
        string filePath = _filePath;
        try
        {
            Log.Info($"{LogPrefix}{ThreadIdText} Starting {nameof(ReadFile)} with filepath: {filePath}.");

            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(fileStream);

            long lastOffset = reader.BaseStream.Length;


            while (!cancelToken.IsCancellationRequested)
            {
                Thread.Sleep(Timeout);

                if (cancelToken.IsCancellationRequested)
                {
                    Log.Info($"{LogPrefix}{ThreadIdText} Cancellation Token cancel requested for filepath: {filePath}.");
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
                        LastUpdate = DateTime.Now;
                        _lineHandler(line);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"{LogPrefix}{ThreadIdText} Error encountered in filepath {filePath} when processing line: {line}{Environment.NewLine}Error: {e.ToLogMessage()}");
                    }
                }

                lastOffset = reader.BaseStream.Position;
            }

            reader.Close();
            reader.DiscardBufferedData();
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix}{ThreadIdText} Error encountered when processing messages from filepath {filePath}: Error: {ex.ToLogMessage()}");
        }

        Log.Info($"{LogPrefix}{ThreadIdText} Leaving {nameof(ReadFile)} for filepath: {filePath}");
    }
}

public interface IMessageProvider
{
    bool IsSendingMessages { get; }

    void StartMessages();

    void StopMessages();
}
