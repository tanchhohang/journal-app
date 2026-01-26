using JournalEntry = journalApp.Models.Entry;

namespace journalApp.Services
{
    public interface IEntryService
    {
        Task<List<JournalEntry>> GetAllEntriesAsync();
        Task<JournalEntry?> GetEntryByIdAsync(int id);
        Task<JournalEntry?> GetEntryByDateAsync(DateTime date);
        Task<bool> HasEntryForDateAsync(DateTime date);
        Task<int> AddEntryAsync(JournalEntry entry);
        Task<bool> UpdateEntryAsync(JournalEntry entry);
        Task<bool> DeleteEntryAsync(int id);
        Task<List<string>> GetAllTagsAsync();
        Task<List<JournalEntry>> SearchEntriesAsync(string searchText);
        Task<List<JournalEntry>> FilterEntriesAsync(FilterCriteria criteria);
    }

    public class FilterCriteria
    {
        public string SearchText { get; set; } = "";
        public string MoodCategory { get; set; } = "";
        public string SpecificMood { get; set; } = "";
        public string Tag { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}