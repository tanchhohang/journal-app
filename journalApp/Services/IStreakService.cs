namespace journalApp.Services
{
    public interface IStreakService
    {
        Task<int> GetCurrentStreak();
        Task<int> GetLongestStreak();
        Task UpdateStreak();
        Task<List<DateTime>> GetMissedDays(int days = 30);
    }
}