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
    public class BorrowingsController : ControllerBase
    {
        private readonly XmlService _xmlService;
        private const string FileName = "borrowings.xml";
        private const int DefaultBorrowingDays = 14;
        private readonly ILogger<BorrowingsController> _logger;

        public BorrowingsController(XmlService xmlService, ILogger<BorrowingsController> logger)
        {
            _xmlService = xmlService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<object> GetBorrowings()
        {
            try
            {
                var borrowings = _xmlService.LoadXml<Borrowings>(FileName);
                if (borrowings == null)
                {
                    borrowings = new Borrowings { BorrowingList = new List<Borrowing>() };
                    _xmlService.SaveXml(borrowings, FileName);
                }

                // Load books and members to get names
                var books = _xmlService.LoadXml<Books>("books.xml");
                var members = _xmlService.LoadXml<Members>("members.xml");

                var borrowingDetails = borrowings.BorrowingList.Select(b => new
                {
                    b.Id,
                    b.BookId,
                    b.MemberId,
                    b.BorrowDate,
                    b.DueDate,
                    b.ReturnDate,
                    b.Status,
                    BookTitle = books?.BookList?.FirstOrDefault(book => book.Id == b.BookId)?.Title ?? "Unknown Book",
                    MemberName = members?.MemberList?.FirstOrDefault(member => member.Id == b.MemberId) != null 
                        ? $"{members.MemberList.First(member => member.Id == b.MemberId).FirstName} {members.MemberList.First(member => member.Id == b.MemberId).LastName}"
                        : "Unknown Member"
                }).ToList();

                return Ok(new { borrowingList = borrowingDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting borrowings: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while retrieving borrowings.");
            }
        }

        [HttpGet("{id}")]
        public ActionResult<Borrowing> GetBorrowing(int id)
        {
            var borrowings = _xmlService.LoadXml<Borrowings>(FileName);
            if (borrowings?.BorrowingList == null) return NotFound();

            var borrowing = borrowings.BorrowingList.FirstOrDefault(b => b.Id == id);
            if (borrowing == null) return NotFound();

            return Ok(borrowing);
        }

        [HttpPost]
        public ActionResult<Borrowing> CreateBorrowing(Borrowing borrowing)
        {
            try
            {
                _logger.LogInformation("Received borrowing data: {Borrowing}", System.Text.Json.JsonSerializer.Serialize(borrowing));

                var borrowings = _xmlService.LoadXml<Borrowings>(FileName) ?? new Borrowings { BorrowingList = new List<Borrowing>() };

                // Set default values
                borrowing.BorrowDate = DateTime.UtcNow;
                borrowing.DueDate = DateTime.UtcNow.AddDays(DefaultBorrowingDays);
                borrowing.Status = "Borrowed";
                borrowing.ReturnDate = null; // Explicitly set to null for new borrowings

                // Generate a new ID
                borrowing.Id = borrowings.BorrowingList.Count > 0 ? borrowings.BorrowingList.Max(b => b.Id) + 1 : 1;

                _logger.LogInformation("Processed borrowing data: {Borrowing}", System.Text.Json.JsonSerializer.Serialize(borrowing));

                // Create a temporary Borrowings object for validation
                var tempBorrowings = new Borrowings { BorrowingList = new List<Borrowing> { borrowing } };

                // Validate against schema
                var serializer = new XmlSerializer(typeof(Borrowings));
                using var writer = new StringWriter();
                serializer.Serialize(writer, tempBorrowings);
                var xmlContent = writer.ToString();
                _logger.LogInformation("Validating XML: {XmlContent}", xmlContent);

                try
                {
                    if (!_xmlService.ValidateXml(xmlContent, "Borrowings"))
                    {
                        return BadRequest("Invalid borrowing data. Please check all required fields are filled correctly.");
                    }
                }
                catch (XmlSchemaValidationException ex)
                {
                    _logger.LogError("Borrowing validation failed: {Error}", ex.Message);
                    return BadRequest($"Invalid borrowing data: {ex.Message}");
                }

                // Update book availability
                var books = _xmlService.LoadXml<Books>("books.xml");
                if (books?.BookList == null) return BadRequest("Books data not found");
                
                var book = books.BookList.FirstOrDefault(b => b.Id == borrowing.BookId);
                if (book == null) return BadRequest("Book not found");
                if (book.AvailableCopies <= 0) return BadRequest("Book is not available");

                book.AvailableCopies--;
                _xmlService.SaveXml(books, "books.xml");

                borrowings.BorrowingList.Add(borrowing);
                _xmlService.SaveXml(borrowings, FileName);

                return CreatedAtAction(nameof(GetBorrowing), new { id = borrowing.Id }, borrowing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating borrowing: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while creating the borrowing record.");
            }
        }

        [HttpPut("{id}/return")]
        public IActionResult ReturnBook(int id)
        {
            try
            {
                var borrowings = _xmlService.LoadXml<Borrowings>(FileName);
                if (borrowings?.BorrowingList == null) return NotFound();

                var borrowing = borrowings.BorrowingList.FirstOrDefault(b => b.Id == id);
                if (borrowing == null) return NotFound();
                if (borrowing.Status == "Returned") return BadRequest("Book already returned");

                // Update borrowing record
                borrowing.ReturnDate = DateTime.UtcNow;
                borrowing.Status = "Returned";

                // Create a temporary Borrowings object for validation
                var tempBorrowings = new Borrowings { BorrowingList = new List<Borrowing> { borrowing } };
                var serializer = new XmlSerializer(typeof(Borrowings));
                using var writer = new StringWriter();
                serializer.Serialize(writer, tempBorrowings);
                if (!_xmlService.ValidateXml(writer.ToString(), "Borrowings"))
                {
                    return BadRequest("Invalid borrowing data after return update.");
                }

                // Update book availability
                var books = _xmlService.LoadXml<Books>("books.xml");
                if (books?.BookList != null)
                {
                    var book = books.BookList.FirstOrDefault(b => b.Id == borrowing.BookId);
                    if (book != null)
                    {
                        book.AvailableCopies++;
                        _xmlService.SaveXml(books, "books.xml");
                    }
                }

                _xmlService.SaveXml(borrowings, FileName);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning book: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while returning the book.");
            }
        }

        [HttpGet("overdue")]
        public ActionResult<object> GetOverdueBorrowings()
        {
            try
            {
                var borrowings = _xmlService.LoadXml<Borrowings>(FileName);
                if (borrowings?.BorrowingList == null) return Ok(new List<object>());

                var now = DateTime.UtcNow;
                var overdueBorrowings = borrowings.BorrowingList
                    .Where(b => b.Status == "Borrowed" && b.DueDate < now)
                    .ToList();

                // Update status for overdue books
                foreach (var borrowing in overdueBorrowings)
                {
                    borrowing.Status = "Overdue";
                }

                // Load books and members to get names
                var books = _xmlService.LoadXml<Books>("books.xml");
                var members = _xmlService.LoadXml<Members>("members.xml");

                var overdueDetails = overdueBorrowings.Select(b => new
                {
                    b.Id,
                    b.BookId,
                    b.MemberId,
                    b.BorrowDate,
                    b.DueDate,
                    b.ReturnDate,
                    b.Status,
                    BookTitle = books?.BookList?.FirstOrDefault(book => book.Id == b.BookId)?.Title ?? "Unknown Book",
                    MemberName = members?.MemberList?.FirstOrDefault(member => member.Id == b.MemberId) != null 
                        ? $"{members.MemberList.First(member => member.Id == b.MemberId).FirstName} {members.MemberList.First(member => member.Id == b.MemberId).LastName}"
                        : "Unknown Member"
                }).ToList();

                // Validate the updated borrowings
                var serializer = new XmlSerializer(typeof(Borrowings));
                using var writer = new StringWriter();
                serializer.Serialize(writer, borrowings);
                if (!_xmlService.ValidateXml(writer.ToString(), "Borrowings"))
                {
                    _logger.LogError("Validation failed after updating overdue status");
                    return StatusCode(500, "Error updating overdue status");
                }

                _xmlService.SaveXml(borrowings, FileName);

                return Ok(overdueDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue borrowings: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while retrieving overdue borrowings.");
            }
        }
    }

    [XmlRoot("Borrowings")]
    public class Borrowings
    {
        [XmlElement("Borrowing")]
        public List<Borrowing> BorrowingList { get; set; } = new List<Borrowing>();
    }
} 