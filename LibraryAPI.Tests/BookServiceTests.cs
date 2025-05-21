using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Serialization;
using LibraryAPI.Models;
using LibraryAPI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace LibraryAPI.Tests
{
    public class BookServiceTests : IDisposable
    {
        private readonly XmlService _xmlService;
        private readonly Mock<ILogger<XmlService>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly IMemoryCache _cache;           // <-- real cache, not a mock
        private readonly string _testDataPath;

        public BookServiceTests()
        {
            // 1) Setup logger and environment mocks
            _loggerMock = new Mock<ILogger<XmlService>>();
            _envMock    = new Mock<IWebHostEnvironment>();

            // 2) Use a real MemoryCache
            _cache = new MemoryCache(new MemoryCacheOptions());

            // 3) Prepare a temp Data folder with Schemas & DTDs
            _testDataPath = Path.Combine(Path.GetTempPath(), "LibraryAPITests");
            var dataDir = Path.Combine(_testDataPath, "Data");
            var schemasDir = Path.Combine(dataDir, "Schemas");
            var dtdDir = Path.Combine(dataDir, "DTDs");
            Directory.CreateDirectory(schemasDir);
            Directory.CreateDirectory(dtdDir);

            // 4) Copy your real XSDs/DTDs into the temp folder
            var projectRoot = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", ".."));
            var realData = Path.Combine(projectRoot, "LibraryAPI", "Data");

            foreach (var file in Directory.GetFiles(Path.Combine(realData, "Schemas")))
                File.Copy(file, Path.Combine(schemasDir, Path.GetFileName(file)), overwrite: true);

            foreach (var file in Directory.GetFiles(Path.Combine(realData, "DTDs")))
                File.Copy(file, Path.Combine(dtdDir, Path.GetFileName(file)), overwrite: true);

            // 5) Tell XmlService to use our temp folder
            _envMock.Setup(e => e.ContentRootPath).Returns(_testDataPath);

            // 6) Finally instantiate the service under test
            _xmlService = new XmlService(
                _loggerMock.Object,
                _envMock.Object,
                _cache
            );
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
                Directory.Delete(_testDataPath, recursive: true);
        }

        [Fact]
        public void ValidateBookXml_ValidData_ReturnsTrue()
        {
            var book = new Book
            {
                Id = 1,
                ISBN = "978-3-16-148410-0",
                Title = "Test Book",
                Author = "Test Author",
                Publisher = "Test Publisher",
                PublicationYear = 2024,
                Genre = "Test Genre",
                AvailableCopies = 5,
                TotalCopies = 5
            };
            var books = new Books { BookList = new List<Book> { book } };
            var serializer = new XmlSerializer(typeof(Books));
            using var writer = new StringWriter();
            serializer.Serialize(writer, books);
            var xml = writer.ToString();

            Assert.True(_xmlService.ValidateXml(xml, "Books"));
        }

        [Fact]
        public void ValidateBookXml_InvalidData_ThrowsException()
        {
            var invalidXml = "<Books><Book><Invalid/></Book></Books>";
            Assert.Throws<XmlSchemaValidationException>(() =>
                _xmlService.ValidateXml(invalidXml, "Books"));
        }

        [Fact]
        public void SaveAndLoadBook_ValidData_ReturnsSameData()
        {
            var book = new Book
            {
                Id = 1,
                ISBN = "978-3-16-148410-0",
                Title = "Test Book",
                Author = "Test Author",
                Publisher = "Test Publisher",
                PublicationYear = 2024,
                Genre = "Test Genre",
                AvailableCopies = 5,
                TotalCopies = 5
            };
            var books = new Books { BookList = new List<Book> { book } };
            var fileName = "test_books.xml";

            _xmlService.SaveXml(books, fileName);
            var loaded = _xmlService.LoadXml<Books>(fileName);

            Assert.NotNull(loaded);
            Assert.Single(loaded.BookList);
            Assert.Equal("Test Book", loaded.BookList[0].Title);
            Assert.Equal("Test Author", loaded.BookList[0].Author);
        }

        [Fact]
        public void ExecuteXPathQuery_ValidQuery_ReturnsResults()
        {
            var book = new Book
            {
                Id = 1,
                ISBN = "978-3-16-148410-0",
                Title = "Test Book",
                Author = "Test Author",
                Publisher = "Test Publisher",
                PublicationYear = 2024,
                Genre = "Test Genre",
                AvailableCopies = 5,
                TotalCopies = 5
            };
            var books = new Books { BookList = new List<Book> { book } };
            var serializer = new XmlSerializer(typeof(Books));
            using var writer = new StringWriter();
            serializer.Serialize(writer, books);
            var xml = writer.ToString();

            var results = _xmlService.ExecuteXPathQuery(xml, "//Book[Author='Test Author']/Title");
            Assert.Single(results);
            Assert.Equal("Test Book", results.First());
        }
    }
}
