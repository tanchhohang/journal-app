using SQLite;
using JournalEntry = journalApp.Models.Entry;

namespace journalApp.Services
{
    public class EntryService : IEntryService
    {
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);

        public EntryService()
        {
            // Don't use .Wait() in constructor - let each method initialize
        }

        private async Task InitializeDatabaseAsync()
        {
            if (_database != null)
                return;

            await _initSemaphore.WaitAsync();
            try
            {
                if (_database != null)
                    return;

                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "journal.db3");
                Console.WriteLine($"Database path: {dbPath}");
                
                _database = new SQLiteAsyncConnection(dbPath);
                await _database.CreateTableAsync<JournalEntry>();
                
                Console.WriteLine("Database initialized successfully");
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public async Task<List<JournalEntry>> GetAllEntriesAsync()
        {
            await InitializeDatabaseAsync();
            return await _database!.Table<JournalEntry>()
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<JournalEntry?> GetEntryByIdAsync(int id)
        {
            await InitializeDatabaseAsync();
            return await _database!.Table<JournalEntry>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            await InitializeDatabaseAsync();
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddTicks(-1);
            
            return await _database!.Table<JournalEntry>()
                .Where(e => e.Date >= startOfDay && e.Date <= endOfDay)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasEntryForDateAsync(DateTime date)
        {
            var entry = await GetEntryByDateAsync(date);
            return entry != null;
        }

        public async Task<int> AddEntryAsync(JournalEntry entry)
        {
            await InitializeDatabaseAsync();
            
            // Check if entry already exists for this date
            var existingEntry = await GetEntryByDateAsync(entry.Date);
            if (existingEntry != null)
            {
                throw new InvalidOperationException("An entry already exists for this date.");
            }

            entry.Date = entry.Date.Date; // Normalize to start of day
            var result = await _database!.InsertAsync(entry);
            Console.WriteLine($"Entry added with ID: {entry.Id}");
            return result;
        }

        public async Task<bool> UpdateEntryAsync(JournalEntry entry)
        {
            await InitializeDatabaseAsync();
            var result = await _database!.UpdateAsync(entry);
            Console.WriteLine($"Entry updated: {entry.Id}");
            return result > 0;
        }

        public async Task<bool> DeleteEntryAsync(int id)
        {
            await InitializeDatabaseAsync();
            var result = await _database!.DeleteAsync<JournalEntry>(id);
            Console.WriteLine($"Entry deleted: {id}");
            return result > 0;
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            await InitializeDatabaseAsync();
            var entries = await _database!.Table<JournalEntry>().ToListAsync();
            var allTags = entries
                .SelectMany(e => e.Tags ?? new List<string>())
                .Distinct()
                .OrderBy(t => t)
                .ToList();
            return allTags;
        }

        public async Task<List<JournalEntry>> SearchEntriesAsync(string searchText)
        {
            await InitializeDatabaseAsync();
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllEntriesAsync();

            var entries = await _database!.Table<JournalEntry>().ToListAsync();
            searchText = searchText.ToLower();

            return entries
                .Where(e => 
                    e.Title.ToLower().Contains(searchText) ||
                    e.Content.ToLower().Contains(searchText) ||
                    (e.Tags != null && e.Tags.Any(t => t.ToLower().Contains(searchText))))
                .OrderByDescending(e => e.Date)
                .ToList();
        }

        public async Task<List<JournalEntry>> FilterEntriesAsync(FilterCriteria criteria)
        {
            await InitializeDatabaseAsync();
            var entries = await _database!.Table<JournalEntry>().ToListAsync();

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var searchText = criteria.SearchText.ToLower();
                entries = entries
                    .Where(e =>
                        e.Title.ToLower().Contains(searchText) ||
                        e.Content.ToLower().Contains(searchText) ||
                        (e.Tags != null && e.Tags.Any(t => t.ToLower().Contains(searchText))))
                    .ToList();
            }

            // Apply mood category filter
            if (!string.IsNullOrWhiteSpace(criteria.MoodCategory))
            {
                var positive = new[] { "Happy", "Excited", "Relaxed", "Grateful", "Confident" };
                var neutral = new[] { "Calm", "Thoughtful", "Curious", "Nostalgic", "Bored" };
                var negative = new[] { "Sad", "Angry", "Stressed", "Lonely", "Anxious" };

                entries = criteria.MoodCategory switch
                {
                    "Positive" => entries.Where(e => positive.Contains(e.Mood)).ToList(),
                    "Neutral" => entries.Where(e => neutral.Contains(e.Mood)).ToList(),
                    "Negative" => entries.Where(e => negative.Contains(e.Mood)).ToList(),
                    _ => entries
                };
            }

            // Apply specific mood filter
            if (!string.IsNullOrWhiteSpace(criteria.SpecificMood))
            {
                entries = entries.Where(e => e.Mood == criteria.SpecificMood).ToList();
            }

            // Apply tag filter
            if (!string.IsNullOrWhiteSpace(criteria.Tag))
            {
                entries = entries
                    .Where(e => e.Tags != null && e.Tags.Contains(criteria.Tag))
                    .ToList();
            }

            // Apply date range filter
            if (criteria.FromDate.HasValue)
            {
                entries = entries.Where(e => e.Date >= criteria.FromDate.Value.Date).ToList();
            }

            if (criteria.ToDate.HasValue)
            {
                entries = entries.Where(e => e.Date <= criteria.ToDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            }

            return entries.OrderByDescending(e => e.Date).ToList();
        }

        public async Task<int> GetCurrentStreakAsync()
        {
            await InitializeDatabaseAsync();
            var entries = await _database!.Table<JournalEntry>()
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            if (!entries.Any())
                return 0;

            var streak = 0;
            var currentDate = DateTime.Today;

            // Check if there's an entry today or yesterday
            var latestEntry = entries.First().Date.Date;
            if ((currentDate - latestEntry).Days > 1)
                return 0;

            foreach (var entry in entries)
            {
                var entryDate = entry.Date.Date;
                var expectedDate = currentDate.AddDays(-streak);

                if (entryDate == expectedDate)
                {
                    streak++;
                }
                else if (entryDate < expectedDate)
                {
                    break;
                }
            }

            return streak;
        }

        public async Task<int> GetLongestStreakAsync()
        {
            await InitializeDatabaseAsync();
            var entries = await _database!.Table<JournalEntry>()
                .OrderBy(e => e.Date)
                .ToListAsync();

            if (!entries.Any())
                return 0;

            var longestStreak = 1;
            var currentStreak = 1;

            for (int i = 1; i < entries.Count; i++)
            {
                var prevDate = entries[i - 1].Date.Date;
                var currDate = entries[i].Date.Date;
                var daysDiff = (currDate - prevDate).Days;

                if (daysDiff == 1)
                {
                    currentStreak++;
                    longestStreak = Math.Max(longestStreak, currentStreak);
                }
                else if (daysDiff > 1)
                {
                    currentStreak = 1;
                }
            }

            return longestStreak;
        }
    }
}