// -----------------------------------------------------------------------
// ParserDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

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
    private string _conversationPlayer;
    private string _endTimeText;
    private bool _isCaseSensitive;
    private bool _performingParse = false;
    private string _searchTermText;
    private string _startTimeText;

    internal ParserDialogViewModel(IDkpParserSettings settings, IDialogFactory dialogFactory, IDialogViewFactory viewFactory)
        : base(viewFactory)
    {
        Title = Strings.GetString("ParserDialogTitleText");

        _settings = settings;
        _dialogFactory = dialogFactory;
        _logGenerator = new(settings, dialogFactory);
        _parsedFileGenerator = new(settings, dialogFactory);

        ResetTimeCommand = new DelegateCommand(ResetTime);
        GetConversationCommand = new DelegateCommand(ParseConversation, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(ConversationPlayer) && !string.IsNullOrWhiteSpace(_settings.OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => ConversationPlayer);
        GetAllCommunicationCommand = new DelegateCommand(GetAllCommunication, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(_settings.OutputDirectory))
            .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText);
        GetSearchTermCommand = new DelegateCommand(GetSearchTerm, () => !_performingParse && !string.IsNullOrWhiteSpace(StartTimeText) && !string.IsNullOrWhiteSpace(EndTimeText) && !string.IsNullOrWhiteSpace(SearchTermText) && !string.IsNullOrWhiteSpace(_settings.OutputDirectory))
           .ObservesProperty(() => StartTimeText).ObservesProperty(() => EndTimeText).ObservesProperty(() => SearchTermText);
        OpenGeneralParserCommand = new DelegateCommand(OpenGeneralParser);
    }

    public string ConversationPlayer
    {
        get => _conversationPlayer;
        set => SetProperty(ref _conversationPlayer, value);
    }

    public string EndTimeText
    {
        get => _endTimeText;
        set
        {
            if (SetProperty(ref _endTimeText, value))
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

    public DelegateCommand GetSearchTermCommand { get; }

    public bool IsCaseSensitive
    {
        get => _isCaseSensitive;
        set => SetProperty(ref _isCaseSensitive, value);
    }

    public DelegateCommand OpenGeneralParserCommand { get; }

    public DelegateCommand ResetTimeCommand { get; }

    public string SearchTermText
    {
        get => _searchTermText;
        set => SetProperty(ref _searchTermText, value);
    }

    public string StartTimeText
    {
        get => _startTimeText;
        set => SetProperty(ref _startTimeText, value);
    }

    private async Task ExecuteParse(Func<DateTime, DateTime, Task> parseToExecute)
    {
        if (!_logGenerator.ValidateTimeSettings(StartTimeText, EndTimeText, out DateTime startTime, out DateTime endTime))
            return;

        try
        {
            _performingParse = true;
            RefreshCommands();

            await parseToExecute(startTime, endTime);
        }
        finally
        {
            _performingParse = false;
            RefreshCommands();
        }
    }

    private async void GetAllCommunication()
        => await ExecuteParse(GetAllCommunicationAsync);

    private async Task GetAllCommunicationAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.GetAllCommunicationAsync(startTime, endTime, GetOutputPath());

    private string GetOutputPath()
        => string.IsNullOrWhiteSpace(_settings.OutputDirectory) ? _logGenerator.GetUserProfilePath() : _settings.OutputDirectory;

    private async void GetSearchTerm()
        => await ExecuteParse(GetSearchTermAsync);

    private async Task GetSearchTermAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.GetSearchTermAsync(startTime, endTime, SearchTermText, IsCaseSensitive, GetOutputPath());

    private void OpenGeneralParser()
    {
        IGeneralEqLogParserDialogViewModel parser = _dialogFactory.CreateGeneralEqParserDialogViewModel(_dialogFactory, _settings);
        if (parser.ShowDialog() != true)
            return;
    }

    private async void ParseConversation()
        => await ExecuteParse(ParseConversationAsync);

    private async Task ParseConversationAsync(DateTime startTime, DateTime endTime)
        => await _parsedFileGenerator.ParseConversationAsync(startTime, endTime, ConversationPlayer, GetOutputPath());

    private void RefreshCommands()
    {
        GetConversationCommand.RaiseCanExecuteChanged();
        GetSearchTermCommand.RaiseCanExecuteChanged();
    }

    private void ResetTime()
    {
        DateTime currentTime = DateTime.Now;
        EndTimeText = currentTime.ToString(Constants.TimePickerDisplayDateTimeFormat);
        StartTimeText = currentTime.AddHours(-6).ToString(Constants.TimePickerDisplayDateTimeFormat);
    }
}

public interface IParserDialogViewModel : IDialogViewModel
{
    string ConversationPlayer { get; set; }

    string EndTimeText { get; set; }

    DelegateCommand GetAllCommunicationCommand { get; }

    DelegateCommand GetConversationCommand { get; }

    DelegateCommand GetSearchTermCommand { get; }

    bool IsCaseSensitive { get; set; }

    DelegateCommand OpenGeneralParserCommand { get; }

    DelegateCommand ResetTimeCommand { get; }

    string SearchTermText { get; set; }

    string StartTimeText { get; set; }
}
