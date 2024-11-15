// -----------------------------------------------------------------------
// ReminderDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using EuropaDkpParser.Resources;

internal sealed class ReminderDialogViewModel : DialogViewModelBase, IReminderDialogViewModel
{
    private int _reminderInterval;
    private string _reminderText;

    public ReminderDialogViewModel(IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("ReminderDialogTitleText");
        Height = 220;
        Width = 400;

        ReminderInterval = 3;
    }

    public int ReminderInterval
    {
        get => _reminderInterval;
        set => SetProperty(ref _reminderInterval, value);
    }

    public string ReminderText
    {
        get => _reminderText;
        set => SetProperty(ref _reminderText, value);
    }
}

public interface IReminderDialogViewModel : IDialogViewModel
{
    int ReminderInterval { get; set; }

    string ReminderText { get; set; }
}
