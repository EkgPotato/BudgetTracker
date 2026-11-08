using ClosedXML.Excel;
using Finance.Api.Domain;

namespace Finance.Api.Features.Imports;

/// <summary>Extracts the reusable account/category metadata from the supplied budget workbooks.
/// A preview is deliberately returned before any database write: legacy templates contain formulas
/// and some #REF! cells, which must never be treated as financial postings.</summary>
public sealed class ExcelBudgetParser
{
    private static readonly string[] IgnoredWords = ["stan kont", "wzorzec kategorii", "cały rok", "plan", "wykonanie", "różnica", "suma", "średnia", "miesiąc", "data", "kategoria", "przychody", "wydatki"];

    public ExcelImportPreview Parse(Stream stream, string fileName)
    {
        using var workbook = new XLWorkbook(stream);
        var warnings = new List<string>();
        var accounts = ParseAccounts(FindSheet(workbook, "STAN KONT"), warnings);
        var categories = ParseCategories(FindSheet(workbook, "WZORZEC KATEGORII"), warnings);

        if (accounts.Count == 0)
            warnings.Add("Nie znaleziono kont automatycznie. Dodaj je ręcznie po imporcie.");
        if (categories.Count == 0)
            warnings.Add("Nie znaleziono kategorii automatycznie. Dodaj je ręcznie po imporcie.");
        warnings.Add("Import przenosi konta, kategorie i salda otwarcia. Formuły oraz komórki #REF! są pomijane.");

        return new ExcelImportPreview(
            Normalize(fileName).Contains("domowy") ? "Domowy" : "Prywatny",
            accounts, categories, warnings);
    }

    private static IXLWorksheet? FindSheet(XLWorkbook workbook, string expected) =>
        workbook.Worksheets.FirstOrDefault(x => Normalize(x.Name) == Normalize(expected));

    private static List<ImportedAccountPreview> ParseAccounts(IXLWorksheet? sheet, ICollection<string> warnings)
    {
        if (sheet is null) { warnings.Add("Arkusz „STAN KONT” nie został odnaleziony."); return []; }
        var results = new Dictionary<string, ImportedAccountPreview>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in sheet.RowsUsed())
        {
            var cells = row.CellsUsed().ToList();
            for (var i = 0; i < cells.Count - 1; i++)
            {
                var name = cells[i].GetString().Trim();
                if (!IsCandidate(name)) continue;
                var balanceCell = cells.Skip(i + 1).FirstOrDefault(c => TryGetDecimal(c, out _));
                if (balanceCell is null || !TryGetDecimal(balanceCell, out var balance)) continue;
                results.TryAdd(name, new ImportedAccountPreview(name, balance, GuessAccountType(name)));
            }
        }
        return results.Values.OrderBy(x => x.Name).ToList();
    }

    private static List<ImportedCategoryPreview> ParseCategories(IXLWorksheet? sheet, ICollection<string> warnings)
    {
        if (sheet is null) { warnings.Add("Arkusz „Wzorzec kategorii” nie został odnaleziony."); return []; }
        var results = new Dictionary<string, ImportedCategoryPreview>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in sheet.CellsUsed())
        {
            var name = cell.GetString().Trim();
            if (!IsCandidate(name)) continue;
            var rowText = string.Join(" ", sheet.Row(cell.Address.RowNumber).CellsUsed().Select(x => x.GetString()));
            var kind = Normalize(rowText).Contains("przych") ? TransactionKind.Income : TransactionKind.Expense;
            results.TryAdd(name, new ImportedCategoryPreview(name, null, kind));
        }
        return results.Values.OrderBy(x => x.AppliesTo).ThenBy(x => x.Name).ToList();
    }

    private static bool IsCandidate(string value) =>
        value.Length is > 1 and <= 100 && !value.Contains("#REF!", StringComparison.OrdinalIgnoreCase) &&
        !IgnoredWords.Any(word => Normalize(value).Contains(Normalize(word)));

    private static bool TryGetDecimal(IXLCell cell, out decimal value)
    {
        value = 0;
        if (cell.DataType == XLDataType.Number) { value = cell.GetValue<decimal>(); return true; }
        return decimal.TryParse(cell.GetString(), System.Globalization.NumberStyles.Number, new System.Globalization.CultureInfo("pl-PL"), out value);
    }

    private static AccountType GuessAccountType(string value)
    {
        var name = Normalize(value);
        if (name.Contains("gotow") || name.Contains("kasa")) return AccountType.Cash;
        if (name.Contains("kredyt") || name.Contains("dlug") || name.Contains("zobow")) return AccountType.Liability;
        if (name.Contains("lokata")) return AccountType.Deposit;
        if (name.Contains("tfi") || name.Contains("akcj") || name.Contains("inwest")) return AccountType.Investment;
        if (name.Contains("oszcz") || name.Contains("poduszk")) return AccountType.Savings;
        return AccountType.Bank;
    }

    private static string Normalize(string input) => input.ToLowerInvariant()
        .Replace('ą', 'a').Replace('ć', 'c').Replace('ę', 'e').Replace('ł', 'l').Replace('ń', 'n')
        .Replace('ó', 'o').Replace('ś', 's').Replace('ź', 'z').Replace('ż', 'z').Trim();
}
