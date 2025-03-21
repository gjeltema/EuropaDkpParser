// -----------------------------------------------------------------------
// DkpLogGenerator.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using DkpParser;
using DkpParser.Parsers;
using EuropaDkpParser.Resources;
using EuropaDkpParser.ViewModels;
using Gjeltema.Logging;

internal sealed class DkpLogGenerator
{
    private const string LogPrefix = $"[{nameof(DkpLogGenerator)}]";
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;

    public DkpLogGenerator(IDkpParserSettings settings, IDialogFactory dialogFactory)
    {
        _settings = settings;
        _dialogFactory = dialogFactory;
    }

    public async Task GetRawLogFilesParseAsync(DateTime startTime, DateTime endTime, string outputPath)
    {
        Log.Debug($"{LogPrefix} Starting {nameof(GetRawLogFilesParseAsync)}");

        IFullRaidLogsParser fullLogParser = new FullRaidLogsParser(_settings);
        ICollection<EqLogFile> logFiles = await Task.Run(() => fullLogParser.GetEqLogFiles(startTime, endTime));

        if (logFiles.Count == 0)
        {
            Log.Info($"{LogPrefix} No log entries were found between {startTime} and {endTime}.  Ending RawLog parse.");
            string errorMessage = $"No log entries were found between {startTime} and {endTime}.  Ending parse.";
            MessageBox.Show(errorMessage, "No Log Entries Found", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string directoryName = $"RawLog-{DateTime.Now:yyyyMMdd-HHmmss}";
        string directoryForFiles = Path.Combine(outputPath, directoryName);
        string fullLogOutputFile = $"{Constants.FullGeneratedLogFileNamePrefix}{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string fullLogOutputFullPath = Path.Combine(directoryForFiles, fullLogOutputFile);

        Log.Trace($"{LogPrefix} {nameof(directoryName)}:{directoryName}; {nameof(directoryForFiles)}:{directoryForFiles}; {nameof(fullLogOutputFile)}:{fullLogOutputFile}; {nameof(fullLogOutputFullPath)}{fullLogOutputFullPath}");

        if (!await TryCreateDirectory(directoryForFiles))
            return;

        foreach (EqLogFile logFile in logFiles.OrderBy(x => x.LogEntries[0].Timestamp))
        {
            await CreateFile(fullLogOutputFullPath, logFile.GetAllLogLines());
        }

        RaidParticipationFilesParser listFilesParser = new(_settings);

        IEnumerable<RaidListFile> raidListFiles = listFilesParser.GetRelevantRaidListFiles(startTime, endTime);
        foreach (RaidListFile raidListFile in raidListFiles)
        {
            if (!await TryCopyFile(raidListFile.FullFilePath, Path.Combine(directoryForFiles, raidListFile.FileName)))
            {
                await DeleteDirectory(directoryForFiles);
                return;
            }
        }

        IEnumerable<ZealRaidAttendanceFile> zealRaidFiles = listFilesParser.GetRelevantZealRaidAttendanceFiles(startTime, endTime);
        foreach (ZealRaidAttendanceFile zealRaidFile in zealRaidFiles)
        {
            if (!await TryCopyFile(zealRaidFile.FullFilePath, Path.Combine(directoryForFiles, zealRaidFile.FileName)))
            {
                await DeleteDirectory(directoryForFiles);
                return;
            }
        }

        IEnumerable<RaidDumpFile> raidDumpFiles = listFilesParser.GetRelevantRaidDumpFiles(startTime, endTime);
        foreach (RaidDumpFile raidDumpFile in raidDumpFiles)
        {
            await TryCopyFile(raidDumpFile.FullFilePath, Path.Combine(directoryForFiles, raidDumpFile.FileName));
        }

        await TryCopyApplicationLogFiles(directoryForFiles);

        string zipFullFilePath = Path.Combine(outputPath, directoryName + ".zip");

        if (!await TryCreateZip(directoryForFiles, zipFullFilePath))
        {
            await DeleteDirectory(directoryForFiles);
            return;
        }

        Log.Debug($"Zip file {zipFullFilePath} created.");

        await DeleteDirectory(directoryForFiles);

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(zipFullFilePath);
        completedDialog.ShowDialog();
    }

    public string GetUserProfilePath()
         => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "EuropaDKP");

    public async Task StartLogParseAsync(DkpLogGenerationSessionSettings sessionSettings)
    {
        Log.Debug($"{LogPrefix} Starting {nameof(StartLogParseAsync)}");

        RaidEntries raidEntries = await ParseAndAnalyzeLogFiles(sessionSettings);

        if (raidEntries == null)
        {
            Log.Info($"{LogPrefix} RaidEntries is null. Ending parse.");
            return;
        }

        if (raidEntries.AttendanceEntries.Count == 0)
        {
            MessageBox.Show(
                $"No attendance entries were found between {sessionSettings.StartTime} and {sessionSettings.EndTime}.  Ending parse.",
                "No Attendances Found",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            Log.Info($"{LogPrefix} No attendance entries were found between {sessionSettings.StartTime} and {sessionSettings.EndTime}.  Ending parse.");
            return;
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

        //IBonusDkpAnalyzer bonusDkp = new BonusDkpAnalyzer(_settings);
        //bonusDkp.AddBonusAttendance(raidEntries);

        Log.Trace($"{LogPrefix} RaidEntries:{Environment.NewLine}{raidEntries.GetAllEntries()}");

        IFinalSummaryDialogViewModel finalSummaryDialog = _dialogFactory.CreateFinalSummaryDialogViewModel(_dialogFactory, _settings, raidEntries, _settings.IsApiConfigured);
        if (finalSummaryDialog.ShowDialog() == false)
        {
            Log.Debug($"{LogPrefix} User cancelled out of FinalSummaryDialog.  Ending parse.");
            return;
        }

        IOutputGenerator generator = new FileOutputGenerator();
        IEnumerable<string> fileContents = generator.GenerateOutput(raidEntries, _settings.RaidValue.GetZoneRaidAlias);
        bool success = await CreateFile(sessionSettings.GeneratedFile, fileContents);
        if (!success)
            return;

        Log.Debug($"{LogPrefix} {nameof(StartLogParseAsync)}, {nameof(finalSummaryDialog.UploadToServer)}: {finalSummaryDialog.UploadToServer}, {nameof(_settings.IsApiConfigured)}: {_settings.IsApiConfigured}.");
        if (finalSummaryDialog.UploadToServer && _settings.IsApiConfigured)
        {
            Log.Debug($"{LogPrefix} {nameof(StartLogParseAsync)}, beginning upload.");
            IRaidUploadDialogViewModel raidUpload = _dialogFactory.CreateRaidUploadDialogViewModel(_dialogFactory, raidEntries, _settings);
            raidUpload.ShowDialog();
        }

        Log.Debug($"{LogPrefix} {nameof(StartLogParseAsync)}, finished upload.");

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(sessionSettings.GeneratedFile);
        completedDialog.SummaryDisplay = GetSummaryDisplay(raidEntries);
        completedDialog.ShowDialog();
    }

    public async Task UploadGeneratedLogFile(string generatedLogFile)
    {
        Log.Debug($"{LogPrefix} Starting {nameof(UploadGeneratedLogFile)}");

        RaidEntries raidEntries = await ParseGeneratedLogFile(generatedLogFile);

        if (raidEntries == null || (raidEntries.DkpEntries.Count == 0 && raidEntries.AttendanceEntries.Count == 0))
        {
            MessageBox.Show(
                $"No attendance entries and no DKP entries were found.  Ending Upload Generated File.",
                "No Entries Found",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            Log.Info($"{LogPrefix} RaidEntries is null, or has no attendance entries and no DKP entries.  Ending Upload Generated File.");
            return;
        }

        IFinalSummaryDialogViewModel finalSummaryDialog = _dialogFactory.CreateFinalSummaryDialogViewModel(_dialogFactory, _settings, raidEntries, _settings.IsApiConfigured);
        if (finalSummaryDialog.ShowDialog() == false)
        {
            Log.Debug($"{LogPrefix} User cancelled out of FinalSummaryDialog.  Ending parse.");
            return;
        }

        Log.Debug($"{LogPrefix} {nameof(UploadGeneratedLogFile)}, {nameof(finalSummaryDialog.UploadToServer)}: {finalSummaryDialog.UploadToServer}, {nameof(_settings.IsApiConfigured)}: {_settings.IsApiConfigured}.");
        if (finalSummaryDialog.UploadToServer && _settings.IsApiConfigured)
        {
            Log.Debug($"{LogPrefix} {nameof(UploadGeneratedLogFile)}, beginning upload.");
            IRaidUploadDialogViewModel raidUpload = _dialogFactory.CreateRaidUploadDialogViewModel(_dialogFactory, raidEntries, _settings);
            raidUpload.ShowDialog();
        }

        Log.Debug($"{LogPrefix} {nameof(UploadGeneratedLogFile)}, finished upload.");

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel("No file generated, uploaded existing generated file");
        completedDialog.SummaryDisplay = GetSummaryDisplay(raidEntries);
        completedDialog.ShowDialog();
    }

    public bool ValidateTimeSettings(string startTimeText, string endTimeText, out DateTime startTime, out DateTime endTime)
    {
        endTime = DateTime.MinValue;
        if (!DateTime.TryParse(startTimeText, out startTime))
        {
            MessageBox.Show(Strings.GetString("StartTimeErrorMessage"), Strings.GetString("StartTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            Log.Warning($"{LogPrefix} Unable to parse StartTime value of: {startTimeText}");
            return false;
        }

        if (!DateTime.TryParse(endTimeText, out endTime))
        {
            MessageBox.Show(Strings.GetString("EndTimeErrorMessage"), Strings.GetString("EndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            Log.Warning($"{LogPrefix} Unable to parse EndTime value of: {endTimeText}");
            return false;
        }

        if (startTime > endTime)
        {
            MessageBox.Show(Strings.GetString("StartEndTimeErrorMessage"), Strings.GetString("StartEndTimeError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }

    private async Task<bool> CreateFile(string fileToWriteTo, IEnumerable<string> fileContents)
    {
        try
        {
            await Task.Run(() => File.AppendAllLines(fileToWriteTo, fileContents));
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(CreateFile)} failed to create {fileToWriteTo}: {ex.ToLogMessage()}");
            MessageBox.Show(Strings.GetString("LogGenerationErrorMessage") + ex.Message, Strings.GetString("LogGenerationError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private async Task DeleteDirectory(string directoryName)
    {
        try
        {
            await Task.Run(() => Directory.Delete(directoryName, true));
        }
        catch (Exception ex)
        {
            Log.Warning($"{LogPrefix} Error deleting directory {directoryName} : {ex.ToLogMessage()}");
        }
    }

    private string GetSummaryDisplay(RaidEntries raidEntries)
    {
        StringBuilder summaryDisplay = new();
        summaryDisplay.Append(string.Join(Environment.NewLine, raidEntries.GetAllDkpspentEntries(_settings.RaidValue.GetZoneRaidAlias)));

        if (raidEntries.RemovedPlayerCharacters.Count > 0)
        {
            summaryDisplay.AppendLine();
            summaryDisplay.AppendLine("-------------- Characters Removed Due To No Player On DKP Server --------------");
            summaryDisplay.AppendLine(string.Join(Environment.NewLine, raidEntries.RemovedPlayerCharacters));
        }

        if (raidEntries.RemovedDkpEntries.Count > 0)
        {
            summaryDisplay.AppendLine();
            summaryDisplay.AppendLine("-------------- DKPSPENT Entries Removed Due To No Player On DKP Server --------------");
            summaryDisplay.AppendLine(string.Join(Environment.NewLine, raidEntries.RemovedDkpEntries));
        }

        if (raidEntries.DkpUploadErrors.Count > 0)
        {
            summaryDisplay.AppendLine();
            summaryDisplay.AppendLine("-------------- DKPSPENT Entries With Upload Errors --------------");
            summaryDisplay.AppendLine(string.Join(Environment.NewLine, raidEntries.DkpUploadErrors.Select(x => x.RawLogLine)));
        }

        string summaryDisplayText = summaryDisplay.ToString();
        Log.Trace($"{LogPrefix} Summary Display:{Environment.NewLine}{summaryDisplayText}");
        return summaryDisplayText;
    }

    private async Task<RaidEntries> ParseAndAnalyzeLogFiles(DkpLogGenerationSessionSettings sessionSettings)
    {
        try
        {
            IDkpLogParseProcessor parseProcessor = new DkpLogParseProcessor(_settings);

            Stopwatch timer = new Stopwatch();
            timer.Start();
            LogParseResults results = await Task.Run(() => parseProcessor.ParseLogs(sessionSettings.StartTime, sessionSettings.EndTime));
            timer.Stop();
            Log.Info($"{LogPrefix} {nameof(parseProcessor.ParseLogs)} elapsed time: {(int)timer.Elapsed.TotalMilliseconds}ms");
            Log.Debug($"{LogPrefix} Parse results:{Environment.NewLine}{string.Join(Environment.NewLine, results.GetAllLines())}");

            ILogEntryAnalyzer logEntryAnalyzer = new LogEntryAnalyzer(_settings);

            timer.Reset();
            timer.Start();
            RaidEntries raidEntries = await Task.Run(() => logEntryAnalyzer.AnalyzeRaidLogEntries(results));
            timer.Stop();
            Log.Info($"{LogPrefix} {nameof(logEntryAnalyzer.AnalyzeRaidLogEntries)} elapsed time: {(int)timer.Elapsed.TotalMilliseconds}ms");
            Log.Debug($"{LogPrefix} Analysis results:{Environment.NewLine}{string.Join(Environment.NewLine, raidEntries.GetAllEntries())}");

            return raidEntries;
        }
        catch (EuropaDkpParserException e)
        {
            Log.Error($"{LogPrefix} Parse/analysis error of log line:{e.LogLine}{Environment.NewLine}{e.ToLogMessage()}");
            string errorMessage = $"{e.Message}: {e.InnerException?.Message}{Environment.NewLine}{e.LogLine}";
            MessageBox.Show(errorMessage, Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Unexpected parse/analysis error: {e.ToLogMessage()}");
            MessageBox.Show($"Unexpected failure parsing: {e.Message}", Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
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
            Log.Error($"{LogPrefix} Parse error of log line:{e.LogLine}{Environment.NewLine}{e.ToLogMessage()}");
            string errorMessage = $"{e.Message}: {e.InnerException?.Message}{Environment.NewLine}{e.LogLine}";
            MessageBox.Show(errorMessage, Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Unexpected parse error: {e.ToLogMessage()}");
            MessageBox.Show($"Unexpected failure parsing: {e.Message}", Strings.GetString("UnexpectedError"), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private async Task TryCopyApplicationLogFiles(string destinationDirectory)
    {
        try
        {
            string currentDirectory = AppContext.BaseDirectory;
            string applicationLogDirectory = Path.Combine(currentDirectory, "Logs");
            for (int i = 0; i < 3; i++)
            {
                DateTime currentTime = DateTime.Now;
                string logFileName = $"{currentTime.AddDays(-i):MMdd}_ParserLog.txt";
                string logFileSourcePath = Path.Combine(applicationLogDirectory, logFileName);
                if (File.Exists(logFileSourcePath))
                {
                    string logFileDestinationPath = Path.Combine(destinationDirectory, logFileName);
                    await TryCopyFile(logFileSourcePath, logFileDestinationPath);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Error copying application log files to {destinationDirectory}: {ex.ToLogMessage()}");
        }
    }

    private async Task<bool> TryCopyFile(string sourceFilePath, string destinationFilePath)
    {
        try
        {
            Log.Debug($"{LogPrefix} Copying {sourceFilePath} to {destinationFilePath}");
            await Task.Run(() => File.Copy(sourceFilePath, destinationFilePath));
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Failed to copy file from {sourceFilePath} to {destinationFilePath}, first attempt: {e.ToLogMessage()}");

            try
            {
                Log.Debug($"{LogPrefix} Copying {sourceFilePath} to {destinationFilePath}, second attempt.");
                await Task.Run(() => File.Copy(sourceFilePath, destinationFilePath));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{LogPrefix} Failed to copy file from {sourceFilePath} to {destinationFilePath}, second attempt: {ex.ToLogMessage()}");
                string errorMessage = $"Failed to copy file from {sourceFilePath} to {destinationFilePath}: {ex.Message}";
                MessageBox.Show(errorMessage, "Failed to Copy File", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

    private async Task<bool> TryCreateDirectory(string directoryName)
    {
        try
        {
            Log.Debug($"{LogPrefix} Creating directory {directoryName}, first attempt.");
            await Task.Run(() => Directory.CreateDirectory(directoryName));
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Unable to create directory at {directoryName}, first attempt: {e.ToLogMessage()}");

            try
            {
                Log.Debug($"{LogPrefix} Creating directory {directoryName}, second attempt.");
                await Task.Run(() => Directory.CreateDirectory(directoryName));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{LogPrefix} Unable to create directory at {directoryName}, second attempt: {ex.ToLogMessage()}");
                MessageBox.Show($"Unable to create directory at {directoryName}: " + ex.Message, "Unable to create directory", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

    private async Task<bool> TryCreateZip(string sourceDirectory, string zipFullFilePath)
    {
        try
        {
            Log.Debug($"{LogPrefix} Creating zip {zipFullFilePath} using {sourceDirectory}, first attempt.");
            await Task.Run(() => ZipFile.CreateFromDirectory(sourceDirectory, zipFullFilePath));
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"{LogPrefix} Failed to create zip {zipFullFilePath} using {sourceDirectory}, first attempt: {e.ToLogMessage()}");

            try
            {
                Log.Debug($"{LogPrefix} Creating zip {zipFullFilePath} using {sourceDirectory}, second attempt.");
                await Task.Run(() => ZipFile.CreateFromDirectory(sourceDirectory, zipFullFilePath));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"{LogPrefix} Failed to create zip {zipFullFilePath} using {sourceDirectory}, second attempt: {ex.ToLogMessage()}");
                string errorMessage = $"Failed to create zip {zipFullFilePath} using {sourceDirectory}: {ex.Message}";
                MessageBox.Show(errorMessage, "Failed to Create Zip", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}

internal sealed class DkpLogGenerationSessionSettings
{
    public DateTime EndTime { get; init; }

    public string GeneratedFile { get; init; }

    public string OutputDirectory { get; init; }

    public string OutputPath { get; init; }

    public DateTime StartTime { get; init; }
}
