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

    private ZealNamedPipe() { }

    public static ZealNamedPipe Instance { get; } = new();

    public bool IsConnected { get; private set; } = false;

    public void StartListening(IZealMessageUpdater messageUpdater)
    {
        Log.Debug($"{LogPrefix} In {nameof(StartListening)}.");

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

            Thread messageListenerThread = new(() => ProcessZealPipeMessages(pipeName, messageUpdater, _cancelTokenSource.Token));
            messageListenerThread.IsBackground = true;

            Log.Info($"{LogPrefix} Spawning thread to listen to pipe, pipe name {pipeName}.");
            messageListenerThread.Start();
        }
    }

    public void StopListening()
    {
        Log.Info($"{LogPrefix} {nameof(StopListening)} called.");

        _cancelTokenSource?.Cancel();
        _cancelTokenSource?.Dispose();
        _cancelTokenSource = null;
    }

    private void ProcessZealPipeMessages(string pipeName, IZealMessageUpdater messageUpdater, CancellationToken cancelToken)
    {
        try
        {
            Log.Info($"{LogPrefix} Starting Zeal Pipe listener, pipe name {pipeName}.");

            ZealMessageProcessor messageProcessor = new(messageUpdater);

            using (NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.In))
            {
                pipeClient.Connect();

                Log.Debug($"{LogPrefix} Zeal Pipe connected.");
                IsConnected = true;

                Span<char> charBuffer = new char[Constants.ZealPipeBufferSize];
                Span<byte> pipeReadBytes = new byte[Constants.ZealPipeBufferSize];

                while (!cancelToken.IsCancellationRequested)
                {
                    if (!pipeClient.IsConnected)
                    {
                        Log.Info($"{LogPrefix} Zeal Pipe is no longer connected");
                        break;
                    }

                    int bytesRead = pipeClient.Read(pipeReadBytes);

                    if (cancelToken.IsCancellationRequested)
                    {
                        Log.Info($"{LogPrefix} CancelToken cancellation requested.");
                        break;
                    }

                    if (bytesRead > 0)
                    {
                        int charsWritten = Encoding.UTF8.GetChars(pipeReadBytes[..bytesRead], charBuffer);
                        if (charsWritten == 0)
                        {
                            Log.Debug($"{LogPrefix} Unable to decode chars from message");
                            continue;
                        }

                        Log.Trace($"{LogPrefix} Zeal message received.  CharsWritten: {charsWritten}.  Message: {charBuffer[..charsWritten].ToString()}");

                        try
                        {
                            if (!messageProcessor.ProcessMessage(charBuffer[..charsWritten], charsWritten))
                            {
                                Log.Info($"{LogPrefix} Ending listening to pipe {pipeName}.");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"{LogPrefix} Error processing zeal message: {ex.ToLogMessage()}");
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
            Log.Error($"{LogPrefix} Pipe object disposed error: {ioex.ToLogMessage()}");
            messageUpdater.SendPipeError("Zeal named pipe was disposed.  Close Bid Tracker window and reopen to re-connect.", ioex);
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Pipe read error: {ex.ToLogMessage()}");
            messageUpdater.SendPipeError("Unexpected error with Zeal named pipe.  Close Bid Tracker window and reopen to re-connect.", ex);
        }

        IsConnected = false;
        Log.Info($"{LogPrefix} Exiting listening to Zeal Pipe name: {pipeName}.  CancelToken IsCancelled:{cancelToken.IsCancellationRequested}");
    }
}
