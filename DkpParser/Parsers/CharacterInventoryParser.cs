// -----------------------------------------------------------------------
// CharacterInventoryParser.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Parsers;

using System.IO;
using Gjeltema.Logging;

public sealed class CharacterInventoryParser
{
    private const string LogPrefix = $"[{nameof(CharacterInventoryParser)}]";

    public async Task AggregateInventoryFromDirectoriesAsync(IEnumerable<string> inventoryDirectories, string outputFilePath)
    {
        IEnumerable<string> fileContents = [];
        foreach (string inventoryDirectory in inventoryDirectories)
        {
            if (string.IsNullOrWhiteSpace(inventoryDirectory))
                continue;
            if (!Directory.Exists(inventoryDirectory))
                continue;

            IEnumerable<string> inventoryFiles = Directory.EnumerateFiles(inventoryDirectory, "*-Inventory.txt");
            ICollection<string> inventoryFileContentsFromDir = await GetAggregateInventoryFilesContentsAsync(inventoryFiles);
            fileContents = fileContents.Concat(inventoryFileContentsFromDir);
        }

        await CreateFileAsync(outputFilePath, fileContents);
    }

    public async Task<ICollection<string>> GetAggregateInventoryFilesContentsAsync(IEnumerable<string> inventoryFilePaths)
    {
        List<string> fileContents = [];
        fileContents.Add($"{"Name",-37} {"Count",-3} {"Loc",-17} {"Slots",-3} {"ID",-2}");
        fileContents.Add(string.Empty);

        foreach (string inventoryFilePath in inventoryFilePaths)
        {
            fileContents.Add(inventoryFilePath);
            ICollection<InventoryItem> inventoryItems = await GetInventoryItemsFromFileAsync(inventoryFilePath);
            fileContents.AddRange(inventoryItems.Select(x => x.ToAggregateFileString()));
            fileContents.Add(string.Empty);
        }

        return fileContents;
    }

    public ICollection<InventoryItem> GetInventoryItems(IEnumerable<string> inventoryItemsListing)
    {
        List<InventoryItem> inventoryItems = [];

        foreach (string inventoryItem in inventoryItemsListing)
        {
            string[] inventoryLineSplit = inventoryItem.Split('\t');
            if (inventoryLineSplit.Length < 5)
                continue;

            string name = inventoryLineSplit[1];
            if (string.IsNullOrWhiteSpace(name) || name == "Empty")
                continue;

            int id = GetIntValue(inventoryLineSplit[2]);
            int numberInStack = GetIntValue(inventoryLineSplit[3]);
            int slotsOfContainer = GetIntValue(inventoryLineSplit[4]);

            inventoryItems.Add(new InventoryItem
            {
                Location = inventoryLineSplit[0],
                Name = name,
                ItemId = id,
                NumberInStack = numberInStack,
                NumberOfSlotsIfContainer = slotsOfContainer
            });
        }

        return inventoryItems;
    }

    public async Task<ICollection<InventoryItem>> GetInventoryItemsFromFileAsync(string inventoryFilePath)
    {
        if (File.Exists(inventoryFilePath))
        {
            Log.Debug($"{LogPrefix} Parsing {inventoryFilePath}");
            string[] fileContents = await Task.Run(() => File.ReadAllLines(inventoryFilePath));
            return GetInventoryItemsFromFileContents(fileContents);
        }

        return [];
    }

    public ICollection<InventoryItem> GetInventoryItemsFromFileContents(IEnumerable<string> inventoryFileContents)
    {
        // First row is header: Location	Name	ID	Count	Slots
        return GetInventoryItems(inventoryFileContents.Skip(1));
    }

    private async Task<bool> CreateFileAsync(string fileToWriteTo, IEnumerable<string> fileContents)
    {
        try
        {
            Log.Debug($"{LogPrefix} Creating file: {fileToWriteTo}");
            await Task.Run(() => File.AppendAllLines(fileToWriteTo, fileContents));
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} {nameof(CreateFileAsync)} failed to create {fileToWriteTo}: {ex.ToLogMessage()}");
            return false;
        }
    }

    private int GetIntValue(string inputValue, int defaultValue = 0)
    {
        if (int.TryParse(inputValue, out int parsedValue))
        {
            return parsedValue;
        }
        return defaultValue;
    }
}

public sealed class InventoryItem
{
    public int ItemId { get; init; }

    public string Location { get; init; }

    public string Name { get; init; }

    public int NumberInStack { get; init; }

    public int NumberOfSlotsIfContainer { get; init; }

    public string ToAggregateFileString()
    {
        if (Name == "Currency")
            return $"{Name,-20} {NumberInStack,21}c {Location,-20} {NumberOfSlotsIfContainer,-2} {ItemId}";
        else
            return $"{Name,-40} {NumberInStack,-2} {Location,-20} {NumberOfSlotsIfContainer,-2} {ItemId}";
    }
}
