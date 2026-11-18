using System.IO.Compression;
using ClosedXML.Excel;

namespace MarcToken.Api.Services;

/// <summary>
/// Yüklenen/üretilen iş dosyaları üzerinde saf (DB'siz) işlemler: doğrulama, ISBN sayımı, RIS üretimi.
/// </summary>
public static class JobFileInspector
{
    public static void ValidateUploadFile(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext is not ".xlsx")
        {
            throw new InvalidOperationException("Sadece .xlsx formatında Excel dosyaları yüklenebilir.");
        }

        var contentType = file.ContentType?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(contentType)
            && contentType is not "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            && contentType is not "application/octet-stream")
        {
            throw new InvalidOperationException("Geçersiz dosya formatı. Lütfen geçerli bir .xlsx dosyası yükleyin.");
        }
    }

    // .xlsx bir zip arşividir; ayrıştırmadan önce açılmış boyutu sınırlanır (zip bombası koruması).
    // Normal bir ISBN listesi bu sınırların çok altında kalır.
    private const long MaxXlsxUncompressedBytes = 200L * 1024 * 1024;
    private const int MaxXlsxEntries = 2000;

    /// <summary>Dosyanın geçerli bir zip olduğunu ve açıldığında makul boyutta kaldığını doğrular.</summary>
    public static void EnsureSafeXlsx(string excelPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(excelPath);
            if (zip.Entries.Count > MaxXlsxEntries
                || zip.Entries.Sum(e => e.Length) > MaxXlsxUncompressedBytes)
            {
                throw new InvalidOperationException("Excel dosyası çok büyük. Lütfen yalnızca ISBN listesini içeren bir dosya yükleyin.");
            }
        }
        catch (InvalidDataException)
        {
            throw new InvalidOperationException("Geçersiz dosya formatı. Lütfen geçerli bir .xlsx dosyası yükleyin.");
        }
    }

    public static int CountIsbns(string excelPath)
    {
        try
        {
            using var wb = new XLWorkbook(excelPath);
            if (!wb.Worksheets.Any()) return 0;

            var ws = wb.Worksheets.First();
            var rows = ws.RowsUsed().ToList();
            if (rows.Count == 0) return 0;

            int? isbnColIndex = null;
            var firstRow = rows.First();
            foreach (var cell in firstRow.CellsUsed())
            {
                if (string.Equals(cell.GetString().Trim(), "isbn", StringComparison.OrdinalIgnoreCase))
                {
                    isbnColIndex = cell.Address.ColumnNumber;
                    break;
                }
            }

            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (isbnColIndex.HasValue && row.RowNumber() == firstRow.RowNumber())
                    continue;

                string raw = "";
                if (isbnColIndex.HasValue)
                {
                    raw = row.Cell(isbnColIndex.Value).GetString().Trim();
                }
                else
                {
                    var firstCell = row.CellsUsed().FirstOrDefault();
                    if (firstCell != null)
                    {
                        raw = firstCell.GetString().Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(raw) || string.Equals(raw, "isbn", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                values.Add(raw);
            }

            return values.Count;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Excel dosyası okunamadı. Dosyanın geçerli ve bozuk olmayan bir .xlsx dosyası olduğundan emin olun. Hata detay: {ex.Message}");
        }
    }

    /// <summary>ISO 2709 dosyasındaki kayıt sayısı (her kayıt 0x1D ile biter).</summary>
    public static int CountMarcRecords(string mrcPath)
    {
        try
        {
            using var stream = File.OpenRead(mrcPath);
            var count = 0;
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                if (b == 0x1D) count++;
            }
            return count;
        }
        catch
        {
            return 0;
        }
    }

    public static (int FoundCount, int NotFoundCount) CountRisRecords(string risPath, int totalIsbns)
    {
        var foundCount = 0;
        try
        {
            foreach (var line in File.ReadLines(risPath))
            {
                if (line.TrimStart().StartsWith("TY  -", StringComparison.OrdinalIgnoreCase))
                {
                    foundCount++;
                }
            }
        }
        catch
        {
            // ignore
        }
        var notFound = Math.Max(0, totalIsbns - foundCount);
        return (foundCount, notFound);
    }

    public static (int FoundCount, int NotFoundCount) GenerateRisFromExcel(string excelPath, string risPath)
    {
        using var wb = new XLWorkbook(excelPath);
        var ws = wb.Worksheets.FirstOrDefault();
        if (ws is null)
        {
            File.WriteAllText(risPath, string.Empty, System.Text.Encoding.UTF8);
            return (0, 0);
        }

        var rows = ws.RowsUsed().ToList();
        if (rows.Count <= 1)
        {
            File.WriteAllText(risPath, string.Empty, System.Text.Encoding.UTF8);
            return (0, 0);
        }

        var headerRow = rows[0];
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            colMap[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        }

        string GetVal(IXLRow row, string headerName)
        {
            if (colMap.TryGetValue(headerName, out var colIdx))
            {
                return row.Cell(colIdx).GetString().Trim();
            }
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        int foundCount = 0;
        int notFoundCount = 0;

        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var status = GetVal(row, "Durum").ToLowerInvariant();
            if (status != "found")
            {
                notFoundCount++;
                continue;
            }

            foundCount++;
            sb.AppendLine("TY  - BOOK");

            var title = GetVal(row, "Başlık");
            if (!string.IsNullOrEmpty(title))
                sb.AppendLine($"TI  - {title}");

            var author = GetVal(row, "Yazar");
            if (!string.IsNullOrEmpty(author))
            {
                var authors = author.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var a in authors)
                {
                    sb.AppendLine($"AU  - {a}");
                }
            }

            var publisher = GetVal(row, "Yayınevi");
            if (!string.IsNullOrEmpty(publisher))
                sb.AppendLine($"PB  - {publisher}");

            var year = GetVal(row, "Yıl");
            if (!string.IsNullOrEmpty(year))
            {
                var match = System.Text.RegularExpressions.Regex.Match(year, @"\b(1\d{3}|20\d{2})\b");
                sb.AppendLine($"PY  - {(match.Success ? match.Value : year)}");
            }

            var isbn = GetVal(row, "ISBN");
            if (!string.IsNullOrEmpty(isbn))
                sb.AppendLine($"SN  - {isbn}");

            var language = GetVal(row, "Dil");
            if (!string.IsNullOrEmpty(language))
                sb.AppendLine($"LA  - {language}");

            var pages = GetVal(row, "Sayfa");
            if (!string.IsNullOrEmpty(pages))
                sb.AppendLine($"SP  - {pages}");

            var subjects = GetVal(row, "Konu Başlıkları");
            if (!string.IsNullOrEmpty(subjects))
            {
                var subjs = subjects.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var s in subjs)
                {
                    sb.AppendLine($"KW  - {s}");
                }
            }

            sb.AppendLine("ER  - ");
            sb.AppendLine();
        }

        File.WriteAllText(risPath, sb.ToString(), System.Text.Encoding.UTF8);
        return (foundCount, notFoundCount);
    }
}
