// -----------------------------------------------------------------------
// MessageDialog.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using EuropaDkpParser.ViewModels;

public static class MessageDialog
{
    private static IDialogFactory _dialogFactory;

    public static void Initialize(IDialogFactory dialogFactory)
    {
        _dialogFactory = dialogFactory;
    }

    public static void ShowDialog(string message, string title, int height = 150, int width = 400, int fontSize = 12)
    {
        ISimpleMultilineDisplayDialogViewModel dialog = _dialogFactory.CreateSimpleMultilineDisplayDialogViewModel();
        dialog.DisplayLines = message;
        dialog.Title = title;
        dialog.Height = height;
        dialog.Width = width;
        dialog.DisplayFontSize = fontSize;

        dialog.ShowDialog();
    }
}
