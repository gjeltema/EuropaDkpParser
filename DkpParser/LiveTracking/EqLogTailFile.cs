// -----------------------------------------------------------------------
// EqLogTailFile.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Collections.Immutable;
using System.Timers;
using DkpParser;
using DkpParser.Zeal;
using Gjeltema.Logging;

public sealed class EqLogTailFile : IEqLogTailFile
{
    public event EventHandler<AfkCommandEventArgs> AfkCommandMessage;
    public event EventHandler<AuctionStartEventArgs> AuctionStartMessage;
    public event EventHandler<BidInfoEventArgs> BidInfoMessage;
    public event EventHandler<BossKilledEventArgs> BossKilledMessage;
    public event EventHandler<CharacterReadyCheckEventArgs> CharacterReadyCheckMessage;
    public event EventHandler<LogFileChangedEventArgs> LogFileChanged;
    public event EventHandler<MezBreakEventArgs> MezBreakMessage;
    public event EventHandler<EventArgs> ReadyCheckInitiatedMessage;
    public event EventHandler<RawRollInfoEventArgs> RollMessage;
    public event EventHandler<SpellInfoEventArgs> SpellInfoMessage;
    public event EventHandler<LiveSpentCallEventArgs> SpentCallMessage;

    private const string LogPrefix = $"[{nameof(EqLogTailFile)}]";
    private const string MezBreakIdentifier = " is no longer mezzed. (";
    private readonly DelimiterStringSanitizer _sanitizer = new();
    private ActiveBiddingAnalyzer _activeBiddingAnalyzer;
    private ActiveBossKillAnalyzer _activeBossKillAnalyzer;
    private ActiveAuctionEndAnalyzer _auctionEndAnalyzer;
    private ImmutableList<string> _auctionItems;
    private ActiveAuctionStartAnalyzer _auctionStartAnalyzer;
    private ChannelAnalyzer _channelAnalyzer;
    private string _logFilePath;
    private IMessageProviderFactory _messageProviderFactory;
    private IDkpParserSettings _settings;
    private IMessageProvider _tailFile;
    private Timer _updateTimer;

    private EqLogTailFile() { }

    public static EqLogTailFile Instance { get; } = new EqLogTailFile();

    public bool IsSendingMessages
        => _tailFile?.IsSendingMessages ?? false;

    public void AddAuctionItem(string itemName)
    {
        // Allow duplicates - multiple listeners may be listening for the same item, and one
        // may "close" tracking its auction earlier than the other.
        _auctionItems = _auctionItems.Add(itemName);
    }

    public void Initialize(IDkpParserSettings settings, IMessageProviderFactory messageProviderFactory)
    {
        _settings = settings;
        _messageProviderFactory = messageProviderFactory;

        _channelAnalyzer = new(settings);
        _auctionStartAnalyzer = new();
        _auctionEndAnalyzer = new();
        _activeBiddingAnalyzer = new(settings);
        _activeBossKillAnalyzer = new(settings.RaidValue);

        _auctionItems = [];

        _updateTimer = new Timer(6000);
        _updateTimer.Elapsed += CheckAndSetTailFile;
    }

    public void RemoveAuctionItem(string itemName)
    {
        _auctionItems = _auctionItems.Remove(itemName);
    }

    public void StartMessages()
    {
        _updateTimer.Start();
    }

    public void StartMessages(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Log.Warning($"{LogPrefix} {nameof(filePath)} is null or whitespace.  Exiting {nameof(StartMessages)}.");
            return;
        }

        if (filePath == _logFilePath)
        {
            Log.Debug($"{LogPrefix} {nameof(filePath)} value of {filePath} matches {nameof(_logFilePath)} value of {_logFilePath}.  Exiting {nameof(StartMessages)}.");
            return;
        }

        string characterName = _settings.GetCharacterNameFromLogFileName(filePath);
        InformListenersOfFileChange(filePath, characterName, string.IsNullOrWhiteSpace(characterName));

