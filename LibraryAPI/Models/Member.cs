using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace LibraryAPI.Models
{
    [XmlRoot("Member")]
    public class Member
    {
        [XmlAttribute("Id")]
        public int Id { get; set; }

        [XmlElement("FirstName")]
        public string FirstName { get; set; } = string.Empty;

        [XmlElement("LastName")]
        public string LastName { get; set; } = string.Empty;

        [XmlElement("Email")]
        public string Email { get; set; } = string.Empty;

        [XmlElement("PhoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [XmlElement("Address")]
        public string Address { get; set; } = string.Empty;

        [XmlElement("MembershipDate", DataType = "date")]
        public DateTime MembershipDate { get; set; }


        [XmlElement("Status")]
        public string Status { get; set; } = "Active";
    }

    [XmlRoot("Members")]
    public class Members
    {
        [XmlElement("Member")]
        public List<Member> MemberList { get; set; } = new List<Member>();
    }
} 