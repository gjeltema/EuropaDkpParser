// -----------------------------------------------------------------------
// ParsedFileGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using System.IO;
using System.Windows;
using DkpParser;
using DkpParser.Parsers;
using EuropaDkpParser.Resources;
using EuropaDkpParser.ViewModels;

internal sealed class ParsedFileGenerator
{
    private readonly IDialogFactory _dialogFactory;
    private readonly IDkpParserSettings _settings;

    public ParsedFileGenerator(IDkpParserSettings settings, IDialogFactory dialogFactory)
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

    public async Task GetAllCommunicationAsync(DateTime startTime, DateTime endTime, string outputDirectory)
    {
        IAllCommunicationParser communicationParser = new AllCommunicationParser(_settings);
        ICollection<EqLogFile> logFiles = await Task.Run(() => communicationParser.GetEqLogFiles(startTime, endTime));

        string communicationOutputFile = $"{Constants.CommunicationFileNamePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string communicationOutputFullPath = Path.Combine(outputDirectory, communicationOutputFile);
        bool anyCommunicationFound = false;
        foreach (EqLogFile logFile in logFiles)
        {
            if (logFile.LogEntries.Count > 0)
            {
                await CreateFile(communicationOutputFullPath, logFile.GetAllLogLines());
                anyCommunicationFound = true;
            }
        }

        if (!anyCommunicationFound)
        {
            MessageBox.Show(Strings.GetString("NoCommunicationFound"), Strings.GetString("NoCommunicationFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(communicationOutputFullPath);
        completedDialog.ShowDialog();
    }

    public async Task GetSearchTermAsync(DateTime startTime, DateTime endTime, string searchTermText, bool isCaseSensitive, string outputDirectory)
    {
        ITermParser termParser = new TermParser(_settings, searchTermText, isCaseSensitive);
        ICollection<EqLogFile> logFiles = await Task.Run(() => termParser.GetEqLogFiles(startTime, endTime));

        string searchTermOutputFile = $"{Constants.SearchTermFileNamePrefix}{searchTermText}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string searchTermOutputFullPath = Path.Combine(outputDirectory, searchTermOutputFile);
        bool anySearchTermFound = false;
        foreach (EqLogFile logFile in logFiles)
        {
            if (logFile.LogEntries.Count > 0)
            {
                await CreateFile(searchTermOutputFullPath, logFile.GetAllLogLines());
                anySearchTermFound = true;
            }
        }

        if (!anySearchTermFound)
        {
            MessageBox.Show(Strings.GetString("NoSearchTermFound"), Strings.GetString("NoSearchTermFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(searchTermOutputFullPath);
        completedDialog.ShowDialog();
    }

    public async Task ParseConversationAsync(DateTime startTime, DateTime endTime, string conversationPlayer, string outputDirectory)
    {
        IConversationParser conversationParser = new ConversationParser(_settings, conversationPlayer);
        ICollection<EqLogFile> logFiles = await Task.Run(() => conversationParser.GetEqLogFiles(startTime, endTime));

        string conversationPlayers = string.Join("-", conversationPlayer.Split(';'));
        string conversationOutputFile = $"{Constants.ConversationFileNamePrefix}{conversationPlayers}-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string conversationOutputFullPath = Path.Combine(outputDirectory, conversationOutputFile);
        bool anyConversationFound = false;
        foreach (EqLogFile logFile in logFiles)
        {
            if (logFile.LogEntries.Count > 0)
            {
                await CreateFile(conversationOutputFullPath, logFile.GetAllLogLines());
                anyConversationFound = true;
            }
        }

        if (!anyConversationFound)
        {
            MessageBox.Show(Strings.GetString("NoConversationFound"), Strings.GetString("NoConversationFoundTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ICompletedDialogViewModel completedDialog = _dialogFactory.CreateCompletedDialogViewModel(conversationOutputFullPath);
        completedDialog.ShowDialog();
    }
}
