// -----------------------------------------------------------------------
// DkpLogGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using System.IO;
using System.Text;
using System.Windows;
using DkpParser;
using DkpParser.Parsers;
using EuropaDkpParser.Resources;
using EuropaDkpParser.ViewModels;

internal sealed class DkpLogGenerator
{
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;

    public DkpLogGenerator(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
    }

    public async Task<bool> CreateFile(string fileToWriteTo, IEnumerable<string> fileContents)
    {
        try
        {
            await Task.Run(() => File.AppendAllLines(fileToWriteTo, fileContents));
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Strings.GetString("LogGenerationErrorMessage") + ex.ToString(), Strings.GetString("LogGenerationError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    public async Task GetRawLogFilesParseAsync(DateTime startTime, DateTime endTime, string outputPath)
    {
        IFullEqLogParser fullLogParser = new FullEqLogParser(_settings);
        ICollection<EqLogFile> logFiles = await Task.Run(() => fullLogParser.GetEqLogFiles(startTime, endTime));

        string fullLogOutputFile = $"{Constants.FullGeneratedLogFileNamePrefix}{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string fullLogOutputFullPath = Path.Combine(outputPath, fullLogOutputFile);

        foreach (EqLogFile logFile in logFiles)
        {
            await CreateFile(fullLogOutputFullPath, logFile.GetAllLogLines());
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(fullLogOutputFullPath);
        completedDialog.ShowDialog();
    }

    public string GetUserProfilePath()
         => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "EuropaDKP");

    public async Task StartLogParseAsync(DkpLogGenerationSessionSettings sessionSettings)
    {
        RaidEntries raidEntries = await ParseAndAnalyzeLogFiles(sessionSettings);

        if (raidEntries == null)
            return;

        if (sessionSettings.IsRawAnalyzerResultsChecked)
        {
            await OutputRawAnalyzerResults(raidEntries, sessionSettings);
        }

        if (sessionSettings.OutputAnalyzerErrors && raidEntries.AnalysisErrors.Count > 0)
        {
            await OutputAnalyzerErrorsToFile(raidEntries, sessionSettings);
        }

        if (raidEntries.AttendanceEntries.Any(x => x.PossibleError != PossibleError.None))
        {
            IAttendanceErrorDisplayDialogViewModel attendanceErrorDialog = _dialogFactory.CreateAttendanceErrorDisplayDialogViewModel(_settings, raidEntries);
            if (attendanceErrorDialog.ShowDialog() == false)
                return;
        }

        if (raidEntries.DkpEntries.Any(x => x.PossibleError != PossibleError.None))
        {
            IDkpErrorDisplayDialogViewModel dkpErrorDialog = _dialogFactory.CreateDkpErrorDisplayDialogViewModel(_settings, raidEntries);
            if (dkpErrorDialog.ShowDialog() == false)
                return;
        }

        if (raidEntries.PossibleLinkdeads.Count > 0)
        {
            IPossibleLinkdeadErrorDialogViewModel possibleLDDialog = _dialogFactory.CreatePossibleLinkdeadErrorDialogViewModel(raidEntries);
            possibleLDDialog.ShowDialog();
        }

        if (_settings.ShowAfkReview)
        {
            IAfkCheckerDialogViewModel afkDialog = _dialogFactory.CreateAfkCheckerDialogViewModel(raidEntries);
            if (afkDialog.SetNextAfkPlayer())
                afkDialog.ShowDialog();
        }

        IBonusDkpAnalyzer bonusDkp = new BonusDkpAnalyzer(_settings);
        bonusDkp.AddBonusAttendance(raidEntries);

        IFinalSummaryDialogViewModel finalSummaryDialog = _dialogFactory.CreateFinalSummaryDialogViewModel(_dialogFactory, _settings, raidEntries, _settings.IsApiConfigured);
        if (finalSummaryDialog.ShowDialog() == false)
            return;

        IOutputGenerator generator = new FileOutputGenerator();
        ICollection<string> fileContents = generator.GenerateOutput(raidEntries);
        bool success = await CreateFile(sessionSettings.GeneratedFile, fileContents);
        if (!success)
            return;

        if (finalSummaryDialog.UploadToServer && _settings.IsApiConfigured)
        {
            IRaidUploadDialogViewModel raidUpload = _dialogFactory.CreateRaidUploadDialogViewModel(raidEntries, _settings);
            raidUpload.ShowDialog();
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(sessionSettings.GeneratedFile);
        completedDialog.DkpSpentEntries = GetDkpSpentEntries(raidEntries);
        completedDialog.ShowDialog();
    }

    public async Task UploadGeneratedLogFile(string generatedLogFile)
    {
        RaidEntries raidEntries = await ParseGeneratedLogFile(generatedLogFile);

        if (raidEntries == null || (raidEntries.DkpEntries.Count == 0 && raidEntries.AttendanceEntries.Count == 0))
            return;

        IFinalSummaryDialogViewModel finalSummaryDialog = _dialogFactory.CreateFinalSummaryDialogViewModel(_dialogFactory, _settings, raidEntries, _settings.IsApiConfigured);
        if (finalSummaryDialog.ShowDialog() == false)
            return;

        if (finalSummaryDialog.UploadToServer && _settings.IsApiConfigured)
        {
            IRaidUploadDialogViewModel raidUpload = _dialogFactory.CreateRaidUploadDialogViewModel(raidEntries, _settings);
            raidUpload.ShowDialog();
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel("No file generated, uploaded existing generated file");
        completedDialog.DkpSpentEntries = GetDkpSpentEntries(raidEntries);
        completedDialog.ShowDialog();
    }

    private async Task<RaidEntries> ParseGeneratedLogFile(string generatedLogFile)
    {
        try
        {
            IDkpLogParseProcessor parseProcessor = new DkpLogParseProcessor(_settings);
            LogParseResults results = await Task.Run(() => parseProcessor.ParseGeneratedLog(generatedLogFile));

            ILogEntryAnalyzer logEntryAnalyzer = new LogEntryAnalyzer(_settings);
            return await Task.Run(() => logEntryAnalyzer.AnalyzeRaidLogEntries(results));
        }
        catch (EuropaDkpParserException e)
        {
            string errorMessage = $"{e.Message}: {e.InnerException?.Message}{Environment.NewLine}{e.LogLine}";
            MessageBox.Show(errorMessage, Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    public bool ValidateTimeSettings(string startTimeText, string endTimeText, out DateTime startTime, out DateTime endTime)
    {
        endTime = DateTime.MinValue;
        if (!DateTime.TryParse(startTimeText, out startTime))
        {
            MessageBox.Show(Strings.GetString("StartTimeErrorMessage"), Strings.GetString("StartTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (!DateTime.TryParse(endTimeText, out endTime))
        {
            MessageBox.Show(Strings.GetString("EndTimeErrorMessage"), Strings.GetString("EndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (startTime > endTime)
        {
            MessageBox.Show(Strings.GetString("StartEndTimeErrorMessage"), Strings.GetString("StartEndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }

    private string GetDkpSpentEntries(RaidEntries raidEntries)
    {
        string entries = string.Join(Environment.NewLine, raidEntries.GetAllDkpspentEntries());

        if (raidEntries.RemovedDkpEntries.Count > 0)
        {
            entries += Environment.NewLine + Environment.NewLine;
            entries += "-------------- DKPSPENT Entries Removed Due To No Player On DKP Server --------------";
            entries += string.Join(Environment.NewLine, raidEntries.RemovedDkpEntries);
        }

        return entries;
    }

    private async Task OutputAnalyzerErrorsToFile(RaidEntries raidEntries, DkpLogGenerationSessionSettings sessionSettings)
    {
        string directory = sessionSettings.OutputPath;

        string rawAnalyzerOutputFile = $"DEBUG_AnalyzerErrors-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawAnalyzerOutputFullPath = Path.Combine(directory, rawAnalyzerOutputFile);
        await CreateFile(rawAnalyzerOutputFullPath, raidEntries.AnalysisErrors);
    }

    private async Task OutputRawAnalyzerResults(RaidEntries raidEntries, DkpLogGenerationSessionSettings sessionSettings)
    {
        string directory = sessionSettings.OutputPath;

        string rawAnalyzerOutputFile = $"DEBUG_RawAnalyzerOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawAnalyzerOutputFullPath = Path.Combine(directory, rawAnalyzerOutputFile);
        await CreateFile(rawAnalyzerOutputFullPath, raidEntries.GetAllEntries());
    }

    private async Task OutputRawParseResults(LogParseResults results, DkpLogGenerationSessionSettings sessionSettings)
    {
        string directory = sessionSettings.OutputPath;

        string rawParseOutputFile = $"RawParseOutput-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string rawParseOutputFullPath = Path.Combine(directory, rawParseOutputFile);
        foreach (EqLogFile logFile in results.EqLogFiles)
        {
            await CreateFile(rawParseOutputFullPath, logFile.GetAllLogLines());
        }
    }

    private async Task<RaidEntries> ParseAndAnalyzeLogFiles(DkpLogGenerationSessionSettings sessionSettings)
    {
        try
        {
            IDkpLogParseProcessor parseProcessor = new DkpLogParseProcessor(_settings);
            LogParseResults results = await Task.Run(() => parseProcessor.ParseLogs(sessionSettings.StartTime, sessionSettings.EndTime));

            if (sessionSettings.IsRawParseResultsChecked)
            {
                await OutputRawParseResults(results, sessionSettings);
            }

            ILogEntryAnalyzer logEntryAnalyzer = new LogEntryAnalyzer(_settings);
            return await Task.Run(() => logEntryAnalyzer.AnalyzeRaidLogEntries(results));
        }
        catch (EuropaDkpParserException e)
        {
            await WriteEuropaExceptionToLogFile(e, sessionSettings);

            string errorMessage = $"{e.Message}: {e.InnerException?.Message}{Environment.NewLine}{e.LogLine}";
            MessageBox.Show(errorMessage, Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        catch (Exception e)
        {
            await WriteExceptionToLogFile(e, sessionSettings);

            MessageBox.Show(e.Message, Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private async Task WriteEuropaExceptionToLogFile(EuropaDkpParserException e, DkpLogGenerationSessionSettings sessionSettings)
    {
        StringBuilder message = new();
        message.AppendLine(e.Message);
        message.Append("Raw log line: ").AppendLine(e.LogLine);
        message.Append("Inner exception message: ").AppendLine(e.InnerException?.Message);
        message.Append("Stack trace:").AppendLine(e.InnerException?.StackTrace).AppendLine();
        message.AppendLine("Inner exception ToString: ").AppendLine(e.InnerException?.ToString());

        string directory = sessionSettings.OutputPath;
        string errorOutputFile = $"ERROR_EXCEPTION-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string errorOutputFullPath = Path.Combine(directory, errorOutputFile);
        await CreateFile(errorOutputFullPath, [message.ToString()]);
    }

    private async Task WriteExceptionToLogFile(Exception e, DkpLogGenerationSessionSettings sessionSettings)
    {
        StringBuilder message = new();
        message.AppendLine(e.Message);
        message.Append("Stack trace:").AppendLine(e.StackTrace).AppendLine();
        message.AppendLine("Exception ToString: ").AppendLine(e.ToString());

        string directory = sessionSettings.OutputPath;
        string errorOutputFile = $"ERROR_EXCEPTION-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string errorOutputFullPath = Path.Combine(directory, errorOutputFile);
        await CreateFile(errorOutputFullPath, [message.ToString()]);
    }
}

internal sealed class DkpLogGenerationSessionSettings
{
    public DateTime EndTime { get; init; }

    public string GeneratedFile { get; init; }

    public bool IsRawAnalyzerResultsChecked { get; init; }

    public bool IsRawParseResultsChecked { get; init; }

    public bool OutputAnalyzerErrors { get; init; }

    public string OutputDirectory { get; init; }

    public string OutputPath { get; init; }

    public DateTime StartTime { get; init; }
}
