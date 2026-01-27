using JournalEntry = journalApp.Models.Entry;

namespace journalApp.Services;

public interface IPdfExportService
{
    Task<string> ExportJournalToPdfAsync(JournalEntry entry);
    Task<string> ExportMultipleJournalsToPdfAsync(List<JournalEntry> entries, DateTime? fromDate = null, DateTime? toDate = null);
}