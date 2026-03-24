// -----------------------------------------------------------------------
// ReadyCheckOverlayViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using DkpParser.LiveTracking;
using Prism.Commands;

internal sealed class ReadyCheckOverlayViewModel : OverlayViewModelBase, IReadyCheckOverlayViewModel
{
    private readonly IDkpParserSettings _settings;

    public ReadyCheckOverlayViewModel(IOverlayViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        _settings = settings;

        AllowResizing = true;

        XPos = _settings.ReadyCheckOverlayXLoc;
        YPos = _settings.ReadyCheckOverlayYLoc;
        Height = _settings.ReadyCheckOverlayHeight;
        Width = _settings.ReadyCheckOverlayWidth;

        Hide = new DelegateCommand(HideOverlay);
    }

    public ICollection<CharacterReadyCheckStatus> CharactersNotReady { get; private set => SetProperty(ref field, value); } = [];

    public DelegateCommand Hide { get; }

    public void SetCharacterReadyStatus(CharacterReadyCheckStatus newCharacterStatus)
    {
        if (!ContentIsVisible)
            return;

        CharacterReadyCheckStatus characterStatus = CharactersNotReady.FirstOrDefault(x => x.CharacterName == newCharacterStatus.CharacterName);

        if (characterStatus == null)
            return;

        if (newCharacterStatus.IsReady == true)
        {
            CharactersNotReady.Remove(characterStatus);
            CharactersNotReady = [.. CharactersNotReady];
        }
        else if (newCharacterStatus.IsReady == false)
        {
            characterStatus.IsReady = false;
            CharactersNotReady = [.. CharactersNotReady];
        }
    }

    public void SetInitialCharacterList(IEnumerable<string> characterNames)
        => CharactersNotReady = characterNames.Select(x => new CharacterReadyCheckStatus { CharacterName = x, IsReady = null }).ToList();

    protected override void SaveLocation()
    {
        _settings.ReadyCheckOverlayXLoc = XPos;
        _settings.ReadyCheckOverlayYLoc = YPos;
        _settings.ReadyCheckOverlayWidth = Width;
        _settings.ReadyCheckOverlayHeight = Height;
        _settings.SaveSettings();
    }
}

public interface IReadyCheckOverlayViewModel : IOverlayViewModel
{
    ICollection<CharacterReadyCheckStatus> CharactersNotReady { get; }

    DelegateCommand Hide { get; }

    void SetCharacterReadyStatus(CharacterReadyCheckStatus newCharacterStatus);

    void SetInitialCharacterList(IEnumerable<string> characterNames);
}
