// -----------------------------------------------------------------------
// ItemLinkValues.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class ItemLinkValues
{
    private const char Delimiter = '\t';
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

        while (itemIdValue.Length < 6)
        {
            itemIdValue = "0" + itemIdValue;
        }
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
            return;

        IEnumerable<string> fileContents = File.ReadAllLines(_itemLinkValuesFileName);
        foreach (string itemLine in fileContents)
        {
            string[] itemLineParts = itemLine.Split(Delimiter);
            _itemLinkValues[itemLineParts[0]] = itemLineParts[1];
        }
    }

    public void SaveValues()
    {
        try
        {
            File.WriteAllLines(_itemLinkValuesFileName, _itemLinkValues.Select(x => $"{x.Key}{Delimiter}{x.Value}"));
        }
        catch { }
    }
}
