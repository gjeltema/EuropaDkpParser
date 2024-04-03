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
    }

    public DelegateCommand CloseOkCommand { get; }

    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

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
        return view.ShowDialog();
    }
}

public interface IDialogViewModel : IEuropaViewModel
{
    DelegateCommand CloseOkCommand { get; }

    bool? DialogResult { get; set; }

    string Title { get; set; }

    void CloseCancel();

    void CloseOk();

    bool? ShowDialog();
}
