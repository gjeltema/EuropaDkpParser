// -----------------------------------------------------------------------
// LogSelectionViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Collections.Generic;
using DkpParser;

internal sealed class LogSelectionViewModel : DialogViewModelBase, ILogSelectionViewModel
{
    internal LogSelectionViewModel(IDialogViewFactory viewFactory, IDkpParserSettings settings)
        : base(viewFactory)
    {
    }

    public IEnumerable<string> AllCharacterLogFiles { get; } = ["Test1", "Test2", "Test3"];

    public string EqDirectory { get; set; } = "TestDir";

    public IEnumerable<string> SelectedCharacterLogFiles { get; } = ["Test1", "Test2"];
}

public interface ILogSelectionViewModel : IDialogViewModel
{
    IEnumerable<string> AllCharacterLogFiles { get; }

    string EqDirectory { get; set; }

    IEnumerable<string> SelectedCharacterLogFiles { get; }
}
