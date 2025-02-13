// -----------------------------------------------------------------------
// ReadyCheckOverlayViewModel.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using DkpParser.LiveTracking;
using Prism.Commands;

internal sealed class ReadyCheckOverlayViewModel : OverlayViewModelBase, IReadyCheckOverlayViewModel
{
    private readonly IDkpParserSettings _settings;
    private ICollection<CharacterReadyCheckStatus> _charactersNotReady = [];

    public ReadyCheckOverlayViewModel(IOverlayViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        _settings = settings;

        XPos = _settings.OverlayLocationX;
        YPos = _settings.OverlayLocationY;

        Hide = new DelegateCommand(HideOverlay);
    }

    public ICollection<CharacterReadyCheckStatus> CharactersNotReady
    {
        get => _charactersNotReady;
        private set => SetProperty(ref _charactersNotReady, value);
    }

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

    public void Show()
    {
        XPos = _settings.OverlayLocationX;
        YPos = _settings.OverlayLocationY;

        CreateAndShowOverlay();
    }
}

public interface IReadyCheckOverlayViewModel : IOverlayViewModel
{
    ICollection<CharacterReadyCheckStatus> CharactersNotReady { get; }

    DelegateCommand Hide { get; }

    void SetCharacterReadyStatus(CharacterReadyCheckStatus newCharacterStatus);

    void SetInitialCharacterList(IEnumerable<string> characterNames);

    void Show();
}
