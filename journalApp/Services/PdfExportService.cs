using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.Text.RegularExpressions;
using JournalEntry = journalApp.Models.Entry;

namespace journalApp.Services;

public class PdfExportService : IPdfExportService
{
    public async Task<string> ExportJournalToPdfAsync(JournalEntry entry)
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var journalsPath = Path.Combine(documentsPath, "JournalExports");
            
            if (!Directory.Exists(journalsPath))
            {
                Directory.CreateDirectory(journalsPath);
            }

            var sanitizedTitle = SanitizeFileName(entry.Title);
            var fileName = $"{entry.Date:yyyy-MM-dd}_{sanitizedTitle}.pdf";
            var outputPath = Path.Combine(journalsPath, fileName);

            await Task.Run(() => GenerateSingleEntryPdf(entry, outputPath));

            return outputPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error exporting journal: {ex.Message}", ex);
        }
    }

    public async Task<string> ExportMultipleJournalsToPdfAsync(List<JournalEntry> entries, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            if (entries == null || !entries.Any())
            {
                throw new Exception("No entries to export");
            }

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var journalsPath = Path.Combine(documentsPath, "JournalExports");
            
            if (!Directory.Exists(journalsPath))
            {
                Directory.CreateDirectory(journalsPath);
            }

            var fileName = GenerateMultipleEntriesFileName(entries, fromDate, toDate);
            var outputPath = Path.Combine(journalsPath, fileName);

            var sortedEntries = entries.OrderByDescending(e => e.Date).ToList();

            await Task.Run(() => GenerateMultipleEntriesPdf(sortedEntries, outputPath, fromDate, toDate));

            return outputPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error exporting journals: {ex.Message}", ex);
        }
    }

    private void GenerateSingleEntryPdf(JournalEntry entry, string outputPath)
    {
        PdfWriter? writer = null;
        PdfDocument? pdf = null;
        Document? document = null;

        try
        {
            writer = new PdfWriter(outputPath);
            pdf = new PdfDocument(writer);
            document = new Document(pdf);

            // Use simple fonts that work on all platforms
            PdfFont titleFont;
            PdfFont normalFont;
            PdfFont boldFont;

            try
            {
                titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            }
            catch
            {
                // Fallback to courier if helvetica fails
                titleFont = PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD);
                normalFont = PdfFontFactory.CreateFont(StandardFonts.COURIER);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD);
            }

            // Title
            var title = new Paragraph(entry.Title ?? "Untitled")
                .SetFont(titleFont)
                .SetFontSize(24)
                .SetFontColor(ColorConstants.BLACK)
                .SetMarginBottom(10);
            document.Add(title);

            // Date
            var date = new Paragraph(entry.Date.ToString("dddd, MMMM dd, yyyy"))
                .SetFont(normalFont)
                .SetFontSize(12)
                .SetFontColor(new DeviceRgb(102, 102, 102))
                .SetMarginBottom(5);
            document.Add(date);

            // Mood
            var moodCategory = GetMoodCategory(entry.Mood);
            var moodColor = GetMoodDeviceRgb(moodCategory);
            var mood = new Paragraph($"Mood: {entry.Mood} ({moodCategory})")
                .SetFont(boldFont)
                .SetFontSize(11)
                .SetFontColor(moodColor)
                .SetMarginBottom(10);
            document.Add(mood);

            // Tags
            if (entry.Tags != null && entry.Tags.Any())
            {
                var tags = new Paragraph($"Tags: {string.Join(", ", entry.Tags)}")
                    .SetFont(normalFont)
                    .SetFontSize(10)
                    .SetFontColor(new DeviceRgb(102, 102, 102))
                    .SetMarginBottom(15);
                document.Add(tags);
            }

            // Separator line
            var line = new Paragraph()
                .SetMarginBottom(20)
                .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.LIGHT_GRAY, 1));
            document.Add(line);

            // Content
            var plainText = StripHtml(entry.Content ?? "");
            var paragraphs = plainText.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var para in paragraphs)
            {
                if (!string.IsNullOrWhiteSpace(para))
                {
                    var contentPara = new Paragraph(para.Trim())
                        .SetFont(normalFont)
                        .SetFontSize(11)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.JUSTIFIED)
                        .SetMarginBottom(10);
                    document.Add(contentPara);
                }
            }

            // Footer
            document.Add(new Paragraph()
                .SetMarginTop(30)
                .SetBorderTop(new iText.Layout.Borders.SolidBorder(ColorConstants.LIGHT_GRAY, 1)));

            var footer = new Paragraph($"Exported from Journal App - {DateTime.Now:MMMM dd, yyyy}")
                .SetFont(normalFont)
                .SetFontSize(9)
                .SetFontColor(new DeviceRgb(153, 153, 153))
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetMarginTop(10);
            document.Add(footer);
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            document?.Close();
            pdf?.Close();
            writer?.Close();
        }
    }

    private void GenerateMultipleEntriesPdf(List<JournalEntry> entries, string outputPath, DateTime? fromDate, DateTime? toDate)
    {
        PdfWriter? writer = null;
        PdfDocument? pdf = null;
        Document? document = null;

        try
        {
            writer = new PdfWriter(outputPath);
            pdf = new PdfDocument(writer);
            document = new Document(pdf);

            // Use simple fonts that work on all platforms
            PdfFont titleFont;
            PdfFont normalFont;
            PdfFont boldFont;

            try
            {
                titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            }
            catch
            {
                titleFont = PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD);
                normalFont = PdfFontFactory.CreateFont(StandardFonts.COURIER);
                boldFont = PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD);
            }

            // Main Title
            var mainTitle = new Paragraph("Journal Entries")
                .SetFont(titleFont)
                .SetFontSize(28)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetMarginBottom(10);
            document.Add(mainTitle);

            // Date Range
            var dateRangeText = "";
            if (fromDate.HasValue && toDate.HasValue)
            {
                dateRangeText = $"{fromDate.Value:MMMM dd, yyyy} - {toDate.Value:MMMM dd, yyyy}";
            }
            else if (fromDate.HasValue)
            {
                dateRangeText = $"From {fromDate.Value:MMMM dd, yyyy}";
            }
            else if (toDate.HasValue)
            {
                dateRangeText = $"Until {toDate.Value:MMMM dd, yyyy}";
            }
            else
            {
                dateRangeText = "All Entries";
            }

            var subtitle = new Paragraph(dateRangeText)
                .SetFont(normalFont)
                .SetFontSize(14)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetFontColor(new DeviceRgb(102, 102, 102))
                .SetMarginBottom(5);
            document.Add(subtitle);

            var count = new Paragraph($"{entries.Count} {(entries.Count == 1 ? "Entry" : "Entries")}")
                .SetFont(normalFont)
                .SetFontSize(11)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetFontColor(new DeviceRgb(153, 153, 153))
                .SetMarginBottom(30);
            document.Add(count);

            // Add each entry
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // Entry Title
                var entryTitle = new Paragraph(entry.Title ?? "Untitled")
                    .SetFont(titleFont)
                    .SetFontSize(20)
                    .SetMarginTop(i == 0 ? 0 : 20)
                    .SetMarginBottom(8);
                document.Add(entryTitle);

                // Entry Date and Mood
                var moodCategory = GetMoodCategory(entry.Mood);
                var moodColor = GetMoodDeviceRgb(moodCategory);
                
                var entryMeta = new Paragraph()
                    .Add(new Text($"Date: {entry.Date:dddd, MMMM dd, yyyy}  ")
                        .SetFont(normalFont)
                        .SetFontSize(11)
                        .SetFontColor(new DeviceRgb(102, 102, 102)))
                    .Add(new Text($"Mood: {entry.Mood} ({moodCategory})")
                        .SetFont(boldFont)
                        .SetFontSize(11)
                        .SetFontColor(moodColor))
                    .SetMarginBottom(8);
                document.Add(entryMeta);

                // Tags
                if (entry.Tags != null && entry.Tags.Any())
                {
                    var tags = new Paragraph(string.Join(", ", entry.Tags.Select(t => $"#{t}")))
                        .SetFont(normalFont)
                        .SetFontSize(10)
                        .SetFontColor(new DeviceRgb(102, 102, 102))
                        .SetMarginBottom(10);
                    document.Add(tags);
                }

                // Entry separator
                document.Add(new Paragraph()
                    .SetMarginBottom(12)
                    .SetBorderBottom(new iText.Layout.Borders.SolidBorder(ColorConstants.LIGHT_GRAY, 1)));

                // Content
                var plainText = StripHtml(entry.Content ?? "");
                var paragraphs = plainText.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var para in paragraphs)
                {
                    if (!string.IsNullOrWhiteSpace(para))
                    {
                        var contentPara = new Paragraph(para.Trim())
                            .SetFont(normalFont)
                            .SetFontSize(11)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.JUSTIFIED)
                            .SetMarginBottom(10);
                        document.Add(contentPara);
                    }
                }

                // Add separator between entries (but not after the last one)
                if (i < entries.Count - 1)
                {
                    document.Add(new Paragraph()
                        .SetMarginTop(20)
                        .SetMarginBottom(20)
                        .SetHeight(2)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                }
            }

            // Footer
            document.Add(new Paragraph()
                .SetMarginTop(30)
                .SetBorderTop(new iText.Layout.Borders.SolidBorder(ColorConstants.LIGHT_GRAY, 1)));

            var footer = new Paragraph($"Exported from Journal App - {DateTime.Now:MMMM dd, yyyy HH:mm}")
                .SetFont(normalFont)
                .SetFontSize(9)
                .SetFontColor(new DeviceRgb(153, 153, 153))
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetMarginTop(10);
            document.Add(footer);
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            document?.Close();
            pdf?.Close();
            writer?.Close();
        }
    }

    private string GenerateMultipleEntriesFileName(List<JournalEntry> entries, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue)
        {
            return $"Journals_{fromDate.Value:yyyy-MM-dd}_to_{toDate.Value:yyyy-MM-dd}.pdf";
        }
        else if (fromDate.HasValue)
        {
            return $"Journals_from_{fromDate.Value:yyyy-MM-dd}.pdf";
        }
        else if (toDate.HasValue)
        {
            return $"Journals_until_{toDate.Value:yyyy-MM-dd}.pdf";
        }
        else
        {
            return $"Journals_{entries.Count}_entries_{DateTime.Now:yyyy-MM-dd}.pdf";
        }
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "";

        html = html.Replace("&nbsp;", " ");
        html = html.Replace("&amp;", "&");
        html = html.Replace("&lt;", "<");
        html = html.Replace("&gt;", ">");
        html = html.Replace("&quot;", "\"");
        
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</p>", "\n\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<p[^>]*>", "", RegexOptions.IgnoreCase);
        
        var text = Regex.Replace(html, "<.*?>", "");
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n\s*\n\s*\n+", "\n\n");
        
        return text.Trim();
    }

    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            fileName = "journal";
            
        var invalids = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50);
        
        if (string.IsNullOrEmpty(sanitized))
            sanitized = "journal";
            
        return sanitized;
    }

    private string GetMoodCategory(string mood)
    {
        if (string.IsNullOrEmpty(mood))
            return "Neutral";
            
        var positive = new[] { "Happy", "Excited", "Relaxed", "Grateful", "Confident" };
        var neutral = new[] { "Calm", "Thoughtful", "Curious", "Nostalgic", "Bored" };
        var negative = new[] { "Sad", "Angry", "Stressed", "Lonely", "Anxious" };

        if (positive.Contains(mood)) return "Positive";
        if (neutral.Contains(mood)) return "Neutral";
        if (negative.Contains(mood)) return "Negative";
        return "Neutral";
    }

    private DeviceRgb GetMoodDeviceRgb(string category)
    {
        return category switch
        {
            "Positive" => new DeviceRgb(40, 167, 69),    // Green
            "Negative" => new DeviceRgb(220, 53, 69),     // Red
            _ => new DeviceRgb(255, 193, 7)               // Orange/Yellow
        };
    }
}