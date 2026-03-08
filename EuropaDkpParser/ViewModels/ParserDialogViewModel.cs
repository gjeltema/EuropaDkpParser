// -----------------------------------------------------------------------
// ParserDialogViewModel.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using System.Windows;
using DkpParser;
using EuropaDkpParser.Resources;
using EuropaDkpParser.Utility;
using Prism.Commands;

internal class ParserDialogViewModel : DialogViewModelBase, IParserDialogViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly DkpLogGenerator _logGenerator;
    private readonly ParsedFileGenerator _parsedFileGenerator;
    private readonly IDkpParserSettings _settings;

    internal ParserDialogViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory, IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("ParserDialogTitleText");

        _settings = settings;
        _dialogFactory = dialogFactory;
        _logGenerator = new(settings, dialogFactory);
        _parsedFileGenerator = new(settings, dialogFactory);

        ResetTimeCommand = new DelegateCommand(ResetTime);
        GetConversationCommand = new DelegateCommand(ParseConversation, () => !PerformingParse && !string.IsNullOrWhiteSpace(ConversationPlayer) && !string.IsNullOrWhiteSpace(_settings.OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => ConversationPlayer).ObservesProperty(() => PerformingParse);
        GetAllCommunicationCommand = new DelegateCommand(GetAllCommunication, () => !PerformingParse && !string.IsNullOrWhiteSpace(_settings.OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => PerformingParse);
        GetSearchTermCommand = new DelegateCommand(GetSearchTerm, () => !PerformingParse && !string.IsNullOrWhiteSpace(SearchTermText) && !string.IsNullOrWhiteSpace(_settings.OutputDirectory))
           .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => SearchTermText).ObservesProperty(() => PerformingParse);
        OpenGeneralParserCommand = new DelegateCommand(OpenGeneralParser);
        GetRaidSummaryCommand = new DelegateCommand(GetRaidSummary, () => !PerformingParse && !string.IsNullOrWhiteSpace(_settings.OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => PerformingParse);
    }

    public string ConversationPlayer { get; set => SetProperty(ref field, value); }

    public string EndTimeText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (DateTime.TryParse(value, out DateTime endTime))
                {
                    StartTimeText = endTime.AddHours(-6).ToString(Constants.TimePickerDisplayDateTimeFormat);
                }
            }
        }
    }

    public DelegateCommand GetAllCommunicationCommand { get; }

    public DelegateCommand GetConversationCommand { get; }

    public DelegateCommand GetRaidSummaryCommand { get; }

    public DelegateCommand GetSearchTermCommand { get; }

    public bool IncludeTells { get; set => SetProperty(ref field, value); }

    public bool IsCaseSensitive { get; set => SetProperty(ref field, value); }

    public DelegateCommand OpenGeneralParserCommand { get; }

    public DelegateCommand ResetTimeCommand { get; }

    public string SearchTermText { get; set => SetProperty(ref field, value); }

    public string StartTimeText { get; set => SetProperty(ref field, value); }

    private bool PerformingParse { get; set => SetProperty(ref field, value); }

    private async Task ExecuteParse(Func<DateTime, DateTime, Task> parseToExecute)
    {
        if (!_logGenerator.ValidateTimeSettings(StartTimeText, EndTimeText, out DateTime startTime, out DateTime endTime))
            return;

        try
        {
            PerformingParse = true;
            RefreshCommands();

            await parseToExecute(startTime, endTime);
        }
        finally
        {
            PerformingParse = false;
            RefreshCommands();
        }
    }

    private async void GetAllCommunication()
    {
        if (!TimesAreValid())
            return;

        await ExecuteParse(GetAllCommunicationAsync);
    }

    private async Task GetAllCommunicationAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.GetAllCommunicationAsync(startTime, endTime, GetOutputPath());

    private string GetOutputPath()
        => string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? _logGenerator.GetUserProfilePath() : _settings.OutputDirectory;

    private async void GetRaidSummary()
    {
        if (!TimesAreValid())
            return;

        await ExecuteParse(GetRaidSummaryAsync);
    }

    private async Task GetRaidSummaryAsync(DateTime startTime, DateTime endTime)
       => await _parsedFileGenerator.GetRaidSummaryAsync(startTime, endTime, IncludeTells, GetOutputPath());

    private async void GetSearchTerm()
    {
        if (!TimesAreValid())
            return;

        await ExecuteParse(GetSearchTermAsync);
    }

    private async Task GetSearchTermAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.GetSearchTermAsync(startTime, endTime, SearchTermText, IsCaseSensitive, GetOutputPath());

    private void OpenGeneralParser()
    {
        IGeneralEqLogParserDialogViewModel parser = _dialogFactory.CreateGeneralEqParserDialogViewModel(_dialogFactory, _settings);
        if (parser.ShowDialog() != true)
            return;
    }

    private async void ParseConversation()
    {
        if (!TimesAreValid())
            return;

        await ExecuteParse(ParseConversationAsync);
    }

    private async Task ParseConversationAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.ParseConversationAsync(startTime, endTime, ConversationPlayer, GetOutputPath());

    private void RefreshCommands()
    {
        GetConversationCommand.RaiseCanExecuteChanged();
        GetSearchTermCommand.RaiseCanExecuteChanged();
        GetAllCommunicationCommand.RaiseCanExecuteChanged();
        GetRaidSummaryCommand.RaiseCanExecuteChanged();
    }

    private void ResetTime()
    {
        DateTime currentTime = DateTime.Now;
        EndTimeText = currentTime.ToString(Constants.TimePickerDisplayDateTimeFormat);
        StartTimeText = currentTime.AddHours(-6).ToString(Constants.TimePickerDisplayDateTimeFormat);
    }

    private bool TimesAreValid()
    {
        if (!DateTime.TryParse(StartTimeText, out DateTime startTime))
        {
            MessageBox.Show(Strings.GetString("StartTimeErrorMessage"), Strings.GetString("StartTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (!DateTime.TryParse(EndTimeText, out DateTime endTime))
        {
            MessageBox.Show(Strings.GetString("EndTimeErrorMessage"), Strings.GetString("EndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }
}

public interface IParserDialogViewModel : IDialogViewModel
{
    string ConversationPlayer { get; set; }

    string EndTimeText { get; set; }

    DelegateCommand GetAllCommunicationCommand { get; }

    DelegateCommand GetConversationCommand { get; }

    DelegateCommand GetRaidSummaryCommand { get; }

    DelegateCommand GetSearchTermCommand { get; }

    bool IncludeTells { get; set; }

    bool IsCaseSensitive { get; set; }

    DelegateCommand OpenGeneralParserCommand { get; }

    DelegateCommand ResetTimeCommand { get; }

    string SearchTermText { get; set; }

    string StartTimeText { get; set; }
}
