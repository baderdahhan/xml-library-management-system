using Microsoft.AspNetCore.Mvc;
using LibraryAPI.Models;
using LibraryAPI.Services;
using System.Xml.Serialization;
using System.Text;
using System.Xml.Schema;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly XmlService _xmlService;
        private const string FileName = "members.xml";
        private readonly ILogger<MembersController> _logger;

        public MembersController(XmlService xmlService, ILogger<MembersController> logger)
        {
            _xmlService = xmlService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<Members> GetMembers()
        {
            var members = _xmlService.LoadXml<Members>(FileName);
            if (members == null)
            {
                members = new Members { MemberList = new List<Member>() };
                _xmlService.SaveXml(members, FileName);
            }
            return Ok(members);
        }

        [HttpGet("{id}")]
        public ActionResult<Member> GetMember(int id)
        {
            var members = _xmlService.LoadXml<Members>(FileName);
            if (members?.MemberList == null) return NotFound();

            var member = members.MemberList.FirstOrDefault(m => m.Id == id);
            if (member == null) return NotFound();

            return Ok(member);
        }

        [HttpPost]
        public ActionResult<Member> CreateMember(Member member)
        {
            try
            {
                _logger.LogInformation("Received member data: {Member}", System.Text.Json.JsonSerializer.Serialize(member));

                var members = _xmlService.LoadXml<Members>(FileName) ?? new Members { MemberList = new List<Member>() };

                // Generate a new ID
                member.Id = members.MemberList.Count > 0 ? members.MemberList.Max(m => m.Id) + 1 : 1;
                member.MembershipDate = DateTime.Now.Date;
                member.Status = "Active";

                _logger.LogInformation("Processed member data: {Member}", System.Text.Json.JsonSerializer.Serialize(member));

                // Create a temporary Members object for validation
                var tempMembers = new Members { MemberList = new List<Member> { member } };

                // Validate against schema
                var serializer = new XmlSerializer(typeof(Members));
                using var writer = new StringWriter();
                serializer.Serialize(writer, tempMembers);
                var xmlContent = writer.ToString();
                _logger.LogInformation("Validating XML: {XmlContent}", xmlContent);

                try
                {
                    if (!_xmlService.ValidateXml(xmlContent, "Members"))
                    {
                        return BadRequest("Invalid member data. Please check all required fields are filled correctly.");
                    }
                }
                catch (XmlSchemaValidationException ex)
                {
                    _logger.LogError("Member validation failed: {Error}", ex.Message);
                    return BadRequest($"Invalid member data: {ex.Message}");
                }

                members.MemberList.Add(member);
                _xmlService.SaveXml(members, FileName);

                return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while creating the member.");
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateMember(int id, Member updatedMember)
        {
            try
            {
                var members = _xmlService.LoadXml<Members>(FileName);
                if (members?.MemberList == null) return NotFound();

                var member = members.MemberList.FirstOrDefault(m => m.Id == id);
                if (member == null) return NotFound();

                // Update member properties
                member.FirstName = updatedMember.FirstName;
                member.LastName = updatedMember.LastName;
                member.Email = updatedMember.Email;
                member.PhoneNumber = updatedMember.PhoneNumber;
                member.Address = updatedMember.Address;
                member.Status = updatedMember.Status;

                // Create a temporary Members object for validation
                var tempMembers = new Members { MemberList = new List<Member> { member } };
                var serializer = new XmlSerializer(typeof(Members));
                using var writer = new StringWriter();
                serializer.Serialize(writer, tempMembers);
                if (!_xmlService.ValidateXml(writer.ToString(), "Members"))
                {
                    return BadRequest("Invalid member data after update.");
                }

                _xmlService.SaveXml(members, FileName);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while updating the member.");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteMember(int id)
        {
            try
            {
                var members = _xmlService.LoadXml<Members>(FileName);
                if (members?.MemberList == null) return NotFound();

                var member = members.MemberList.FirstOrDefault(m => m.Id == id);
                if (member == null) return NotFound();

                // Check if member has any active borrowings
                var borrowings = _xmlService.LoadXml<Borrowings>("borrowings.xml");
                if (borrowings?.BorrowingList != null)
                {
                    var activeBorrowings = borrowings.BorrowingList
                        .Where(b => b.MemberId == id && (b.Status == "Borrowed" || b.Status == "Overdue"))
                        .ToList();
                    if (activeBorrowings.Any())
                    {
                        return BadRequest("Cannot delete member with active borrowings. Please return all books first.");
                    }
                }

                members.MemberList.Remove(member);
                _xmlService.SaveXml(members, FileName);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting member: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while deleting the member.");
            }
        }
    }

    [XmlRoot("Members")]
    public class Members
    {
        [XmlElement("Member")]
        public List<Member> MemberList { get; set; } = new List<Member>();
    }
} 