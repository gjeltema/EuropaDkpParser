// -----------------------------------------------------------------------
// ZealMessageProcessor.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Text.Json;
using System.Text.RegularExpressions;
using Gjeltema.Logging;

internal sealed partial class ZealMessageProcessor
{
    private const string CharacterKey = "\"character\":";
    private const string ClassKey = "\"class\":";
    private const string DataPrefix = @"data"":""";
    private const string GroupKey = "\"group\":";
    private const string LevelKey = "\"level\":";
    private const string LogPrefix = $"[{nameof(ZealMessageProcessor)}]";
    private const string NameKey = "\"name\":";
    private const string RankKey = "\"rank\":";
    private const string TypeKey = "\"type\":";
    private readonly char[] _dataBuffer = new char[20000];
    private readonly IZealMessageUpdater _messageUpdater;
    private List<ZealRaidCharacter> _raidCharacterBuffer = new(73);

    public ZealMessageProcessor(IZealMessageUpdater messageUpdater)
    {
        _messageUpdater = messageUpdater;
    }

    [GeneratedRegex(DataPrefix + @"\[(.*?)\]")]
    public static partial Regex BracketDataFieldRegex();

    [GeneratedRegex(@"(?<=\})\s*(?=\{)")]
    public static partial Regex JsonMessageSplitRegex();

    [GeneratedRegex(DataPrefix + @"\{((?<NEST>\{)|\}(?<-NEST>)|[^{}]*)*\}")]
    public static partial Regex ParenDataFieldRegex();

    [GeneratedRegex(@"\{((?<NEST>\{)|\}(?<-NEST>)|[^{}]*)*\}")]
    public static partial Regex ParenDataRegex();

    public bool ProcessMessage(ReadOnlySpan<char> fullMessage, int charsInMessage)
    {
        int currentIndex = 0;

        foreach (ValueMatch match in JsonMessageSplitRegex().EnumerateMatches(fullMessage))
        {
            int matchIndex = match.Index;
            ReadOnlySpan<char> message = fullMessage[currentIndex..matchIndex];
            ReadOnlySpan<char> characterName = GetStringValue(message, CharacterKey);

            int currentMessageType = GetMessageType(message);
            ReadOnlySpan<char> dataField = GetDataFromMessage(message, currentMessageType);
            ParseMessage(dataField, currentMessageType);

            currentIndex = matchIndex + match.Length;
        }

        ReadOnlySpan<char> remainder = fullMessage[currentIndex..charsInMessage].TrimEnd();
        if (!remainder.EndsWith("}"))
        {
            Log.Debug($"{LogPrefix} Incomplete remainder message: {remainder.ToString()}");
            return false;
        }

        int messageType = GetMessageType(remainder);
        ReadOnlySpan<char> messageCharacterName = GetStringValue(remainder, CharacterKey);

        ReadOnlySpan<char> remainderDataField = GetDataFromMessage(remainder, messageType);
        ParseMessage(remainderDataField, messageType);

        return true;
    }

    private ReadOnlySpan<char> GetDataFromMessage(ReadOnlySpan<char> message, int messageType)
    {
        if (messageType == 3)
        {
            foreach (ValueMatch match in ParenDataFieldRegex().EnumerateMatches(message))
            {
                ReadOnlySpan<char> matchingChars = message.Slice(match.Index + DataPrefix.Length, match.Length - DataPrefix.Length);
                ReadOnlySpan<char> prunedData = SanitizeData(matchingChars);
                return prunedData;
            }
            throw new ZealMessageProcessingException($"Unable to match parentheses Data Field with regex: {message.ToString()}");
        }
        else if (messageType == 5)
        {
            foreach (ValueMatch match in BracketDataFieldRegex().EnumerateMatches(message))
            {
                ReadOnlySpan<char> matchingChars = message.Slice(match.Index + DataPrefix.Length, match.Length - DataPrefix.Length);
                ReadOnlySpan<char> prunedData = SanitizeData(matchingChars);
                return prunedData;
            }
            throw new ZealMessageProcessingException($"Unable to match bracket Data Field with regex: {message.ToString()}");
        }
        return message;
    }

    private ZealRaidCharacter GetMatchingCharacter(ReadOnlySpan<char> name)
    {
        foreach (ZealRaidCharacter character in _messageUpdater.GetRaidAttendees())
        {
            if (MemoryExtensions.Equals(character.Name, name, StringComparison.Ordinal))
                return character;
        }

        return null;
    }

