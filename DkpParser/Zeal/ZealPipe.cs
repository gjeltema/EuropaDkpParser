// -----------------------------------------------------------------------
// ZealPipe.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class ZealPipe
{
    public event EventHandler<ZealPipeMessageEventArgs> ZealPipeMessageReceived;

    private CancellationTokenSource _cancellationTokenSource;

    public void StartListening()
    {
        if (_cancellationTokenSource != null)
        {
            StopListening();
        }

        _cancellationTokenSource = new CancellationTokenSource();

        Process[] eqProcesses = Process.GetProcessesByName(Constants.EqProcessName);
        foreach (Process eqProcess in eqProcesses)
        {
            string pipeName = string.Format(Constants.ZealPipeNameFormat, eqProcess.Id.ToString());

            Thread messageListenerThread = new(() => ProcessZealPipeMessages(pipeName, _cancellationTokenSource.Token));
            messageListenerThread.Start();
        }
    }

    public void StopListening()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = null;
    }

    private void ProcessZealPipeMessages(string pipeName, CancellationToken cancelToken)
    {
        try
        {
            using (NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.In))
            {
                pipeClient.Connect();
                byte[] buffer = new byte[Constants.ZealPipeBufferSize];

                JsonSplitter splitter = new();
                while (!cancelToken.IsCancellationRequested)
                {
                    if (!pipeClient.IsConnected)
                        break;

                    Array.Clear(buffer);
                    int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);

                    if (cancelToken.IsCancellationRequested)
                        break;

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        foreach (string json in splitter.SplitJson(message))
                        {
                            try
                            {
                                ZealPipeMessage zpm = JsonSerializer.Deserialize<ZealPipeMessage>(json);
                                if (bytesRead >= Constants.ZealPipeBufferSize / 2
                                    && (zpm.MessageType == PipeMessageType.Gauge || zpm.MessageType == PipeMessageType.Label))
                                    continue;
                                ZealPipeMessageReceived?.Invoke(this, new ZealPipeMessageEventArgs { PipeName = pipeName, Message = zpm });
                            }
                            catch (JsonException)
                            {
                                // Handle JSON parsing error
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }
        catch (IOException ex) when (ex.InnerException is ObjectDisposedException)
        {
        }
        catch (Exception)
        {
        }
    }
}

internal sealed class ZealPipeMessageEventArgs
{
    public ZealPipeMessage Message { get; init; }

    public string PipeName { get; init; }
}

internal sealed class ZealPipeMessage
{
    [JsonPropertyName("character")]
    public string Character { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("data_len")]
    public uint DataLength { get; set; }

    public PipeMessageType MessageType
        => (PipeMessageType)RawType;

    [JsonPropertyName("type")]
    public int RawType { get; set; }
}
