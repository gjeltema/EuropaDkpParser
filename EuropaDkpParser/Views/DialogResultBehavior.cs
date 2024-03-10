// -----------------------------------------------------------------------
// DialogResultBehavior.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Views
{
    using System.Windows;

    public static class DialogResultBehavior
    {
        public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached(
            "DialogResult", typeof(bool?), typeof(DialogResultBehavior), new PropertyMetadata(default(bool?), DialogResultChanged));

        public static void SetDialogResult(Window target, bool? value)
            => target.SetValue(DialogResultProperty, value);

        private static void DialogResultChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Window window && window.IsVisible)
                window.DialogResult = e.NewValue as bool?;
        }
    }
}
