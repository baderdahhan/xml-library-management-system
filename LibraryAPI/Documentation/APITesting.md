# API Testing Documentation

## Overview
This document outlines the testing approach for the Library Management System API, including test cases, tools, and procedures.

## Testing Tools
1. **Postman Collection**
   - Import the `LibraryAPI.postman_collection.json` for all API endpoints
   - Environment variables for different environments (Development, Testing, Production)

2. **xUnit Tests**
   - Unit tests for XML validation
   - Integration tests for API endpoints
   - Test coverage for core functionality

## Test Cases

### 1. Authentication Tests

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
    "username": "testuser",
    "password": "testpass"
}
```
Expected Results:
- 200 OK with JWT token
- 401 Unauthorized for invalid credentials
- 400 Bad Request for missing fields

#### Token Validation
```http
GET /api/books
Authorization: Bearer {token}
```
Expected Results:
- 200 OK with books list
- 401 Unauthorized for invalid/missing token
- 403 Forbidden for expired token

### 2. Book Management Tests

#### Get All Books
```http
GET /api/books
Authorization: Bearer {token}
```
Expected Results:
- 200 OK with books array
- XML format validation
- Schema validation

#### Get Book by ID
```http
GET /api/books/{id}
Authorization: Bearer {token}
```
Expected Results:
- 200 OK with book details
- 404 Not Found for invalid ID
- XML validation

#### Create Book
```http
POST /api/books
Authorization: Bearer {token}
Content-Type: application/xml

<Book>
    <ISBN>978-3-16-148410-0</ISBN>
    <Title>Test Book</Title>
    <Author>Test Author</Author>
    <Publisher>Test Publisher</Publisher>
    <PublicationYear>2024</PublicationYear>
    <Genre>Test Genre</Genre>
    <TotalCopies>5</TotalCopies>
    <AvailableCopies>5</AvailableCopies>
</Book>
```
Expected Results:
- 201 Created with new book
- 400 Bad Request for invalid data
- XML schema validation
- DTD validation

#### Update Book
```http
PUT /api/books/{id}
Authorization: Bearer {token}
Content-Type: application/xml

<Book>
    <!-- Updated book data -->
</Book>
```
Expected Results:
- 200 OK with updated book
- 404 Not Found for invalid ID
- 400 Bad Request for invalid data
- XML validation

#### Delete Book
```http
DELETE /api/books/{id}
Authorization: Bearer {token}
```
Expected Results:
- 204 No Content
- 404 Not Found for invalid ID
- 403 Forbidden if book is borrowed

### 3. XML Validation Tests

#### Schema Validation
- Test valid XML against XSD
- Test invalid XML against XSD
- Test missing required fields
- Test invalid data types
- Test pattern validation (ISBN)

#### DTD Validation
- Test valid XML against DTD
- Test invalid XML against DTD
- Test element ordering
- Test required attributes

### 4. Transformation Tests

#### XML to HTML
```http
GET /api/books/transform/html
Authorization: Bearer {token}
```
Expected Results:
- 200 OK with HTML output
- Valid HTML structure
- All book data present

#### XML to Text
```http
GET /api/books/transform/text
Authorization: Bearer {token}
```
Expected Results:
- 200 OK with text output
- Proper formatting
- All book data present

## Test Environment Setup

1. **Development Environment**
   - Local database
   - Test XML files
   - Development JWT key

2. **Testing Environment**
   - Test database
   - Sample data
   - Test JWT key

## Running Tests

### Manual Testing with Postman
1. Import the Postman collection
2. Set up environment variables
3. Run the collection
4. Verify responses
5. Check XML validation
6. Validate transformations

### Automated Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test LibraryAPI.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~BookServiceTests"
```

## Test Data

### Sample Books
```xml
<Books>
    <Book>
        <Id>1</Id>
        <ISBN>978-3-16-148410-0</ISBN>
        <Title>Test Book 1</Title>
        <Author>Test Author 1</Author>
        <Publisher>Test Publisher</Publisher>
        <PublicationYear>2024</PublicationYear>
        <Genre>Test Genre</Genre>
        <TotalCopies>5</TotalCopies>
        <AvailableCopies>5</AvailableCopies>
    </Book>
    <!-- More test books -->
</Books>
```

### Invalid Test Cases
1. Invalid ISBN format
2. Missing required fields
3. Invalid publication year
4. Negative copies
5. Invalid XML structure

## Performance Testing

### Load Testing
- Test with 1000 concurrent requests
- Measure response times
- Check memory usage
- Monitor XML processing

### Stress Testing
- Test with large XML files
- Test with complex transformations
- Test with multiple concurrent users

## Security Testing

### Authentication
- Test token expiration
- Test invalid tokens
- Test missing tokens

### Authorization
- Test role-based access
- Test resource ownership
- Test permission boundaries

## Error Handling

### Expected Errors
1. 400 Bad Request
   - Invalid XML
   - Missing fields
   - Invalid data types

2. 401 Unauthorized
   - Missing token
   - Invalid token
   - Expired token

3. 403 Forbidden
   - Insufficient permissions
   - Resource locked

4. 404 Not Found
   - Invalid resource ID
   - Missing resource

5. 500 Internal Server Error
   - XML processing error
   - Database error
   - Transformation error

## Test Results Documentation

### Test Report Template
1. Test Case ID
2. Description
3. Preconditions
4. Test Steps
5. Expected Results
6. Actual Results
7. Status (Pass/Fail)
8. Comments

### Test Metrics
1. Test Coverage
2. Pass Rate
3. Response Times
4. Error Rates
5. XML Validation Success Rate 