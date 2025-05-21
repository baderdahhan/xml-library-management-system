using System;

namespace LibraryAPI.Models.DTOs
{
    public class BorrowingDTO
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "Borrowed";
        public string StatusBadge => Status.ToLower() switch
        {
            "borrowed" => "primary",
            "returned" => "success",
            "overdue" => "danger",
            _ => "secondary"
        };
        public bool IsOverdue => Status == "Borrowed" && DateTime.UtcNow > DueDate;
        public string DaysStatus => Status switch
        {
            "Returned" => "Returned",
            "Borrowed" when IsOverdue => $"Overdue by {(DateTime.UtcNow - DueDate).Days} days",
            "Borrowed" => $"Due in {(DueDate - DateTime.UtcNow).Days} days",
            _ => Status
        };
    }

    public class BorrowingSummaryDTO
    {
        public string BookTitle { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBadge { get; set; } = string.Empty;
        public string DaysStatus { get; set; } = string.Empty;
    }
} 