        SetTailFile(filePath);
        StartMessages();
    }

    private void CheckAndSetTailFile(object sender, ElapsedEventArgs args)
    {
        if (!ZealAttendanceMessageProvider.Instance.IsConnected)
        {
            ZealAttendanceMessageProvider.Instance.StartMessageProcessing();
            return;
        }

        if (IsSendingMessages)
            return;

        string currentCharacterName = ZealAttendanceMessageProvider.Instance.CharacterInfo.CharacterName;
        bool isZealConnected = ZealAttendanceMessageProvider.Instance.IsConnected && !ZealAttendanceMessageProvider.Instance.CharacterInfo.IsDataStale;
        if (!isZealConnected)
        {
            Log.Debug($"{LogPrefix} {nameof(CheckAndSetTailFile)}: Not reading log file and zeal is not connected.");
            InformListenersOfFileChange(string.Empty, currentCharacterName, false);
            return;
        }

        string logFilePath = _settings.GetLogFileForCharacter(currentCharacterName);
        if (logFilePath != null)
        {
            Log.Debug($"{LogPrefix} {nameof(CheckAndSetTailFile)}: Not reading log file, starting to read {logFilePath}.");
            SetTailFile(logFilePath);
            InformListenersOfFileChange(logFilePath, currentCharacterName, true);
            return;
        }

        InformListenersOfFileChange(logFilePath, currentCharacterName, false);
        Log.Debug($"{LogPrefix} {nameof(CheckAndSetTailFile)}: Not reading log file, Zeal is connected, but character name does not have a matching log file configured.");
    }

    private bool CheckForAfk(string message, string messageSenderName)
    {
        if (AfkCommandMessage == null)
            return false;

        AfkCommandEventArgs afk = GetAfkCommand(message, messageSenderName);
        if (afk != null)
        {
            AfkCommandMessage?.Invoke(this, afk);
            return true;
        }

        return false;
    }

    private bool CheckForAuctionStart(string message, DateTime timestamp, string messageSenderName, EqChannel channel)
    {
        if (AuctionStartMessage == null)
            return false;

        ICollection<LiveAuctionInfo> auctionStarts = _auctionStartAnalyzer.GetAuctionStart(message, channel, timestamp, messageSenderName);
        if (auctionStarts.Count > 0)
        {
            foreach (LiveAuctionInfo auction in auctionStarts)
            {
                AuctionStartMessage?.Invoke(this, new AuctionStartEventArgs { AuctionStart = auction });
            }
            return true;
        }

        return false;
    }

    private bool CheckForBid(string message, DateTime timestamp, string messageSenderName, EqChannel channel, bool isValidDkpChannel)
    {
        if (!isValidDkpChannel || BidInfoMessage == null)
            return false;

        RawBidInfo rawBidInfo = _activeBiddingAnalyzer.GetBidInfo(message, channel, timestamp, messageSenderName, _auctionItems);
        if (rawBidInfo != null)
        {
            BidInfoMessage?.Invoke(this, new BidInfoEventArgs { BidInfo = rawBidInfo });
            return true;
        }

        return false;
    }

    private bool CheckForBossKill(string message)
    {
        if (BossKilledMessage == null)
            return false;

        string bossKilledName = _activeBossKillAnalyzer.GetBossKillName(message);
        if (!string.IsNullOrWhiteSpace(bossKilledName))
        {
            BossKilledMessage?.Invoke(this, new BossKilledEventArgs { BossName = bossKilledName });
            return true;
        }

        return false;
    }

    private bool CheckForMezBreak(string message, DateTime timestamp)
    {
        if (MezBreakMessage == null)
            return false;

        MezBreak mezBreak = GetMezBreak(message, timestamp);
        if (mezBreak != null)
        {
            MezBreakMessage?.Invoke(this, new MezBreakEventArgs { MezBreak = mezBreak });
            return true;
        }

        return false;
    }

    private bool CheckForReadyCheck(string message, bool isValidDkpChannel, EqChannel channel)
    {
        if ((ReadyCheckInitiatedMessage != null || CharacterReadyCheckMessage != null)
                && (isValidDkpChannel || channel == EqChannel.ReadyCheck))
        {
            bool isReadyCheckMessage = ParseReadyCheck(message);
            if (isReadyCheckMessage)
                return true;
        }

        return false;
    }

    private bool CheckForRoll(string message, DateTime timestamp)
    {
        if (RollMessage == null)
            return false;

        bool isRoll = _activeBiddingAnalyzer.CheckIfRoll(message, timestamp, out RawRollInfo rollInfo);
        if (isRoll)
        {
            if (rollInfo != null)
            {
                RollMessage?.Invoke(this, new RawRollInfoEventArgs { RollInfo = rollInfo });
            }

            return true;
        }

        return false;
    }

    private bool CheckForSpent(string message, DateTime timestamp, string messageSenderName, EqChannel channel)
    {
        if (SpentCallMessage == null)
            return false;

        LiveSpentCall spentCall = _auctionEndAnalyzer.GetSpentCall(message, channel, timestamp, messageSenderName);
        if (spentCall != null)
        {
            SpentCallMessage?.Invoke(this, new LiveSpentCallEventArgs { SpentCall = spentCall });
            return true;
        }

        return false;
    }

    private AfkCommandEventArgs GetAfkCommand(string logLineNoTimestamp, string messageSenderName)
    {
        string noWhitespaceLogline = logLineNoTimestamp.RemoveAllWhitespace();
        if (noWhitespaceLogline.ContainsIgnoreCase(Constants.AfkWithDelimiter) || noWhitespaceLogline.ContainsIgnoreCase(Constants.AfkAlternateDelimiter))
        {
            return new AfkCommandEventArgs { CharacterName = messageSenderName, StartAfk = true };
        }
        else if (noWhitespaceLogline.ContainsIgnoreCase(Constants.AfkEndWithDelimiter) || noWhitespaceLogline.ContainsIgnoreCase(Constants.AfkEndAlternateDelimiter))
        {
            return new AfkCommandEventArgs { CharacterName = messageSenderName, StartAfk = false };
        }

        return null;
    }

    private string GetMessageSenderName(ReadOnlySpan<char> logLine)
    {
        int indexOfSpace = logLine.IndexOf(' ');
        if (indexOfSpace < 3)
            return string.Empty;

        string auctioneerName = logLine[0..indexOfSpace].Trim().ToString();
        return auctioneerName;
    }

    private MezBreak GetMezBreak(string logLineNoTimestamp, DateTime timestamp)
    {
        // [Fri Dec 05 20:02:17 2025] a fetid fiend is no longer mezzed. (Haight - melee)
        // [Fri Dec 12 20:20:13 2025] Amygdalan knight is no longer mezzed. (Naddin - Upheaval)

        if (MezBreakMessage == null)
            return null;

        if (!logLineNoTimestamp.Contains(MezBreakIdentifier))
            return null;

        string[] mezBreakInfo = logLineNoTimestamp.Split(MezBreakIdentifier);
        if (mezBreakInfo.Length != 2)
            return null;

        string mobName = mezBreakInfo[0];
        string characterAndReason = mezBreakInfo[1];
        int indexOfDash = characterAndReason.IndexOf('-');
        if (indexOfDash < 5)
            return null;

        string characterName = characterAndReason[0..(indexOfDash - 1)];
        string reason = characterAndReason[(indexOfDash + 2)..(characterAndReason.Length - 1)];

        return new MezBreak
        {
            CharacterName = characterName,
            MobName = mobName,
            Reason = reason,
            TimeOfBreak = timestamp
        };
    }

    private void InformListenersOfFileChange(string filePath, string characterName, bool isReadingFile)
    {
        try
        {
            LogFileChanged?.Invoke(this, new LogFileChangedEventArgs { LogFile = filePath, CharacterName = characterName, IsReadingFile = isReadingFile });
        }
        catch (Exception ex)
        {
            Log.Warning($"{LogPrefix} Listener encountered an error when being informed of updated file {filePath} with character {characterName}: {ex.ToLogMessage()}");
        }
    }

    private bool ParseReadyCheck(string logLineNoTimestamp)
    {
        if (!logLineNoTimestamp.Contains(Constants.PossibleErrorDelimiter) && !logLineNoTimestamp.Contains(Constants.AlternateDelimiter))
            return false;

        string sanitizedLogLine = _sanitizer.SanitizeDelimiterString(logLineNoTimestamp);
        string noWhitespaceLogLine = sanitizedLogLine.RemoveAllWhitespace();

        if (noWhitespaceLogLine.Contains(Constants.ReadyCheckWithDelimiter) || noWhitespaceLogLine.Contains(Constants.ReadyCheckAlternateDelimiter))
        {
            Log.Debug($"{LogPrefix} Ready Check initiated: {logLineNoTimestamp}");
            ReadyCheckInitiatedMessage?.Invoke(this, EventArgs.Empty);

            return true;
        }
        else if (noWhitespaceLogLine.ContainsIgnoreCase(Constants.ReadyWithDelimiter)
            || noWhitespaceLogLine.ContainsIgnoreCase(Constants.ReadyAlternateDelimiter))
        {
            string senderName = GetMessageSenderName(logLineNoTimestamp);
            CharacterReadyCheckMessage?.Invoke(this, new CharacterReadyCheckEventArgs { ReadyCheckStatus = new CharacterReadyCheckStatus { CharacterName = senderName, IsReady = true } });
            Log.Debug($"{LogPrefix} {senderName} is READY: {logLineNoTimestamp}");

            return true;
        }
        else if (noWhitespaceLogLine.ContainsIgnoreCase(Constants.NotReadyWithDelimiter)
            || noWhitespaceLogLine.ContainsIgnoreCase(Constants.NotReadyAlternateDelimiter))
        {
            string senderName = GetMessageSenderName(logLineNoTimestamp);
            CharacterReadyCheckMessage?.Invoke(this, new CharacterReadyCheckEventArgs { ReadyCheckStatus = new CharacterReadyCheckStatus { CharacterName = senderName, IsReady = false } });
            Log.Debug($"{LogPrefix} {senderName} is NOT READY: {logLineNoTimestamp}");

            return true;
        }

        return false;
    }

    private void ProcessMessage(string message)
    {
        Log.Trace($"{LogPrefix} {nameof(message)}: {message}");
        if (!message.TryExtractEqLogTimeStamp(out DateTime timestamp))
        {
            Log.Debug($"{LogPrefix} Message has no parsable timestamp: {message}");
            return;
        }

        try
        {
            // +1 to remove the following space.
            string logLineNoTimestamp = message[(Constants.EqLogDateTimeLength + 1)..];

            if (CheckForMezBreak(logLineNoTimestamp, timestamp))
                return;

            if (CheckForBossKill(logLineNoTimestamp))
                return;

            if (CheckForRoll(logLineNoTimestamp, timestamp))
                return;

            EqChannel channel = _channelAnalyzer.GetChannel(logLineNoTimestamp);
            if (channel == EqChannel.None)
            {
                SpellInfoMessage?.Invoke(this, new SpellInfoEventArgs { Message = logLineNoTimestamp, Timestamp = timestamp });
                return;
            }

            bool isValidDkpChannel = _channelAnalyzer.IsValidDkpChannel(channel);

            if (CheckForReadyCheck(logLineNoTimestamp, isValidDkpChannel, channel))
                return;

            // Include Group so that the tool can be used in xp groups
            if (!isValidDkpChannel && channel != EqChannel.Group)
                return;

            string messageSenderName = GetMessageSenderName(logLineNoTimestamp);
            int indexOfFirstQuote = logLineNoTimestamp.IndexOf('\'');
            string messageFromPlayer = logLineNoTimestamp.AsSpan()[(indexOfFirstQuote + 1)..^1].Trim().ToString();

            if (CheckForAuctionStart(messageFromPlayer, timestamp, messageSenderName, channel))
                return;

            if (CheckForSpent(messageFromPlayer, timestamp, messageSenderName, channel))
                return;

            if (CheckForBid(messageFromPlayer, timestamp, messageSenderName, channel, isValidDkpChannel))
                return;

            if (CheckForAfk(logLineNoTimestamp, messageSenderName))
                return;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error processing message: {ex.ToLogMessage()}");
        }
    }

    private void SetTailFile(string logFilePath)
    {
        _tailFile?.StopMessages();
        _tailFile = null;
        if (string.IsNullOrEmpty(logFilePath))
            return;

        _logFilePath = logFilePath;
        _tailFile = _messageProviderFactory.CreateTailFileProvider(logFilePath, ProcessMessage);
        _tailFile.StartMessages();
    }
}

