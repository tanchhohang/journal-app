namespace journalApp.Services
{
    public class StreakService : IStreakService
    {
        private readonly IEntryService _entryService;
        private const string CURRENT_STREAK_KEY = "current_streak";
        private const string LONGEST_STREAK_KEY = "longest_streak";
        private const string LAST_ENTRY_DATE_KEY = "last_entry_date";

        public StreakService(IEntryService entryService)
        {
            _entryService = entryService;
        }

        public async Task<int> GetCurrentStreak()
        {
            await UpdateStreak();
            return Preferences.Default.Get(CURRENT_STREAK_KEY, 0);
        }

        public async Task<int> GetLongestStreak()
        {
            return Preferences.Default.Get(LONGEST_STREAK_KEY, 0);
        }

        public async Task UpdateStreak()
        {
            try
            {
                var entries = await _entryService.GetAllEntriesAsync();
                if (entries == null || entries.Count == 0)
                {
                    Preferences.Default.Set(CURRENT_STREAK_KEY, 0);
                    return;
                }

                // get all unique dates with entries
                var entryDates = entries
                    .Select(e => e.Date.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                if (entryDates.Count == 0)
                {
                    Preferences.Default.Set(CURRENT_STREAK_KEY, 0);
                    return;
                }

                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);

                // check if theres entry today or yesterday
                if (!entryDates.Contains(today) && !entryDates.Contains(yesterday))
                {
                    // streak broken
                    Preferences.Default.Set(CURRENT_STREAK_KEY, 0);
                    return;
                }

                // calculate current streak
                int currentStreak = 0;
                var checkDate = entryDates.Contains(today) ? today : yesterday;

                foreach (var date in entryDates)
                {
                    if (date == checkDate)
                    {
                        currentStreak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else if (date < checkDate)
                    {
                        break;
                    }
                }

                // update the current streak
                Preferences.Default.Set(CURRENT_STREAK_KEY, currentStreak);

                // update longest streak
                var longestStreak = Preferences.Default.Get(LONGEST_STREAK_KEY, 0);
                if (currentStreak > longestStreak)
                {
                    Preferences.Default.Set(LONGEST_STREAK_KEY, currentStreak);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating streak: {ex.Message}");
            }
        }

        public async Task<List<DateTime>> GetMissedDays(int days = 30)
        {
            try
            {
                var entries = await _entryService.GetAllEntriesAsync();
                var entryDates = entries
                    .Select(e => e.Date.Date)
                    .Distinct()
                    .ToHashSet();

                var missedDays = new List<DateTime>();
                var startDate = DateTime.Today.AddDays(-days);

                for (var date = startDate; date <= DateTime.Today; date = date.AddDays(1))
                {
                    if (!entryDates.Contains(date) && date < DateTime.Today)
                    {
                        missedDays.Add(date);
                    }
                }

                return missedDays;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting missed days: {ex.Message}");
                return new List<DateTime>();
            }
        }
    }
}