using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using Microsoft.AspNetCore.Hosting;

namespace LibraryAPI.Services
{
    public class XmlTransformService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<XmlTransformService> _logger;

        public XmlTransformService(IWebHostEnvironment environment, ILogger<XmlTransformService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public string TransformBooksToHtml(string xmlContent)
        {
            try
            {
                var xsltPath = Path.Combine(_environment.ContentRootPath, "Data", "Transforms", "books-to-html.xslt");
                var xslt = new XslCompiledTransform();
                xslt.Load(xsltPath);

                using var xmlReader = XmlReader.Create(new StringReader(xmlContent));
                using var stringWriter = new StringWriter();
                xslt.Transform(xmlReader, null, stringWriter);

                return stringWriter.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming books XML to HTML");
                throw;
            }
        }

        public IEnumerable<string> ExecuteXPathQuery(string xmlContent, string xpathExpression)
        {
            if (string.IsNullOrEmpty(xmlContent))
            {
                throw new ArgumentNullException(nameof(xmlContent));
            }

            if (string.IsNullOrEmpty(xpathExpression))
            {
                throw new ArgumentNullException(nameof(xpathExpression));
            }

            try
            {
                var doc = new XPathDocument(new StringReader(xmlContent));
                var navigator = doc.CreateNavigator();
                if (navigator == null)
                {
                    throw new XmlException("Failed to create XPath navigator");
                }

                var nodes = navigator.Select(xpathExpression);
                if (nodes == null)
                {
                    throw new XmlException("Failed to execute XPath query");
                }

                var results = new List<string>();
                while (nodes.MoveNext())
                {
                    if (nodes.Current != null)
                    {
                        results.Add(nodes.Current.Value);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing XPath query: {XPath}", xpathExpression);
                throw;
            }
        }

        // Example XPath queries for common operations
        public IEnumerable<string> GetBooksByAuthor(string xmlContent, string author)
        {
            return ExecuteXPathQuery(xmlContent, $"//Book[Author='{author}']/Title");
        }

        public IEnumerable<string> GetAvailableBooks(string xmlContent)
        {
            return ExecuteXPathQuery(xmlContent, "//Book[AvailableCopies > 0]/Title");
        }

        public IEnumerable<string> GetBooksByGenre(string xmlContent, string genre)
        {
            return ExecuteXPathQuery(xmlContent, $"//Book[Genre='{genre}']/Title");
        }

        public IEnumerable<string> GetOverdueBooks(string xmlContent)
        {
            return ExecuteXPathQuery(xmlContent, "//Book[AvailableCopies = 0]/Title");
        }

        public bool ValidateWithDtd(string xmlContent)
        {
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse,
                    ValidationType = ValidationType.DTD
                };

                var dtdPath = Path.Combine(_environment.ContentRootPath, "Data", "Schemas", "books.dtd");
                using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
                while (reader.Read()) { } // Read through the document to validate
                return true;
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "DTD validation failed");
                return false;
            }
        }
    }
} 