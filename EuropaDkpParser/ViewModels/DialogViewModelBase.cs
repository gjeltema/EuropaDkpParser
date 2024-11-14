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

        Width = 700;
        Height = 500;
    }

    public DelegateCommand CloseCancelCommand { get; }

    public DelegateCommand CloseOkCommand { get; }

    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }

    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
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
