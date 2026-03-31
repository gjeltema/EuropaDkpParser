// -----------------------------------------------------------------------
// SpellTrackerOverlayViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows.Threading;
using DkpParser;
using DkpParser.LiveTracking;

internal sealed class SpellTrackerOverlayViewModel : OverlayViewModelBase, ISpellTrackerOverlayViewModel
{
    private readonly IDkpParserSettings _settings;
    private readonly SpellTracker _spellTracker;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1);
    private readonly DispatcherTimer _updateTimer;

    public SpellTrackerOverlayViewModel(IOverlayViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        _settings = settings;

        AllowResizing = true;
        _spellTracker = new SpellTracker(EqLogTailFile.Instance, _settings);
        _spellTracker.StartListening();

        XPos = _settings.SpellTrackerXLoc;
        YPos = _settings.SpellTrackerYLoc;
        Height = _settings.SpellTrackerHeight;
        Width = _settings.SpellTrackerWidth;

        _updateTimer = new(_updateInterval, DispatcherPriority.Normal, HandleUpdate, Dispatcher.CurrentDispatcher);
    }

    public ICollection<SpellTrackerItemViewModel> Spells { get; set => SetProperty(ref field, value); }

    protected override void HandleClose()
    {
        _updateTimer.Stop();
        _spellTracker.StopListening();
    }

    protected override void SaveLocation()
    {
        _settings.SpellTrackerXLoc = XPos;
        _settings.SpellTrackerYLoc = YPos;
        _settings.SpellTrackerWidth = Width;
        _settings.SpellTrackerHeight = Height;
        _settings.SaveSettings();
    }

    private void HandleUpdate(object sender, EventArgs e)
        => UpdateDisplay();

    private void UpdateDisplay()
    {
        if (_spellTracker.ActiveSpells.Count == 0 && !_spellTracker.Updated)
            return;

        Spells = [.. _spellTracker.ActiveSpells.Select(x => new SpellTrackerItemViewModel(x))];
    }
}

public interface ISpellTrackerOverlayViewModel : IOverlayViewModel
{
    ICollection<SpellTrackerItemViewModel> Spells { get; }
}

public sealed class SpellTrackerItemViewModel : EuropaViewModelBase
{
    public SpellTrackerItemViewModel(ActiveSpellInfo spellInfo)
    {
        string targetName = spellInfo.Target.Length > 12 ? spellInfo.Target[0..10] + "..." : spellInfo.Target;
        double secondsRemaining = spellInfo.BaseInfo.EstimatedDuration - (DateTime.Now - spellInfo.StartTime).TotalSeconds;
        TextDisplay = $"{spellInfo.BaseInfo.DisplayName} - {targetName}: {(int)secondsRemaining}s";

        Color = spellInfo.BaseInfo.DisplayColor;

        PercentRemaining = 0;
        if (secondsRemaining > 0 && secondsRemaining <= spellInfo.BaseInfo.EstimatedDuration)
            PercentRemaining = (int)(secondsRemaining * 100 / spellInfo.BaseInfo.EstimatedDuration);
    }

    public string Color { get; }

    public int PercentRemaining { get; set; }

    public string TextDisplay { get; }
}
