// -----------------------------------------------------------------------
// WindowViewModelBase.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.ComponentModel;

internal abstract class WindowViewModelBase : EuropaViewModelBase, IWindowViewModel
{
    public event EventHandler WindowClosing;

    private readonly IWindowViewFactory _viewFactory;
    private IWindowView _windowView;

    protected WindowViewModelBase(IWindowViewFactory viewFactory)
    {
        _viewFactory = viewFactory;
    }

    public void Show()
    {
        _windowView = CreateWindowView(_viewFactory);
        _windowView.Closing += HandleWindowClosing;
        _windowView.Show();
    }

    protected abstract IWindowView CreateWindowView(IWindowViewFactory viewFactory);

    protected virtual void HandleClosing()
    {
    }

    private void HandleWindowClosing(object sender, CancelEventArgs e)
    {
        HandleClosing();
        WindowClosing?.Invoke(this, EventArgs.Empty);
    }
}

public interface IWindowViewModel : IEuropaViewModel
{
    event EventHandler WindowClosing;

    void Show();
}
