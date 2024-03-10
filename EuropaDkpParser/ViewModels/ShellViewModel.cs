namespace EuropaDkpParser.ViewModels;

internal sealed class ShellViewModel : IShellViewModel
{
    internal ShellViewModel()
    {
        MainDisplayVM = new MainDisplayViewModel();
    }

    public IMainDisplayViewModel MainDisplayVM { get; }
}

public interface IShellViewModel
{
    IMainDisplayViewModel MainDisplayVM { get; }
}
