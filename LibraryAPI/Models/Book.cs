using System.Xml.Serialization;

namespace LibraryAPI.Models
{
    [XmlRoot("Book")]
    public class Book
    {
        [XmlAttribute("Id")]
        public int Id { get; set; }

        [XmlElement("ISBN")]
        public string ISBN { get; set; } = string.Empty;

        [XmlElement("Title")]
        public string Title { get; set; } = string.Empty;

        [XmlElement("Author")]
        public string Author { get; set; } = string.Empty;

        [XmlElement("Publisher")]
        public string Publisher { get; set; } = string.Empty;

        [XmlElement("PublicationYear")]
        public int PublicationYear { get; set; }

        [XmlElement("Genre")]
        public string Genre { get; set; } = string.Empty;

        [XmlElement("AvailableCopies")]
        public int AvailableCopies { get; set; } = 1;

        [XmlElement("TotalCopies")]
        public int TotalCopies { get; set; } = 1;
    }

    [XmlRoot("Books")]
    public class Books
    {
        [XmlElement("Book")]
        public List<Book> BookList { get; set; } = new List<Book>();
    }
} 