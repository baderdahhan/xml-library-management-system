using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Text;
using LibraryAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryAPI.Services
{
    /// <summary>
    /// XML Service for handling XML operations in the library system.
    /// XML is chosen over JSON for this system because:
    /// 1. Schema Validation: XML provides strong schema validation through XSD and DTD
    /// 2. Complex Data Structures: Library data has hierarchical relationships (books, members, borrowings)
    /// 3. Data Transformation: XSLT enables powerful report generation and data transformation
    /// 4. Metadata Support: XML attributes allow for rich metadata without cluttering the data structure
    /// 5. Industry Standards: Library systems often use XML for data exchange (MARC, MODS)
    /// </summary>
    public class XmlService
    {
        private readonly string _dataPath;
        private readonly Dictionary<string, XmlSchema> _schemas;
        private readonly Dictionary<string, string> _dtds;
        private readonly ILogger<XmlService> _logger;
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _cacheOptions;

        public XmlService(ILogger<XmlService> logger, IWebHostEnvironment environment, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
            _dataPath = Path.Combine(environment.ContentRootPath, "Data");
            Directory.CreateDirectory(_dataPath);
            Directory.CreateDirectory(Path.Combine(_dataPath, "Schemas"));
            Directory.CreateDirectory(Path.Combine(_dataPath, "DTDs"));

            _cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _schemas = new Dictionary<string, XmlSchema>
            {
                { "Books", LoadSchema("bookschema.xsd") },
                { "Members", LoadSchema("memberschema.xsd") },
                { "Borrowings", LoadSchema("borrowingschema.xsd") }
            };

            _dtds = new Dictionary<string, string>
            {
                { "Books", LoadDtd("books.dtd") },
                { "Members", LoadDtd("members.dtd") },
                { "Borrowings", LoadDtd("borrowings.dtd") }
            };
        }

        private XmlSchema LoadSchema(string schemaName)
        {
            var schemaPath = Path.Combine(_dataPath, "Schemas", schemaName);
            if (!File.Exists(schemaPath))
            {
                throw new FileNotFoundException($"Schema file not found: {schemaPath}");
            }
            using var reader = XmlReader.Create(schemaPath);
            return XmlSchema.Read(reader, null)!;
        }

        private string LoadDtd(string dtdName)
        {
            var dtdPath = Path.Combine(_dataPath, "DTDs", dtdName);
            if (!File.Exists(dtdPath))
            {
                throw new FileNotFoundException($"DTD file not found: {dtdPath}");
            }
            return File.ReadAllText(dtdPath);
        }

        /// <summary>
        /// Loads and deserializes XML data into strongly-typed objects.
        /// XML is preferred here because:
        /// 1. Type Safety: XSD validation ensures data integrity
        /// 2. Namespace Support: Prevents element name conflicts
        /// 3. Schema Evolution: XSD allows for backward compatibility
        /// </summary>
        public T? LoadXml<T>(string fileName) where T : class
        {
            var cacheKey = $"xml_{fileName}";
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.SetOptions(_cacheOptions);
                try
                {
                    var filePath = Path.Combine(_dataPath, fileName);
                    _logger.LogInformation("Loading XML from {FilePath}", filePath);

                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("XML file not found: {FilePath}", filePath);
                        return null;
                    }

                    var fileContent = File.ReadAllText(filePath);
                    _logger.LogInformation("File content length: {Length} bytes", fileContent.Length);

                    if (string.IsNullOrWhiteSpace(fileContent))
                    {
                        _logger.LogWarning("XML file is empty: {FilePath}", filePath);
                        return null;
                    }

                    var serializer = new XmlSerializer(typeof(T));
                    using var reader = new StringReader(fileContent);
                    var result = (T?)serializer.Deserialize(reader);
                    
                    if (result == null)
                    {
                        _logger.LogWarning("Failed to deserialize XML content from {FilePath}", filePath);
                    }
                    else
                    {
                        _logger.LogInformation("Successfully deserialized XML from {FilePath}", filePath);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading XML from {FileName}: {Message}", fileName, ex.Message);
                    throw;
                }
            });
        }

        public void SaveXml<T>(T data, string fileName) where T : class
        {
            var filePath = Path.Combine(_dataPath, fileName);
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StreamWriter(filePath);
            serializer.Serialize(writer, data);

            // Invalidate cache
            var cacheKey = $"xml_{fileName}";
            _cache.Remove(cacheKey);
        }

        /// <summary>
        /// Validates XML against XSD schema.
        /// XML Schema validation provides:
        /// 1. Data Type Validation: Enforces correct data types (e.g., ISBN format)
        /// 2. Required Fields: Ensures all mandatory data is present
        /// 3. Business Rules: Enforces business constraints (e.g., available copies <= total copies)
        /// 4. Complex Validation: Supports custom validation rules and patterns
        /// </summary>
        public bool ValidateXml(string xmlContent, string schemaName)
        {
            if (string.IsNullOrEmpty(xmlContent))
            {
                throw new ArgumentNullException(nameof(xmlContent));
            }

            if (!_schemas.TryGetValue(schemaName, out var schema))
            {
                throw new ArgumentException($"Schema '{schemaName}' not found");
            }

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema
            };
            settings.Schemas.Add(schema);

            var errors = new List<string>();
            settings.ValidationEventHandler += (sender, e) =>
            {
                var message = $"Validation {(e.Severity == XmlSeverityType.Error ? "Error" : "Warning")}: {e.Message}";
                if (e.Exception != null)
                {
                    message += $" Line: {e.Exception.LineNumber}, Position: {e.Exception.LinePosition}";
                }
                errors.Add(message);
            };

            try
            {
                using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
                while (reader.Read()) { } // Read through the document to trigger validation

                if (errors.Any())
                {
                    var errorMessage = string.Join("\n", errors);
                    throw new XmlSchemaValidationException($"XML validation failed:\n{errorMessage}");
                }
                return true;
            }
            catch (XmlSchemaValidationException ex)
            {
                throw; // Re-throw validation exceptions with detailed messages
            }
            catch (Exception ex)
            {
                throw new XmlSchemaValidationException($"XML validation failed: {ex.Message}", ex);
            }
        }

        public bool ValidateXmlWithDtd(string xmlContent, string dtdName)
        {
            if (!_dtds.TryGetValue(dtdName, out var dtdContent))
            {
                throw new ArgumentException($"DTD '{dtdName}' not found");
            }

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.DTD,
                DtdProcessing = DtdProcessing.Parse
            };

            var errors = new List<string>();
            settings.ValidationEventHandler += (sender, e) =>
            {
                var message = $"DTD Validation {(e.Severity == XmlSeverityType.Error ? "Error" : "Warning")}: {e.Message}";
                if (e.Exception != null)
                {
                    message += $" Line: {e.Exception.LineNumber}, Position: {e.Exception.LinePosition}";
                }
                errors.Add(message);
            };

            try
            {
                // Create a new XML document with the DTD
                var doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                // Add the DTD to the document
                var dtdXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE {dtdName} [
{dtdContent}
]>
{xmlContent}";

                using var reader = XmlReader.Create(new StringReader(dtdXml), settings);
                while (reader.Read()) { } // Read through the document to trigger validation

                if (errors.Any())
                {
                    var errorMessage = string.Join("\n", errors);
                    throw new XmlSchemaValidationException($"DTD validation failed:\n{errorMessage}");
                }
                return true;
            }
            catch (XmlSchemaValidationException ex)
            {
                throw; // Re-throw validation exceptions with detailed messages
            }
            catch (Exception ex)
            {
                throw new XmlSchemaValidationException($"DTD validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Transforms XML data using XSLT.
        /// XML/XSLT advantages over JSON:
        /// 1. Declarative Transformation: XSLT provides powerful template-based transformation
        /// 2. Report Generation: Easy conversion to HTML, PDF, or other formats
        /// 3. Complex Transformations: Support for conditional logic and iteration
        /// 4. Separation of Concerns: Transformation logic is separate from data
        /// </summary>
        public string TransformXml<T>(T data, string xsltPath)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (string.IsNullOrEmpty(xsltPath))
            {
                throw new ArgumentNullException(nameof(xsltPath));
            }

            try
            {
                _logger.LogInformation("Starting XML transformation with XSLT: {XsltPath}", xsltPath);

                // Serialize the data to XML
                var serializer = new XmlSerializer(typeof(T));
                using var writer = new StringWriter();
                serializer.Serialize(writer, data);
                var xmlContent = writer.ToString();
                _logger.LogInformation("Serialized XML content length: {Length} bytes", xmlContent.Length);

                // Load the XSLT
                var fullXsltPath = Path.Combine(_dataPath, "Transforms", xsltPath);
                _logger.LogInformation("Looking for XSLT file at: {FullXsltPath}", fullXsltPath);
                
                if (!File.Exists(fullXsltPath))
                {
                    var error = $"XSLT template not found: {xsltPath} at {fullXsltPath}";
                    _logger.LogError(error);
                    throw new FileNotFoundException(error);
                }

                _logger.LogInformation("Loading XSLT template from: {FullXsltPath}", fullXsltPath);
                var xslt = new XslCompiledTransform();
                xslt.Load(fullXsltPath);
                _logger.LogInformation("XSLT template loaded successfully");

                // Transform the XML
                using var xmlReader = XmlReader.Create(new StringReader(xmlContent));
                using var resultWriter = new StringWriter();
                _logger.LogInformation("Starting XML transformation");
                xslt.Transform(xmlReader, null, resultWriter);
                _logger.LogInformation("XML transformation completed successfully");

                var result = resultWriter.ToString();
                _logger.LogInformation("Transformed content length: {Length} bytes", result.Length);
                return result;
            }
            catch (XmlException xmlEx)
            {
                _logger.LogError(xmlEx, "XML transformation error: {Message}", xmlEx.Message);
                throw new XmlException($"Failed to transform XML: {xmlEx.Message}", xmlEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming XML: {Message}", ex.Message);
                throw new XmlException($"Failed to transform XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes XPath queries on XML data.
        /// XML/XPath advantages over JSON:
        /// 1. Powerful Querying: XPath provides rich query capabilities
        /// 2. Standardized Syntax: Well-defined query language for XML
        /// 3. Complex Selection: Support for complex conditions and relationships
        /// 4. Performance: Efficient navigation of XML document structure
        /// </summary>
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
    }
} 