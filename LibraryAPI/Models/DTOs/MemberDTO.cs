using System;

namespace LibraryAPI.Models.DTOs
{
    public class MemberDTO
    {
        public int Id { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime MembershipDate { get; set; }
        public string Status { get; set; } = "Active";
        public string StatusBadge => Status.ToLower() switch
        {
            "active" => "success",
            "inactive" => "secondary",
            "suspended" => "danger",
            _ => "secondary"
        };
    }

    public class MemberSummaryDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusBadge { get; set; } = string.Empty;
    }
} 