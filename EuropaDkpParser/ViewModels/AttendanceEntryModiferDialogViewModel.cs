

namespace EuropaDkpParser.ViewModels;

internal sealed class AttendanceEntryModiferDialogViewModel : DialogViewModelBase, IAttendanceEntryModiferDialogViewModel
{
    public AttendanceEntryModiferDialogViewModel(IDialogViewFactory viewFactory) 
        : base(viewFactory)
    {
    }
}

public interface IAttendanceEntryModiferDialogViewModel : IDialogViewModel
{

}
