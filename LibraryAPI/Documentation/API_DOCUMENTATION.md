# Library API Documentation

## Authentication
All API endpoints except login require a valid JWT token in the Authorization header:
```
Authorization: Bearer <your_jwt_token>
```

## Base URL
```
https://localhost:5004/api
```

## Endpoints

### Authentication
#### Login
- **POST** `/api/auth/login`
- **Description**: Authenticate user and get JWT token
- **Request Body**:
```json
{
    "username": "string",
    "password": "string"
}
```
- **Response**:
```json
{
    "token": "string",
    "username": "string",
    "role": "string"
}
```

#### Validate Token
- **GET** `/api/auth/validate`
- **Description**: Validate an existing JWT token
- **Response**:
```json
{
    "username": "string",
    "role": "string"
}
```

### Books
#### Get All Books
- **GET** `/api/books`
- **Description**: Retrieve all books
- **Response**: JSON format with book details including status

#### Get Book
- **GET** `/api/books/{id}`
- **Description**: Retrieve a specific book by ID
- **Response**: JSON format with book details

#### Search Books
- **GET** `/api/books/search`
- **Parameters**:
  - `title` (optional): Search by book title
  - `author` (optional): Search by author
  - `isbn` (optional): Search by ISBN
  - `publisher` (optional): Search by publisher
  - `genre` (optional): Search by genre
  - `available` (optional): Filter by availability (true/false)
- **Response**: JSON format with matching books

#### Create Book
- **POST** `/api/books`
- **Description**: Add a new book
- **Request Body**: JSON format
```json
{
    "isbn": "string",
    "title": "string",
    "author": "string",
    "publisher": "string",
    "publicationYear": 0,
    "genre": "string",
    "availableCopies": 0,
    "totalCopies": 0
}
```

#### Update Book
- **PUT** `/api/books/{id}`
- **Description**: Update an existing book
- **Request Body**: JSON format (same as create)
- **Response**: 204 No Content

#### Delete Book
- **DELETE** `/api/books/{id}`
- **Description**: Delete a book
- **Response**: 204 No Content

#### Generate Report
- **GET** `/api/books/report`
- **Description**: Generate a library report with statistics
- **Response**: HTML document containing:
  - Total books, members, and borrowings
  - Available and overdue books
  - Popular genres and authors
  - Generated date and timestamp

### Members
#### Get All Members
- **GET** `/api/members`
- **Description**: Retrieve all members
- **Response**: JSON format with members list
```json
{
    "memberList": [
        {
            "id": 0,
            "firstName": "string",
            "lastName": "string",
            "email": "string",
            "phoneNumber": "string",
            "address": "string",
            "membershipDate": "date",
            "status": "string"
        }
    ]
}
```

#### Get Member
- **GET** `/api/members/{id}`
- **Description**: Retrieve a specific member
- **Response**: JSON format with member details

#### Create Member
- **POST** `/api/members`
- **Description**: Register a new member
- **Request Body**: JSON format
```json
{
    "firstName": "string",
    "lastName": "string",
    "email": "string",
    "phoneNumber": "string",
    "address": "string"
}
```

#### Update Member
- **PUT** `/api/members/{id}`
- **Description**: Update a member's information
- **Request Body**: JSON format (same as create with optional status)
- **Response**: 204 No Content

#### Delete Member
- **DELETE** `/api/members/{id}`
- **Description**: Delete a member
- **Response**: 204 No Content

### Borrowings
#### Get All Borrowings
- **GET** `/api/borrowings`
- **Description**: Retrieve all borrowings
- **Response**: JSON format with borrowing details including book and member info
```json
{
    "borrowingList": [
        {
            "id": 0,
            "bookId": 0,
            "memberId": 0,
            "borrowDate": "date",
            "dueDate": "date",
            "returnDate": "date",
            "status": "string",
            "bookTitle": "string",
            "memberName": "string"
        }
    ]
}
```

#### Get Borrowing
- **GET** `/api/borrowings/{id}`
- **Description**: Retrieve a specific borrowing record
- **Response**: JSON format with borrowing details

#### Create Borrowing
- **POST** `/api/borrowings`
- **Description**: Create a new borrowing record
- **Request Body**: JSON format
```json
{
    "bookId": 0,
    "memberId": 0,
    "borrowDate": "date"
}
```

#### Return Book
- **PUT** `/api/borrowings/{id}/return`
- **Description**: Mark a book as returned
- **Response**: 204 No Content

#### Get Overdue Borrowings
- **GET** `/api/borrowings/overdue`
- **Description**: Get all overdue borrowings
- **Response**: JSON format with overdue borrowing details

## Error Responses
All endpoints may return the following error responses:

### 400 Bad Request
```json
{
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "One or more validation errors occurred.",
    "status": 400,
    "errors": {
        "field": ["error message"]
    }
}
```

### 401 Unauthorized
```json
{
    "error": "Unauthorized",
    "message": "Invalid or missing token"
}
```

### 404 Not Found
```json
{
    "error": "Not Found",
    "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
    "error": "Internal Server Error",
    "message": "An error occurred while processing your request"
}
```

## XML Validation
While the API communicates using JSON, internally all data is stored as XML and validated using:
- DTD (Document Type Definitions)
- XSD (XML Schema Definition)

This ensures data integrity and consistency across the application.

## XSLT Transformation
The report endpoint uses XSLT to transform XML data into HTML for display. This demonstrates:
- XML to HTML transformation
- Data visualization
- Complex reporting capabilities

## Frontend Integration
The API is consumed by a separate frontend application that:
1. Authenticates users and stores JWT tokens
2. Makes HTTP requests to API endpoints
3. Handles response data and displays it to users
4. Manages user sessions and error handling

## Testing
The API can be tested using:
1. Frontend application (preferred method)
2. Postman/Insomnia collections
3. Automated tests
4. Manual testing through the web interface 