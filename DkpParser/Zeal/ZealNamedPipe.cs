// -----------------------------------------------------------------------
// ZealNamedPipe.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Gjeltema.Logging;

public sealed class ZealNamedPipe
{
    private const string LogPrefix = $"[{nameof(ZealNamedPipe)}]";
    private CancellationTokenSource _cancellationTokenSource;
    private string _characterName;

    private ZealNamedPipe() { }

    public static ZealNamedPipe Instance
        => new();

    public void StartListening(string characterName, IZealMessageUpdater messageUpdater)
    {
        if (_cancellationTokenSource != null)
        {
            StopListening();
        }

        _characterName = characterName;
        _cancellationTokenSource = new CancellationTokenSource();

        Process[] eqProcesses = Process.GetProcessesByName(Constants.EqProcessName);
        foreach (Process eqProcess in eqProcesses)
        {
            string pipeName = string.Format(Constants.ZealPipeNameFormat, eqProcess.Id.ToString());

            Thread messageListenerThread = new(() => ProcessZealPipeMessages(pipeName, messageUpdater, _cancellationTokenSource.Token));
            messageListenerThread.IsBackground = true;
            messageListenerThread.Start();
        }
    }

    public void StopListening()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }

    private void ProcessZealPipeMessages(string pipeName, IZealMessageUpdater messageUpdater, CancellationToken cancelToken)
    {
        try
        {
            Log.Debug($"{LogPrefix} Starting Zeal Pipe listener.");

            ZealMessageProcessor messageProcessor = new(messageUpdater);

            using (NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.In))
            {
                pipeClient.Connect();

                Log.Debug($"{LogPrefix} Zeal Pipe connected.");

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
                        if (!Encoding.UTF8.TryGetChars(pipeReadBytes, charBuffer, out int charsWritten))
                            continue;

                        Log.Trace($"{LogPrefix} Zeal message received: {charBuffer.ToString()}");

                        try
                        {
                            if (!messageProcessor.ProcessMessage(charBuffer, charsWritten, _characterName))
                            {
                                Log.Info($"{LogPrefix} Ending listening to pipe.");
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
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Pipe read error: {ex.ToLogMessage()}");
        }

        Log.Info($"{LogPrefix} Exiting listening to Zeal Pipe name: {pipeName}.  CancelToken IsCancelled:{cancelToken.IsCancellationRequested}");
    }
}
