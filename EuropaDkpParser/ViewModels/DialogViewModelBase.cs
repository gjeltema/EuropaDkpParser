// -----------------------------------------------------------------------
// DialogViewModelBase.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using Prism.Commands;

internal abstract class DialogViewModelBase : EuropaViewModelBase, IDialogViewModel
{
    private bool? _dialogResult;
    private string _title;

    protected DialogViewModelBase(IDialogViewFactory viewFactory)
    {
        ViewFactory = viewFactory;
        CloseOkCommand = new DelegateCommand(CloseOk, CloseOkCanExecute);
        CloseCancelCommand = new DelegateCommand(CloseCancel);
    }

    public DelegateCommand CloseCancelCommand { get; }

    public DelegateCommand CloseOkCommand { get; }

    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }

    public int Height { get; set; } = 500;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public int Width { get; set; } = 700;

    protected IDialogViewFactory ViewFactory { get; private set; }

    public void CloseCancel()
        => DialogResult = false;

    public void CloseOk()
        => DialogResult = true;

    public virtual bool? ShowDialog()
        => CreateAndShowDialog();

    protected virtual bool CloseOkCanExecute()
        => true;

    protected bool? CreateAndShowDialog()
    {
        IDialogView view = ViewFactory.CreateDialogView(this);
        view.Height = Height;
        view.Width = Width;
        return view.ShowDialog();
    }
}

public interface IDialogViewModel : IEuropaViewModel
{
    DelegateCommand CloseCancelCommand { get; }

    DelegateCommand CloseOkCommand { get; }

    bool? DialogResult { get; set; }

    int Height { get; set; }

    string Title { get; set; }

    int Width { get; set; }

    void CloseCancel();

    void CloseOk();

    bool? ShowDialog();
}
