namespace journalApp.Models
{
    public class Entry
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public string Mood { get; set; } = "Happy";
        public string Content { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
    }
}