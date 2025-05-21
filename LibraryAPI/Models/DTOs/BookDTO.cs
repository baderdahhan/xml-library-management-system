using System;

namespace LibraryAPI.Models.DTOs
{
    public class BookDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Availability => AvailableCopies > 0 ? "Available" : "Out of Stock";
        public int AvailableCopies { get; set; }
        public int TotalCopies { get; set; }
        public string Status => AvailableCopies switch
        {
            0 => "Out of Stock",
            var x when x < TotalCopies / 2 => "Low Stock",
            _ => "In Stock"
        };
    }

    public class BookSummaryDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Availability => AvailableCopies > 0 ? "Available" : "Out of Stock";
        public int AvailableCopies { get; set; }
        public string Status { get; set; } = string.Empty;
    }
} 