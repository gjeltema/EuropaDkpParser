// -----------------------------------------------------------------------
// MainDisplayViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using Prism.Commands;

internal sealed class MainDisplayViewModel : EuropaViewModelBase, IMainDisplayViewModel
{
    internal MainDisplayViewModel(IDkpParserSettings settings)
    {

    }

    public string EndTimeText { get; set; }

    public DelegateCommand OpenSettingsDialogCommand { get; }

    public string OutputFile { get; set; }

    public DelegateCommand SelectEndTimeCommand { get; }

    public DelegateCommand SelectStartTimeCommand { get; }

    public DelegateCommand StartLogParseCommand { get; }

    public string StartTimeText { get; set; }
}

public interface IMainDisplayViewModel : IEuropaViewModel
{
    string EndTimeText { get; set; }

    DelegateCommand OpenSettingsDialogCommand { get; }

    string OutputFile { get; set; }

    DelegateCommand SelectEndTimeCommand { get; }

    DelegateCommand SelectStartTimeCommand { get; }

    DelegateCommand StartLogParseCommand { get; }

    string StartTimeText { get; set; }
}
