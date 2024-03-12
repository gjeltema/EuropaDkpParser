// -----------------------------------------------------------------------
// LogSelectionViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Collections.Generic;
using DkpParser;
using Prism.Commands;

internal sealed class LogSelectionViewModel : DialogViewModelBase, ILogSelectionViewModel
{
    private readonly IDkpParserSettings _settings;

    internal LogSelectionViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
        _settings = settings;
    }

    public DelegateCommand AddLogFileToListCommand { get; }

    public ICollection<string> AllCharacterLogFiles { get; } = ["Test1", "Test2", "Test3"];

    public string EqDirectory { get; set; } = "TestDir";

    public ICollection<string> SelectedCharacterLogFiles { get; } = ["Test1", "Test2"];

    public DelegateCommand SelectEqDirectoryCommand { get; }
}

public interface ILogSelectionViewModel : IDialogViewModel
{
    DelegateCommand AddLogFileToListCommand { get; }

    ICollection<string> AllCharacterLogFiles { get; }

    string EqDirectory { get; set; }

    ICollection<string> SelectedCharacterLogFiles { get; }

    DelegateCommand SelectEqDirectoryCommand { get; }
}
