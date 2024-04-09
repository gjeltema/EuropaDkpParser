// -----------------------------------------------------------------------
// RaidUploadDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class RaidUploadDialogViewModel : DialogViewModelBase, IRaidUploadDialogViewModel
{
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<string> _errorMessages;
    private bool _hasErrorMessages;
    private bool _showProgress;
    private string _statusMessage;
    private bool _uploadButtonEnabled;

    public RaidUploadDialogViewModel(IDialogViewFactory viewFactory, RaidEntries raidEntries, IDkpParserSettings settings)
        : base(viewFactory)
    {
        Title = Strings.GetString("RaidUploadDialogTitleText");

        _raidEntries = raidEntries;
        _settings = settings;

        StatusMessage = Strings.GetString("BeginStatus");

        UploadButtonEnabled = true;

        BeginUploadCommand = new DelegateCommand(BeginUpload);
    }

    public DelegateCommand BeginUploadCommand { get; }

    public ICollection<string> ErrorMessages
    {
        get => _errorMessages;
        set => SetProperty(ref _errorMessages, value);
    }

    public bool HasErrorMessages
    {
        get => _hasErrorMessages;
        set => SetProperty(ref _hasErrorMessages, value);
    }

    public bool ShowProgress
    {
        get => _showProgress;
        set => SetProperty(ref _showProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool UploadButtonEnabled
    {
        get => _uploadButtonEnabled;
        set => SetProperty(ref _uploadButtonEnabled, value);
    }

    private async void BeginUpload()
        => await BeginUploadAsync();

    private async Task BeginUploadAsync()
    {
        try
        {
            UploadButtonEnabled = false;
            StatusMessage = Strings.GetString("UploadingStatus");
            ShowProgress = true;

            RaidUploader server = new(_settings);
            RaidUploadResults uploadResults = await server.UploadRaid(_raidEntries);

            ErrorMessages = uploadResults.GetErrorMessages().ToList();
            HasErrorMessages = ErrorMessages.Count > 0;
        }
        catch (Exception e)
        {
            ErrorMessages = [$"Unexpected rrror encountered when uploading: {e}"];
            HasErrorMessages = true;
            StatusMessage = Strings.GetString("FailureStatus");
        }
        finally
        {
            ShowProgress = false;
            UploadButtonEnabled = !HasErrorMessages;
            StatusMessage = HasErrorMessages ? Strings.GetString("FailureStatus") : Strings.GetString("SuccessStatus");
        }
    }
}

public interface IRaidUploadDialogViewModel : IDialogViewModel
{
    DelegateCommand BeginUploadCommand { get; }

    ICollection<string> ErrorMessages { get; }

    bool HasErrorMessages { get; }

    bool ShowProgress { get; set; }

    string StatusMessage { get; }

    bool UploadButtonEnabled { get; }
}
