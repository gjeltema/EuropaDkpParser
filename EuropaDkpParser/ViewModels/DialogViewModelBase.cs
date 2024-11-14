// -----------------------------------------------------------------------
// DialogViewModelBase.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using Prism.Commands;

internal abstract class DialogViewModelBase : EuropaViewModelBase, IDialogViewModel
{
    private bool? _dialogResult;
    private int _height;
    private string _title;
    private int _width;

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
        => CreateAndShowDialog(500, 700);

    public bool? ShowDialog(int height, int width)
        => CreateAndShowDialog(height, width);

    protected virtual bool CloseOkCanExecute()
        => true;

    protected bool? CreateAndShowDialog(int height, int width)
    {
        IDialogView view = ViewFactory.CreateDialogView(this);
        view.Height = height;
        view.Width = width;
        return view.ShowDialog();
    }
}

public interface IDialogViewModel : IEuropaViewModel
{
    DelegateCommand CloseCancelCommand { get; }

    DelegateCommand CloseOkCommand { get; }

    bool? DialogResult { get; set; }

    string Title { get; set; }

    void CloseCancel();

    void CloseOk();

    bool? ShowDialog();

    bool? ShowDialog(int height, int width);
}
