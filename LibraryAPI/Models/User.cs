using System.Xml.Serialization;

namespace LibraryAPI.Models
{
    [XmlRoot("Users")]
    public class Users
    {
        [XmlElement("User")]
        public List<User> UserList { get; set; } = new List<User>();
    }

    public class User
    {
        [XmlAttribute("Id")]
        public int Id { get; set; }

        [XmlAttribute("Username")]
        public string Username { get; set; } = string.Empty;

        [XmlAttribute("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [XmlAttribute("Role")]
        public string Role { get; set; } = "User"; // Default role
    }
} 