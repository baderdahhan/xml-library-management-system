using System.Xml.Serialization;

namespace LibraryAPI.Models
{
    [XmlRoot("LibraryReport")]
    public class LibraryReport
    {
        [XmlElement("GeneratedDate")]
        public string GeneratedDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [XmlElement("TotalBooks")]
        public int TotalBooks { get; set; }

        [XmlElement("TotalMembers")]
        public int TotalMembers { get; set; }

        [XmlElement("ActiveBorrowings")]
        public int ActiveBorrowings { get; set; }

        [XmlElement("AvailableBooks")]
        public int AvailableBooks { get; set; }

        [XmlElement("OverdueBooks")]
        public int OverdueBooks { get; set; }

        [XmlArray("PopularGenres")]
        [XmlArrayItem("Genre")]
        public List<GenreCount> PopularGenres { get; set; } = new List<GenreCount>();

        [XmlArray("PopularAuthors")]
        [XmlArrayItem("Author")]
        public List<AuthorCount> PopularAuthors { get; set; } = new List<AuthorCount>();
    }

    public class GenreCount
    {
        [XmlElement("Genre")]
        public string Genre { get; set; }

        [XmlElement("Count")]
        public int Count { get; set; }
    }

    public class AuthorCount
    {
        [XmlElement("Author")]
        public string Author { get; set; }

        [XmlElement("Count")]
        public int Count { get; set; }
    }
} 