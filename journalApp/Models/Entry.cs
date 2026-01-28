using SQLite;

namespace journalApp.Models
{
    [Table("entries")]
    public class Entry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public string UserId { get; set; } = "";

        [NotNull]
        public string Title { get; set; } = "";

        [NotNull]
        public DateTime Date { get; set; } = DateTime.Now;

        [NotNull]
        public string Mood { get; set; } = "Happy";

        [NotNull]
        public string Content { get; set; } = "";

        public string TagsString { get; set; } = "";

        [Ignore]
        public List<string> Tags 
        { 
            get => string.IsNullOrEmpty(TagsString) 
                ? new List<string>() 
                : TagsString.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
            set => TagsString = value != null ? string.Join(",", value) : "";
        }
    }
}