// -----------------------------------------------------------------------
// ZealNamedPipe.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Gjeltema.Logging;

internal sealed class ZealNamedPipe
{
    private const string LogPrefix = $"[{nameof(ZealNamedPipe)}]";
    private CancellationTokenSource _cancelTokenSource;
    private Thread _messageListenerThread;

    private ZealNamedPipe() { }

    public static ZealNamedPipe Instance { get; } = new();

    public bool IsConnected
        => _messageListenerThread?.IsAlive ?? false;

    private string ThreadIdText
        => $" Thread ID [{Environment.CurrentManagedThreadId}]";

    public void StartListening(IZealMessageUpdater messageUpdater)
    {
        Log.Debug($"{LogPrefix}{ThreadIdText} In {nameof(StartListening)}.");

        if (_cancelTokenSource != null)
        {
            StopListening();
        }

        _cancelTokenSource = new();

        Process[] eqProcesses = Process.GetProcessesByName(Constants.EqProcessName);
        Log.Debug($"{LogPrefix} EQ Processes found: {string.Join(',', eqProcesses.Select(x => $"{x.ProcessName}-{x.Id}"))}");

        Process eqProcess = eqProcesses.FirstOrDefault();
        if (eqProcess != null)
        {
            string pipeName = string.Format(Constants.ZealPipeNameFormat, eqProcess.Id.ToString());

            _messageListenerThread = new(() => ProcessZealPipeMessages(pipeName, messageUpdater, _cancelTokenSource.Token));
            _messageListenerThread.IsBackground = true;

            Log.Info($"{LogPrefix} Spawning thread to listen to pipe, pipe name {pipeName}.");
            _messageListenerThread.Start();
        }
    }

    public void StopListening()
    {
        Log.Info($"{LogPrefix} Thread ID: {Environment.CurrentManagedThreadId} - {nameof(StopListening)} called.");

        _cancelTokenSource?.Cancel();
        _cancelTokenSource?.Dispose();
        _cancelTokenSource = null;
    }

    private void ProcessZealPipeMessages(string pipeName, IZealMessageUpdater messageUpdater, CancellationToken cancelToken)
    {
        try
        {
            Log.Info($"{LogPrefix}{ThreadIdText} Starting Zeal Pipe listener, pipe name {pipeName}.");

            ZealMessageProcessor messageProcessor = new(messageUpdater);

            using (NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.In))
            {
                pipeClient.Connect();

                Log.Debug($"{LogPrefix}{ThreadIdText} Zeal Pipe connected.");

                Span<char> charBuffer = new char[Constants.ZealPipeBufferSize];
                Span<byte> pipeReadBytes = new byte[Constants.ZealPipeBufferSize];

                while (!cancelToken.IsCancellationRequested)
                {
                    if (!pipeClient.IsConnected)
                    {
                        Log.Info($"{LogPrefix}{ThreadIdText} Zeal Pipe is no longer connected");
                        break;
                    }

                    int bytesRead = pipeClient.Read(pipeReadBytes);

                    if (cancelToken.IsCancellationRequested)
                    {
                        Log.Info($"{LogPrefix}{ThreadIdText} CancelToken cancellation requested.");
                        break;
                    }

                    if (bytesRead > 0)
                    {
                        int charsWritten = Encoding.UTF8.GetChars(pipeReadBytes[..bytesRead], charBuffer);
                        if (charsWritten == 0)
                        {
                            Log.Debug($"{LogPrefix}{ThreadIdText} Unable to decode chars from message");
                            continue;
                        }

                        Log.Trace($"{LogPrefix}{ThreadIdText} Zeal message received.  CharsWritten: {charsWritten}.  Message: {charBuffer[..charsWritten].ToString()}");

                        try
                        {
                            if (!messageProcessor.ProcessMessage(charBuffer[..charsWritten], charsWritten))
                            {
                                Log.Info($"{LogPrefix}{ThreadIdText} Ending listening to pipe {pipeName}.");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"{LogPrefix}{ThreadIdText} Error processing zeal message: {ex.ToLogMessage()}");
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }
        catch (IOException ioex) when (ioex.InnerException is ObjectDisposedException)
        {
            Log.Error($"{LogPrefix}{ThreadIdText} Pipe object disposed error: {ioex.ToLogMessage()}");
            messageUpdater.SendPipeError("Zeal named pipe was disposed.  Close Bid Tracker window and reopen to re-connect.", ioex);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix}{ThreadIdText} Pipe read error: {ex.ToLogMessage()}");
            messageUpdater.SendPipeError("Unexpected error with Zeal named pipe.  Close Bid Tracker window and reopen to re-connect.", ex);
        }

        Log.Info($"{LogPrefix}{ThreadIdText} Exiting listening to Zeal Pipe name: {pipeName}.  CancelToken IsCancelled:{cancelToken.IsCancellationRequested}");
    }
}
