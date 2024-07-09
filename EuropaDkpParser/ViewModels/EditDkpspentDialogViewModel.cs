// -----------------------------------------------------------------------
// EditDkpspentDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using EuropaDkpParser.Resources;

internal sealed class EditDkpspentDialogViewModel : DialogViewModelBase, IEditDkpspentDialogViewModel
{
    private string _dkpSpent;
    private string _itemName;
    private string _playerName;

    public EditDkpspentDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("EditDkpspentDialogTitleText");
    }

    public string DkpSpent
    {
        get => _dkpSpent;
        set => SetProperty(ref _dkpSpent, value);
    }

    public string ItemName
    {
        get => _itemName;
        set => SetProperty(ref _itemName, value);
    }

    public string PlayerName
    {
        get => _playerName;
        set => SetProperty(ref _playerName, value);
    }
}

public interface IEditDkpspentDialogViewModel : IDialogViewModel
{
    string DkpSpent { get; set; }

    string ItemName { get; set; }

    string PlayerName { get; set; }
}
