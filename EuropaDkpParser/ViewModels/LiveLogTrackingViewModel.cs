namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class LiveLogTrackingViewModel : DialogViewModelBase, ILiveLogTrackingViewModel
{
    public LiveLogTrackingViewModel(IDialogViewFactory viewFactory) 
        : base(viewFactory)
    {
        Title = Strings.GetString("LiveLogTrackingDialogTitleText");


    }
}

public interface ILiveLogTrackingViewModel : IDialogViewModel
{

}