public interface IEqLogTailFile
{
    event EventHandler<AfkCommandEventArgs> AfkCommandMessage;
    event EventHandler<AuctionStartEventArgs> AuctionStartMessage;
    event EventHandler<BidInfoEventArgs> BidInfoMessage;
    event EventHandler<BossKilledEventArgs> BossKilledMessage;
    event EventHandler<CharacterReadyCheckEventArgs> CharacterReadyCheckMessage;
    event EventHandler<LogFileChangedEventArgs> LogFileChanged;
    event EventHandler<MezBreakEventArgs> MezBreakMessage;
    event EventHandler<EventArgs> ReadyCheckInitiatedMessage;
    event EventHandler<RawRollInfoEventArgs> RollMessage;
    event EventHandler<SpellInfoEventArgs> SpellInfoMessage;
    event EventHandler<LiveSpentCallEventArgs> SpentCallMessage;

    bool IsSendingMessages { get; }

    void AddAuctionItem(string itemName);

    void RemoveAuctionItem(string itemName);

    void StartMessages();

    void StartMessages(string filePath);
}

public sealed class AfkCommandEventArgs : EventArgs
{
    public string CharacterName { get; init; }

    public bool StartAfk { get; init; }
}

public sealed class AuctionStartEventArgs : EventArgs
{
    public LiveAuctionInfo AuctionStart { get; init; }
}

public sealed class BidInfoEventArgs : EventArgs
{
    public RawBidInfo BidInfo { get; init; }
}

public sealed class BossKilledEventArgs : EventArgs
{
    public string BossName { get; init; }
}
public sealed class CharacterReadyCheckEventArgs : EventArgs
{
    public CharacterReadyCheckStatus ReadyCheckStatus { get; init; }
}

public sealed class LiveSpentCallEventArgs : EventArgs
{
    public LiveSpentCall SpentCall { get; init; }
}

public sealed class LogFileChangedEventArgs : EventArgs
{
    public string CharacterName { get; init; }

    public bool IsReadingFile { get; init; }

    public string LogFile { get; init; }
}

public sealed class MezBreakEventArgs : EventArgs
{
    public MezBreak MezBreak { get; init; }
}

public sealed class RawRollInfoEventArgs : EventArgs
{
    public RawRollInfo RollInfo { get; init; }
}

public sealed class SpellInfoEventArgs : EventArgs
{
    public string Message { get; init; }

    public DateTime Timestamp { get; init; }
}
