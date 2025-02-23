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
using Gjeltema.Logging;

//internal sealed class ZealPipe
//{
//    public event EventHandler<ZealPipeMessageEventArgs> ZealPipeMessageReceived;

//    private const string LogPrefix = $"[{nameof(ZealPipe)}]";
//    private CancellationTokenSource _cancellationTokenSource;
//    private string _characterName;

//    private ZealPipe() { }

//    public static ZealPipe Instance
//        => new();

//    public void StartListening(string characterName)
//    {
//        if (_cancellationTokenSource != null)
//        {
//            StopListening();
//        }

//        _characterName = characterName;
//        _cancellationTokenSource = new CancellationTokenSource();

//        Process[] eqProcesses = Process.GetProcessesByName(Constants.EqProcessName);
//        foreach (Process eqProcess in eqProcesses)
//        {
//            string pipeName = string.Format(Constants.ZealPipeNameFormat, eqProcess.Id.ToString());

//            Thread messageListenerThread = new(() => ProcessZealPipeMessages(pipeName, _cancellationTokenSource.Token));
//            messageListenerThread.IsBackground = true;
//            messageListenerThread.Start();
//        }
//    }

//    public void StopListening()
//    {
//        _cancellationTokenSource?.Cancel();
//        _cancellationTokenSource = null;
//    }

//    private bool IsValidCharacter(string characterName)
//    {
//        if (characterName == _characterName)
//            return true;

//        if (characterName.Contains(_characterName) && characterName.Contains("s corpse"))
//            return true;

//        return false;
//    }

//    private void ProcessZealPipeMessages(string pipeName, CancellationToken cancelToken)
//    {
//        try
//        {
//            Log.Debug($"{LogPrefix} Starting Zeal Pipe listener.");

//            using (NamedPipeClientStream pipeClient = new(".", pipeName, PipeDirection.In))
//            {
//                pipeClient.Connect();
//                byte[] buffer = new byte[Constants.ZealPipeBufferSize];

//                Log.Debug($"{LogPrefix} Zeal Pipe connected.");

//                JsonSplitter splitter = new();
//                while (!cancelToken.IsCancellationRequested)
//                {
//                    if (!pipeClient.IsConnected)
//                    {
//                        Log.Info($"{LogPrefix} Zeal Pipe is no longer connected");
//                        break;
//                    }

//                    Array.Clear(buffer);
//                    int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);

//                    if (cancelToken.IsCancellationRequested)
//                    {
//                        Log.Info($"{LogPrefix} CancelToken cancellation requested.");
//                        break;
//                    }

//                    if (bytesRead > 0)
//                    {
//                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
//                        Log.Trace($"{LogPrefix} Zeal message received: {message}");

//                        foreach (string json in splitter.SplitJson(message))
//                        {
//                            try
//                            {
//                                ZealPipeMessage zpm = JsonSerializer.Deserialize<ZealPipeMessage>(json);
//                                if (!IsValidCharacter(zpm.Character))
//                                {
//                                    Log.Info($"{LogPrefix} Message character name {zpm.Character} is not set character name {_characterName}. Ending listening to pipe.");
//                                    return;
//                                }

//                                if (bytesRead >= Constants.ZealPipeBufferSize / 2
//                                    && (zpm.MessageType == PipeMessageType.Gauge || zpm.MessageType == PipeMessageType.Label))
//                                    continue;
//                                ZealPipeMessageReceived?.Invoke(this, new ZealPipeMessageEventArgs { PipeName = pipeName, Message = zpm });
//                            }
//                            catch (JsonException jex)
//                            {
//                                Log.Warning($"{LogPrefix} JSON parsing error: {jex.ToLogMessage()}");
//                            }
//                        }
//                    }
//                    else
//                    {
//                        Thread.Sleep(1);
//                    }
//                }
//            }
//        }
//        catch (IOException ioex) when (ioex.InnerException is ObjectDisposedException)
//        {
//            Log.Error($"{LogPrefix} Pipe object disposed error: {ioex.ToLogMessage()}");
//        }
//        catch (Exception ex)
//        {
//            Log.Error($"{LogPrefix} Pipe read error: {ex.ToLogMessage()}");
//        }

//        Log.Info($"{LogPrefix} Exiting listening to Zeal Pipe name: {pipeName}.  CancelToken IsCancelled:{cancelToken.IsCancellationRequested}");
//    }
//}

//internal sealed class ZealPipeMessageEventArgs
//{
//    public ZealPipeMessage Message { get; init; }

//    public string PipeName { get; init; }
//}

//internal sealed class ZealPipeMessage
//{
//    [JsonPropertyName("character")]
//    public string Character { get; set; }

//    [JsonPropertyName("data")]
//    public string Data { get; set; }

//    [JsonPropertyName("data_len")]
//    public uint DataLength { get; set; }

//    public PipeMessageType MessageType
//        => (PipeMessageType)RawType;

//    [JsonPropertyName("type")]
//    public int RawType { get; set; }
//}
