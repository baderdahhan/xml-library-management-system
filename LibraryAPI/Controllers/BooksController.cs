using Microsoft.AspNetCore.Mvc;
using LibraryAPI.Models;
using LibraryAPI.Models.DTOs;
using LibraryAPI.Services;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]  // Simplified route for now
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class BooksController : ControllerBase
    {
        private readonly XmlService _xmlService;
        private readonly XmlTransformService _xmlTransformService;
        private const string FileName = "books.xml";
        private readonly ILogger<BooksController> _logger;

        public BooksController(
            XmlService xmlService, 
            XmlTransformService xmlTransformService,
            ILogger<BooksController> logger)
        {
            _xmlService = xmlService;
            _xmlTransformService = xmlTransformService;
            _logger = logger;
        }

        /// <summary>
        /// Search for books based on various criteria
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<BookDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<BookDTO>> SearchBooks(
            [FromQuery] string? title = null,
            [FromQuery] string? author = null,
            [FromQuery] string? isbn = null,
            [FromQuery] string? publisher = null,
            [FromQuery] string? genre = null,
            [FromQuery] bool? available = null)
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books?.BookList == null) return Ok(new List<BookDTO>());
                var query = books.BookList.AsQueryable();

                if (!string.IsNullOrWhiteSpace(title))
                    query = query.Where(b => b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(author))
                    query = query.Where(b => b.Author.Contains(author, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(isbn))
                    query = query.Where(b => b.ISBN.Contains(isbn, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(publisher))
                    query = query.Where(b => b.Publisher != null && b.Publisher.Contains(publisher, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(genre))
                    query = query.Where(b => b.Genre != null && b.Genre.Contains(genre, StringComparison.OrdinalIgnoreCase));
                if (available.HasValue)
                    query = query.Where(b => b.AvailableCopies > 0 == available.Value);

                var results = query.Select(b => new BookDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN,
                    Publisher = b.Publisher,
                    PublicationYear = b.PublicationYear,
                    Genre = b.Genre,
                    AvailableCopies = b.AvailableCopies,
                    TotalCopies = b.TotalCopies
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching books");
                return StatusCode(500, "An error occurred while searching books");
            }
        }

        [HttpGet]
        public ActionResult<IEnumerable<BookDTO>> GetBooks()
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books?.BookList == null)
                {
                    return Ok(Array.Empty<BookDTO>());
                }

                return Ok(books.BookList.Select(b => new BookDTO
                {
                    Id = b.Id,
                    ISBN = b.ISBN,
                    Title = b.Title,
                    Author = b.Author,
                    Publisher = b.Publisher,
                    PublicationYear = b.PublicationYear,
                    Genre = b.Genre,
                    AvailableCopies = b.AvailableCopies,
                    TotalCopies = b.TotalCopies
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books");
                return StatusCode(500, "An error occurred while retrieving books");
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<BookDTO> GetBook(int id)
        {
            var books = _xmlService.LoadXml<Books>(FileName);
            if (books?.BookList == null) return NotFound();

            var book = books.BookList.FirstOrDefault(b => b.Id == id);
            if (book == null) return NotFound();

            var bookDto = new BookDTO
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                Publisher = book.Publisher,
                PublicationYear = book.PublicationYear,
                Genre = book.Genre,
                AvailableCopies = book.AvailableCopies,
                TotalCopies = book.TotalCopies
            };

            return Ok(bookDto);
        }

        [HttpPost]
        public ActionResult<Book> CreateBook(Book book)
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName) ?? new Books { BookList = new List<Book>() };

                // Generate a new ID
                book.Id = books.BookList.Count > 0 ? books.BookList.Max(b => b.Id) + 1 : 1;

                // Ensure copies are valid
                if (book.TotalCopies < 1) book.TotalCopies = 1;
                if (book.AvailableCopies < 0) book.AvailableCopies = 0;
                if (book.AvailableCopies > book.TotalCopies) book.AvailableCopies = book.TotalCopies;

                // Create a temporary Books object for validation
                var tempBooks = new Books { BookList = new List<Book> { book } };

                // Validate against schema
                var serializer = new XmlSerializer(typeof(Books));
                using var writer = new StringWriter();
                serializer.Serialize(writer, tempBooks);
                var xmlContent = writer.ToString();
                _logger.LogInformation("Validating XML: {XmlContent}", xmlContent);

                if (!_xmlService.ValidateXml(xmlContent, "Books"))
                {
                    _logger.LogError("Book validation failed for: {Book}", System.Text.Json.JsonSerializer.Serialize(book));
                    return BadRequest("Invalid book data. Please check all required fields are filled correctly.");
                }

                books.BookList.Add(book);
                _xmlService.SaveXml(books, FileName);

                return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while creating the book.");
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateBook(int id, Book updatedBook)
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books?.BookList == null) return NotFound();

                var book = books.BookList.FirstOrDefault(b => b.Id == id);
                if (book == null) return NotFound();

                // Update book properties
                book.Title = updatedBook.Title;
                book.Author = updatedBook.Author;
                book.ISBN = updatedBook.ISBN;
                book.Publisher = updatedBook.Publisher;
                book.PublicationYear = updatedBook.PublicationYear;
                book.Genre = updatedBook.Genre;
                book.TotalCopies = Math.Max(1, updatedBook.TotalCopies);
                book.AvailableCopies = Math.Min(book.TotalCopies, Math.Max(0, updatedBook.AvailableCopies));

                // Create a temporary Books object for validation
                var tempBooks = new Books { BookList = new List<Book> { book } };
                var serializer = new XmlSerializer(typeof(Books));
                using var writer = new StringWriter();
                serializer.Serialize(writer, tempBooks);
                if (!_xmlService.ValidateXml(writer.ToString(), "Books"))
                {
                    return BadRequest("Invalid book data. Please check all required fields are filled correctly.");
                }

                _xmlService.SaveXml(books, FileName);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while updating the book.");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBook(int id)
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books?.BookList == null) return NotFound();

                var book = books.BookList.FirstOrDefault(b => b.Id == id);
                if (book == null) return NotFound();

                books.BookList.Remove(book);
                _xmlService.SaveXml(books, FileName);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while deleting the book.");
            }
        }

        private static string GetBookStatus(int availableCopies, int totalCopies)
        {
            return availableCopies switch
            {
                0 => "Out of Stock",
                var x when x < totalCopies / 2 => "Low Stock",
                _ => "In Stock"
            };
        }

        [HttpGet("report")]
        public ActionResult<string> GenerateReport()
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                var members = _xmlService.LoadXml<Members>("members.xml");
                var borrowings = _xmlService.LoadXml<Borrowings>("borrowings.xml");

                if (books == null || members == null || borrowings == null)
                {
                    return NotFound("Required data not found");
                }

                // Create a summary report using the new LibraryReport class
                var report = new LibraryReport
                {
                    TotalBooks = books.BookList.Count,
                    TotalMembers = members.MemberList.Count,
                    ActiveBorrowings = borrowings.BorrowingList.Count(b => b.Status == "Borrowed"),
                    AvailableBooks = books.BookList.Count(b => b.AvailableCopies > 0),
                    OverdueBooks = books.BookList.Count(b => b.AvailableCopies == 0),
                    PopularGenres = books.BookList
                        .GroupBy(b => b.Genre)
                        .Select(g => new GenreCount { Genre = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList(),
                    PopularAuthors = books.BookList
                        .GroupBy(b => b.Author)
                        .Select(g => new AuthorCount { Author = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(5)
                        .ToList()
                };

                // Transform the report to HTML using XSLT
                var serializer = new XmlSerializer(typeof(LibraryReport));
                using var writer = new StringWriter();
                serializer.Serialize(writer, report);
                var xmlContent = writer.ToString();

                var htmlReport = _xmlTransformService.TransformBooksToHtml(xmlContent);
                return Content(htmlReport, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while generating the report.");
            }
        }

        [HttpGet("transform")]
        public ActionResult<string> TransformBooksToHtml()
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books == null) return NotFound("No books found");

                var serializer = new XmlSerializer(typeof(Books));
                using var writer = new StringWriter();
                serializer.Serialize(writer, books);
                var xmlContent = writer.ToString();

                var html = _xmlTransformService.TransformBooksToHtml(xmlContent);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming books to HTML");
                return StatusCode(500, "Error transforming books to HTML");
            }
        }

        [HttpGet("xpath")]
        public ActionResult<IEnumerable<string>> ExecuteXPathQuery([FromQuery] string query)
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books == null) return NotFound("No books found");

                var serializer = new XmlSerializer(typeof(Books));
                using var writer = new StringWriter();
                serializer.Serialize(writer, books);
                var xmlContent = writer.ToString();

                var results = _xmlTransformService.ExecuteXPathQuery(xmlContent, query);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing XPath query: {Query}", query);
                return StatusCode(500, "Error executing XPath query");
            }
        }

        [HttpGet("validate-dtd")]
        public ActionResult<bool> ValidateBooksWithDtd()
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books == null) return NotFound("No books found");

                var serializer = new XmlSerializer(typeof(Books));
                using var writer = new StringWriter();
                serializer.Serialize(writer, books);
                var xmlContent = writer.ToString();

                var isValid = _xmlTransformService.ValidateWithDtd(xmlContent);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating books with DTD");
                return StatusCode(500, "Error validating books with DTD");
            }
        }

        [HttpGet("search/advanced")]
        public IActionResult SearchBooks([FromQuery] string type, [FromQuery] string? term)
        {
            try
            {
                var books = _xmlService.LoadXml<Books>(FileName);
                if (books?.BookList == null) return NotFound();

                IEnumerable<Book> results = type switch
                {
                    "author" when !string.IsNullOrEmpty(term) => 
                        books.BookList.Where(b => b.Author.Contains(term, StringComparison.OrdinalIgnoreCase)),
                    "genre" when !string.IsNullOrEmpty(term) => 
                        books.BookList.Where(b => b.Genre.Contains(term, StringComparison.OrdinalIgnoreCase)),
                    "available" => 
                        books.BookList.Where(b => b.AvailableCopies > 0),
                    "overdue" => 
                        books.BookList.Where(b => b.AvailableCopies == 0),
                    _ => Enumerable.Empty<Book>()
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching books: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while searching books.");
            }
        }
    }

    [XmlRoot("Books")]
    public class Books
    {
        [XmlElement("Book")]
        public List<Book> BookList { get; set; } = new List<Book>();
    }
} 