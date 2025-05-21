using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace LibraryAPI.Models
{
    [XmlRoot("Borrowing")]
    public class Borrowing
    {
        [XmlAttribute("Id")]
        public int Id { get; set; }

        [XmlElement("BookId")]
        public int BookId { get; set; }

        [XmlElement("MemberId")]
        public int MemberId { get; set; }

        [XmlElement("BorrowDate", DataType = "date")]
        public DateTime BorrowDate { get; set; }

        [XmlElement("DueDate", DataType = "date")]
        public DateTime DueDate { get; set; }

        [XmlElement("ReturnDate", DataType = "date", IsNullable = true)]
        public DateTime? ReturnDate { get; set; }

        [XmlElement("Status")]
        public string Status { get; set; } = "Borrowed";
    }

    [XmlRoot("Borrowings")]
    public class Borrowings
    {
        [XmlElement("Borrowing")]
        public List<Borrowing> BorrowingList { get; set; } = new List<Borrowing>();
    }
}
