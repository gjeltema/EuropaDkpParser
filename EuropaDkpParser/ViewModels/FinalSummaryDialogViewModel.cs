// -----------------------------------------------------------------------
// FinalSummaryDialogViewModel.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.ViewModels;

using DkpParser;
using EuropaDkpParser.Resources;
using Prism.Commands;

internal sealed class FinalSummaryDialogViewModel : DialogViewModelBase, IFinalSummaryDialogViewModel
{
    private readonly IDialogFactory _dialogFactory;
    private readonly RaidEntries _raidEntries;
    private readonly IDkpParserSettings _settings;
    private ICollection<AttendanceEntry> _attendanceCalls;
    private bool _uploadToServer;

    internal FinalSummaryDialogViewModel(IDialogViewFactory viewFactory, IDialogFactory dialogFactory, IDkpParserSettings settings, RaidEntries raidEntries, bool canUploadToServer)
        : base(viewFactory)
    {
        Title = Strings.GetString("LogParseSummaryDialogTitleText");
        _dialogFactory = dialogFactory;
        _settings = settings;
        _raidEntries = raidEntries;
        ShowUploadToServer = canUploadToServer;
        UploadToServer = canUploadToServer;

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
        DkpSpentCalls = new List<DkpEntry>(_raidEntries.DkpEntries.OrderBy(x => x.Timestamp));

        AddOrModifyAttendanceEntryCommand = new DelegateCommand(AddOrModifyAttendanceEntry);
    }

    public DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    public ICollection<AttendanceEntry> AttendanceCalls
    {
        get => _attendanceCalls;
        set => SetProperty(ref _attendanceCalls, value);
    }

    public ICollection<DkpEntry> DkpSpentCalls { get; }

    public bool ShowUploadToServer { get; }

    public bool UploadToServer
    {
        get => _uploadToServer;
        set => SetProperty(ref _uploadToServer, value);
    }

    private void AddOrModifyAttendanceEntry()
    {
        IAttendanceEntryModiferDialogViewModel modifier = _dialogFactory.CreateAttendanceModifierDialogViewModel(_settings, _raidEntries);
        modifier.ShowDialog();

        AttendanceCalls = new List<AttendanceEntry>(_raidEntries.AttendanceEntries.OrderBy(x => x.Timestamp));
    }
}

public interface IFinalSummaryDialogViewModel : IDialogViewModel
{
    DelegateCommand AddOrModifyAttendanceEntryCommand { get; }

    ICollection<AttendanceEntry> AttendanceCalls { get; }

    ICollection<DkpEntry> DkpSpentCalls { get; }

    bool ShowUploadToServer { get; }

    bool UploadToServer { get; set; }
}