    private int GetMessageType(ReadOnlySpan<char> message)
    {
        // ...,"type":5}
        int indexOfType = message.LastIndexOf(TypeKey);
        int startIndex = indexOfType + TypeKey.Length;
        int endIndex = message[startIndex..].IndexOf("}") + startIndex; //** Need better way to find end of type number
        ReadOnlySpan<char> typeNumberText = message[startIndex..endIndex];
        if (int.TryParse(typeNumberText, out int messageType))
            return messageType;

        Log.Debug($"{LogPrefix} Unable to extract message type from message: {message.ToString()}");
        return -1;
    }

    private ReadOnlySpan<char> GetStringValue(ReadOnlySpan<char> message, string key)
    {
        int indexOfKey = message.IndexOf(key);
        if (indexOfKey < 0)
        {
            Log.Debug($"Unable to extract field {key.ToString()} from message, key not found: {message.ToString()}");
            return string.Empty;
        }

        int startIndex = indexOfKey + key.Length + 1;
        int endIndex = message[startIndex..].IndexOf("\"") + startIndex;
        if (message.Length <= endIndex)
        {
            Log.Debug($"Unable to extract field {key.ToString()} from message, index out of range.  StartIndex: {startIndex}, EndIndex: {endIndex}: {message.ToString()}");
            return string.Empty;
        }

        ReadOnlySpan<char> stringValue = message[startIndex..endIndex];
        return stringValue;
    }

    private bool IsCurrentName(ReadOnlySpan<char> messageCharacter, ReadOnlySpan<char> currentCharacter)
    {
        if (MemoryExtensions.Equals(messageCharacter, currentCharacter, StringComparison.Ordinal))
            return true;

        if (messageCharacter.Contains(currentCharacter, StringComparison.Ordinal) && messageCharacter.Contains("s corpse", StringComparison.Ordinal))
            return true;

        Log.Info($"{LogPrefix} Message character name {messageCharacter.ToString()} is not set character name {currentCharacter.ToString()}.");
        return false;
    }

    private void ParseMessage(ReadOnlySpan<char> message, int messageType)
    {
        try
        {
            switch (messageType)
            {
                case 3:
                    ParsePlayerMessage(message);
                    break;
                case 5:
                    ParseRaidMessage(message);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"Error parsing message type {messageType}: {message.ToString()}{Environment.NewLine}Exception:{ex.ToLogMessage()}");
        }
    }

    private void ParsePlayerMessage(ReadOnlySpan<char> message)
    {
        ZealCharacterInfo characterInfo = JsonSerializer.Deserialize(message, ZealCharacterInfoGenerationContext.Default.ZealCharacterInfo);
        _messageUpdater.SetCharacterInfo(characterInfo);
    }

    private void ParseRaidMessage(ReadOnlySpan<char> message)
    {
        _raidCharacterBuffer.Clear();

        foreach (ValueMatch match in ParenDataRegex().EnumerateMatches(message))
        {
            ReadOnlySpan<char> characterInfoText = message.Slice(match.Index, match.Length);
            ReadOnlySpan<char> name = GetStringValue(characterInfoText, NameKey);
            ReadOnlySpan<char> groupNumber = GetStringValue(characterInfoText, GroupKey);
            ReadOnlySpan<char> raidRank = GetStringValue(characterInfoText, RankKey);
            ReadOnlySpan<char> characterLevel = GetStringValue(characterInfoText, LevelKey);

            ZealRaidCharacter existingCharInfo = GetMatchingCharacter(name);
            if (existingCharInfo == null)
            {
                ReadOnlySpan<char> className = GetStringValue(characterInfoText, ClassKey);
                ZealRaidCharacter newCharacter = new()
                {
                    Name = name.ToString(),
                    Class = className.ToString(),
                    Level = characterLevel.ToString(),
                    Group = groupNumber.ToString(),
                    Rank = raidRank.ToString()
                };
                _raidCharacterBuffer.Add(newCharacter);
            }
            else
            {
                existingCharInfo.Rank = raidRank.ToString();
                existingCharInfo.Group = groupNumber.ToString();
                _raidCharacterBuffer.Add(existingCharInfo);
            }
        }

        List<ZealRaidCharacter> tempAttendees = _messageUpdater.GetRaidAttendees();
        _messageUpdater.SetRaidAttendees(_raidCharacterBuffer);
        _raidCharacterBuffer = tempAttendees;
    }

    private ReadOnlySpan<char> SanitizeData(ReadOnlySpan<char> matchingChars)
    {
        int bufferIndex = 0;
        for (int i = 0; i < matchingChars.Length; i++)
        {
            char currentChar = matchingChars[i];
            if (currentChar != '\\')
            {
                _dataBuffer[bufferIndex] = currentChar;
                bufferIndex++;
            }
        }

        return _dataBuffer.AsSpan()[..bufferIndex];
    }
}
