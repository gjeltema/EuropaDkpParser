// -----------------------------------------------------------------------
// ItemLinkValues.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;
using Gjeltema.Logging;

public sealed class ItemLinkValues
{
    private const char Delimiter = '|';
    private const string LogPrefix = $"[{nameof(ItemLinkValues)}]";
    private readonly Dictionary<string, string> _itemLinkValues;
    private readonly string _itemLinkValuesFileName;

    public ItemLinkValues(string itemLinkValuesFileName)
    {
        _itemLinkValuesFileName = itemLinkValuesFileName;
        _itemLinkValues = [];
    }

    public void AddAndSaveItemId(string itemName, string itemValue)
    {
        AddItemId(itemName, itemValue);
        SaveValues();
    }

    public void AddItemId(string itemName, string itemIdValue)
    {
        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(itemIdValue))
            return;

        if (itemIdValue.Length > 6)
            return;

        foreach (char idChar in itemIdValue)
        {
            if (!char.IsDigit(idChar))
            {
                return;
            }
        }

        itemIdValue = PadItemId(itemIdValue);
        _itemLinkValues[itemName] = itemIdValue;
    }

    public string GetItemId(string itemName)
        => _itemLinkValues.TryGetValue(itemName, out string itemValue) ? itemValue : "";

    public string GetItemLink(string itemName)
    {
        string itemLinkId = GetItemId(itemName);
        if (string.IsNullOrEmpty(itemLinkId))
            return itemName;

        return $"\u0012{itemLinkId}: {itemName}\u0012";
    }

    public void LoadValues()
    {
        if (!File.Exists(_itemLinkValuesFileName))
        {
            Log.Error($"{LogPrefix} {_itemLinkValuesFileName} does not exist.");
            FileInfo fi = new(_itemLinkValuesFileName);
            throw new FileNotFoundException($"{_itemLinkValuesFileName} does not exist.", fi.FullName);
        }

        IEnumerable<string> fileContents = File.ReadAllLines(_itemLinkValuesFileName);
        foreach (string itemLine in fileContents)
        {
            string[] itemLineParts = itemLine.Split(Delimiter);
            _itemLinkValues[itemLineParts[0]] = PadItemId(itemLineParts[1]);
        }
    }

    public void SaveValues()
    {
        try
        {
            File.WriteAllLines(_itemLinkValuesFileName, _itemLinkValues.Select(x => $"{x.Key}{Delimiter}{x.Value}"));
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Unable to write item link info to file: {_itemLinkValuesFileName}: {ex.ToLogMessage()}");
        }
    }

    private string PadItemId(string itemId)
    {
        while (itemId.Length < 6)
        {
            itemId = "0" + itemId;
        }
        return itemId;
    }
}
