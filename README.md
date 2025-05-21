# XML-based Library Management System

A library management system that uses XML for data storage and validation. This project demonstrates XML technologies, web services, and RESTful API concepts with a separate frontend and backend architecture.

## Features

- XML-based data storage with DTD and XSD schema validation
- JWT authentication for secure API access
- RESTful API endpoints for books, members, and borrowing operations
- XSLT transformations for report generation
- Responsive web interface with Bootstrap 5
- Support for:
  - Book management (add, edit, search, delete)
  - Member management (add, edit, delete)
  - Book borrowing and returns
  - Overdue book tracking
  - Library statistics and reporting

## Technical Requirements

- .NET 8.0 SDK or later
- Modern web browser (Chrome, Firefox, Edge, etc.)

## Project Structure

```
LibrarySystem/
├── LibraryAPI/                # Backend API project
│   ├── Controllers/           # API endpoints
│   ├── Models/                # Data models and DTOs
│   ├── Services/              # Business logic
│   ├── Data/                  # XML data files
│   │   ├── DTDs/              # Document Type Definitions
│   │   ├── Schemas/           # XML schemas (XSD)
│   │   └── Transforms/        # XSLT transformations
│   └── Documentation/         # API documentation
├── LibraryFrontend/           # Frontend project
│   ├── Controllers/           # MVC controllers
│   └── wwwroot/               # Static web assets
│       ├── css/               # Stylesheets
│       ├── js/                # JavaScript files
│       └── index.html         # Main web interface
└── README.md                  # This file
```

## XML Technologies Used

- **XML Storage**: All data is stored in XML files
- **XSD Schema Validation**: Strong validation for data integrity
- **DTD Validation**: Alternative validation method
- **XSLT Transformations**: Convert XML to HTML for reporting
- **XPath**: Used in XSLT to select and navigate XML nodes

## Authentication

The application uses JWT (JSON Web Token) authentication:
- Tokens are issued upon successful login
- Tokens must be included in all API requests
- Default admin credentials: username: "admin", password: "admin123"

## API Endpoints

### Authentication
- `POST /api/auth/login`: Login and get a JWT token
- `GET /api/auth/validate`: Validate a JWT token

### Books
- `GET /api/books`: Get all books
- `GET /api/books/{id}`: Get a specific book
- `POST /api/books`: Add a new book
- `PUT /api/books/{id}`: Update a book
- `DELETE /api/books/{id}`: Delete a book
- `GET /api/books/search`: Search books by title, author, ISBN, etc.
- `GET /api/books/report`: Generate a library report (HTML)

### Members
- `GET /api/members`: Get all members
- `GET /api/members/{id}`: Get a specific member
- `POST /api/members`: Add a new member
- `PUT /api/members/{id}`: Update a member
- `DELETE /api/members/{id}`: Delete a member

### Borrowings
- `GET /api/borrowings`: Get all borrowing records
- `GET /api/borrowings/{id}`: Get a specific borrowing record
- `POST /api/borrowings`: Create a new borrowing record
- `PUT /api/borrowings/{id}/return`: Return a borrowed book
- `GET /api/borrowings/overdue`: Get all overdue books

## Getting Started

1. Clone the repository
2. Navigate to the project directory
3. Start the backend:
   ```bash
   cd LibraryAPI
   dotnet run
   ```
4. In a new terminal, start the frontend:
   ```bash
   cd LibraryFrontend
   dotnet run
   ```
5. Open your web browser and navigate to the frontend URL shown in the terminal

## XML vs JSON

This project uses XML for data storage and validation while using JSON for API communication:

1. **Schema Validation**: XML Schema (XSD) provides strong validation capabilities
2. **Complex Document Structures**: XML better handles hierarchical data
3. **Transformation**: XSLT allows for powerful transformations to various formats
4. **Industry Standards**: Many library systems use XML for data exchange
5. **JWT for Authentication**: JSON Web Tokens provide a secure and stateless authentication mechanism

## Data Models

### Book
- Id: Integer
- ISBN: String
- Title: String
- Author: String
- Publisher: String
- PublicationYear: Integer
- Genre: String
- AvailableCopies: Integer
- TotalCopies: Integer

### Member
- Id: Integer
- FirstName: String
- LastName: String
- Email: String
- PhoneNumber: String
- Address: String
- MembershipDate: Date
- Status: String (Active, Inactive, Suspended)

### Borrowing
- Id: Integer
- BookId: Integer
- MemberId: Integer
- BorrowDate: Date
- DueDate: Date
- ReturnDate: Date (nullable)
- Status: String (Borrowed, Returned, Overdue)

## Report Generation

The system includes a reporting feature that uses XSLT to transform XML data into HTML. The report includes:
- Library statistics (total books, members, borrowings)
- Available and overdue books
- Popular genres and authors

## Security Considerations

- JWT authentication secures all API endpoints
- Input validation using XML schemas
- HTTPS for secure communication
- CORS is configured for cross-origin requests
- Form validation on frontend

## Contributing

Feel free to submit issues and enhancement requests! 