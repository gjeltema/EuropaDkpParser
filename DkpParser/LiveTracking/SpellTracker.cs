// -----------------------------------------------------------------------
// SpellTracker.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using System.Collections.Immutable;
using System.Diagnostics;
using Gjeltema.Logging;

public sealed class SpellTracker
{
    private const string BeginCasting = "You begin casting ";
    private const string Interrupted = "Your spell is interrupted.";
    private const string LogPrefix = $"[{nameof(SpellTracker)}]";
    private const string Resisted = "Your target resisted the ";
    private const string SpellRecoverNotMet = "Spell recovery time not yet met.";
    private readonly IEqLogTailFile _eqLogTailFile;
    private readonly ICollection<SpellTrackingConfiguration> _spellConfigurations;
    private ImmutableList<ActiveSpellInfo> _activeSpells = [];
    private List<SpellTrackingConfiguration> _cachedConfigurations = [];
    private string _currentCharacterName = string.Empty;
    private CastingSpell _currentlyCastingSpell;

    public SpellTracker(IEqLogTailFile eqLogTailFile, IDkpParserSettings settings)
    {
        _eqLogTailFile = eqLogTailFile;
        _spellConfigurations = settings.SpellTrackers;
    }

    public ICollection<ActiveSpellInfo> ActiveSpells
        => _activeSpells;

    public bool Updated
    {
        get
        {
            bool currentValue = field;
            field = false;
            return currentValue;
        }
        set;
    }

    public void StartListening()
    {
        Log.Info($"{LogPrefix} In {nameof(StartListening)}.");
        _eqLogTailFile.SpellInfoMessage += HandleSpellInfoMessage;
        _eqLogTailFile.LogFileChanged += HandleLogFileChanged;
        _eqLogTailFile.StartMessages();
    }

    public void StopListening()
    {
        Log.Info($"{LogPrefix} In {nameof(StopListening)}.");
        _eqLogTailFile.SpellInfoMessage -= HandleSpellInfoMessage;
        _eqLogTailFile.LogFileChanged -= HandleLogFileChanged;
    }

    private bool HandleActiveSpells(string message, DateTime timestamp)
    {
        foreach (ActiveSpellInfo activeSpell in _activeSpells.OrderBy(x => x.StartTime))
        {
            string spellFadedString = activeSpell.BaseInfo.SpellFadedSearchString;
            if (string.IsNullOrEmpty(spellFadedString))
            {
                if ((timestamp - activeSpell.StartTime).TotalSeconds > activeSpell.BaseInfo.EstimatedDuration)
                {
                    _activeSpells = _activeSpells.Remove(activeSpell);
                    Log.Debug($"{LogPrefix} Closed out spell without faded string: {activeSpell}");
                    Updated = true;
                }

                continue;
            }
            else if (message.Contains(activeSpell.BaseInfo.SpellFadedSearchString))
            {
                _activeSpells = _activeSpells.Remove(activeSpell);
                Log.Debug($"{LogPrefix} Spell faded: {activeSpell}");
                Updated = true;
                return true;
            }
        }

        return false;
    }

    private bool HandleCurrentCasting(string message, DateTime timestamp)
    {
        if (_currentlyCastingSpell == null)
            return false;

        if (message.Contains(_currentlyCastingSpell.BaseInfo.SpellLandedSearchString))
        {
            int indexOfSearchString = message.IndexOf(_currentlyCastingSpell.BaseInfo.SpellLandedSearchString);
            string target = message[0..indexOfSearchString];
            ActiveSpellInfo activeSpell = new()
            {
                BaseInfo = _currentlyCastingSpell.BaseInfo,
                StartTime = timestamp,
                Target = target
            };
            _activeSpells = _activeSpells.Add(activeSpell);
            _currentlyCastingSpell = null;
            Updated = true;
            Log.Debug($"{LogPrefix} Spell landed: {activeSpell}");
            return true;
        }
        else if (message.Equals(Interrupted)
            || message.Equals(SpellRecoverNotMet)
            || (message.StartsWith(Resisted) && message.Contains(_currentlyCastingSpell.BaseInfo.SpellName))
            || (timestamp - _currentlyCastingSpell.CastingStart).TotalSeconds > _currentlyCastingSpell.BaseInfo.CastTime + 2)
        {
            Log.Debug($"{LogPrefix} Spell did not land: {_currentlyCastingSpell}");
            _currentlyCastingSpell = null;
            return true;
        }
        return false;
    }

    private void HandleLogFileChanged(object sender, LogFileChangedEventArgs e)
    {
        if (!e.IsReadingFile)
        {
            Log.Debug($"{LogPrefix} Not reading any file.");
            _currentCharacterName = string.Empty;
            _activeSpells.Clear();
            _cachedConfigurations.Clear();
            return;
        }

        string characterName = e.CharacterName;
        if (_currentCharacterName == characterName)
            return;

        Log.Debug($"{LogPrefix} Reading file for character name: {characterName}.");
        _currentCharacterName = characterName;
        _cachedConfigurations = [.. _spellConfigurations.Where(x => x.CasterCharacterName == characterName)];
        Log.Debug($"{LogPrefix} Found {_cachedConfigurations.Count} spell configs for {characterName}.");
    }

    private bool HandleNewSpellCast(string message, DateTime timestamp)
    {
        if (!message.StartsWith(BeginCasting))
            return false;

        foreach (SpellTrackingConfiguration spell in _cachedConfigurations)
        {
            if (message.Contains(spell.SpellName))
            {
                _currentlyCastingSpell = new CastingSpell { BaseInfo = spell, CastingStart = timestamp };
                Log.Debug($"{LogPrefix} Casting spell: {_currentlyCastingSpell}");
                Updated = true;
                return true;
            }
        }

        return false;
    }

    private void HandleSpellInfoMessage(object sender, SpellInfoEventArgs e)
    {
        if (_cachedConfigurations.Count == 0)
            return;

        string message = e.Message;
        DateTime timestamp = e.Timestamp;
        if (HandleCurrentCasting(message, timestamp))
            return;

        if (HandleActiveSpells(message, timestamp))
            return;

        if (HandleNewSpellCast(message, timestamp))
            return;
    }

    [DebuggerDisplay("{DebugText,nq}")]
    private sealed class CastingSpell
    {
        public SpellTrackingConfiguration BaseInfo { get; init; }

        public DateTime CastingStart { get; init; }

        private string DebugText
        => $"{BaseInfo.SpellName}";

        public override string ToString()
            => DebugText;
    }
}

[DebuggerDisplay("{DebugText,nq}")]
public sealed class ActiveSpellInfo
{
    public SpellTrackingConfiguration BaseInfo { get; init; }

    public DateTime StartTime { get; init; }

    public string Target { get; init; }

    private string DebugText
        => $"{BaseInfo.SpellName} {Target} {StartTime:HH:mm:ss}";

    public override string ToString()
        => DebugText;
}
