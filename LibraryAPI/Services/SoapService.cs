using System.ServiceModel;
using System.Xml.Serialization;
using LibraryAPI.Models;

namespace LibraryAPI.Services
{
    [ServiceContract]
    public interface ILibrarySoapService
    {
        [OperationContract]
        Book GetBookByIsbn(string isbn);

        [OperationContract]
        Member GetMemberById(int id);

        [OperationContract]
        Borrowing GetBorrowingDetails(int id);

        [OperationContract]
        bool ValidateBookXml(string xmlContent);
    }

    /// <summary>
    /// SOAP Service implementation for the library system.
    /// Demonstrates SOAP web service approach alongside REST.
    /// SOAP is used here for:
    /// 1. Strong typing and contract-first development
    /// 2. Built-in security and transaction support
    /// 3. Enterprise integration scenarios
    /// 4. Legacy system compatibility
    /// </summary>
    public class LibrarySoapService : ILibrarySoapService
    {
        private readonly XmlService _xmlService;
        private readonly ILogger<LibrarySoapService> _logger;

        public LibrarySoapService(XmlService xmlService, ILogger<LibrarySoapService> logger)
        {
            _xmlService = xmlService;
            _logger = logger;
        }

        public Book GetBookByIsbn(string isbn)
        {
            try
            {
                var books = _xmlService.LoadXml<Books>("books.xml");
                return books?.BookList.FirstOrDefault(b => b.ISBN == isbn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book by ISBN: {Isbn}", isbn);
                throw new FaultException($"Error retrieving book: {ex.Message}");
            }
        }

        public Member GetMemberById(int id)
        {
            try
            {
                var members = _xmlService.LoadXml<Members>("members.xml");
                return members?.MemberList.FirstOrDefault(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member by ID: {Id}", id);
                throw new FaultException($"Error retrieving member: {ex.Message}");
            }
        }

        public Borrowing GetBorrowingDetails(int id)
        {
            try
            {
                var borrowings = _xmlService.LoadXml<Borrowings>("borrowings.xml");
                return borrowings?.BorrowingList.FirstOrDefault(b => b.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving borrowing details: {Id}", id);
                throw new FaultException($"Error retrieving borrowing: {ex.Message}");
            }
        }

        public bool ValidateBookXml(string xmlContent)
        {
            try
            {
                return _xmlService.ValidateXml(xmlContent, "Books");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating book XML");
                throw new FaultException($"Validation error: {ex.Message}");
            }
        }
    }
} 