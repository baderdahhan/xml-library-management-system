// Global state
let isAuthenticated = false;
let currentUser = null;
let currentRole = null;

// API URLs
const API_BASE_URL = 'https://localhost:5004';
const AUTH_API = `${API_BASE_URL}/api/auth`;
const BOOKS_API = `${API_BASE_URL}/api/books`;
const MEMBERS_API = `${API_BASE_URL}/api/members`;
const BORROWINGS_API = `${API_BASE_URL}/api/borrowings`;

// Initialize Bootstrap modals
const loginModal = new bootstrap.Modal(document.getElementById('loginModal'));
const bookModal = new bootstrap.Modal(document.getElementById('bookModal'));
const memberModal = new bootstrap.Modal(document.getElementById('memberModal'));
const borrowingModal = new bootstrap.Modal(document.getElementById('borrowingModal'));
const searchModal = new bootstrap.Modal(document.getElementById('searchModal'));

// Event Listeners
document.addEventListener('DOMContentLoaded', () => {
    // Navigation
    document.querySelectorAll('.nav-link').forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const section = e.target.dataset.section;
            showSection(section);
        });
    });

    // Logout
    document.getElementById('logoutBtn').addEventListener('click', logout);

    // Forms
    document.getElementById('loginForm').addEventListener('submit', handleLogin);
    document.getElementById('bookForm').addEventListener('submit', (e) => e.preventDefault());
    document.getElementById('memberForm').addEventListener('submit', (e) => e.preventDefault());
    document.getElementById('borrowingForm').addEventListener('submit', (e) => e.preventDefault());
    document.getElementById('searchForm').addEventListener('submit', (e) => e.preventDefault());

    // Always show login on startup
    isAuthenticated = false;
    localStorage.removeItem('token');
    updateUI();
    loginModal.show();
    
    // Add event listener for borrowing status change
    const borrowingStatus = document.getElementById('borrowingStatus');
    if (borrowingStatus) {
        borrowingStatus.addEventListener('change', function() {
            const returnDateGroup = document.getElementById('returnDateGroup');
            if (this.value === 'Returned') {
                returnDateGroup.style.display = 'block';
                document.getElementById('returnDate').required = true;
                document.getElementById('returnDate').value = new Date().toISOString().split('T')[0];
            } else {
                returnDateGroup.style.display = 'none';
                document.getElementById('returnDate').required = false;
            }
        });
    }
});

// Authentication
async function handleLogin(e) {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    try {
        const response = await fetch(`${AUTH_API}/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            const data = await response.json();
            localStorage.setItem('token', data.token);
            isAuthenticated = true;
            currentUser = username;
            currentRole = data.role || 'User';
            loginModal.hide();
            updateUI();
            loadData();
            showToast('Login successful', 'success');
        } else {
            showToast('Invalid credentials', 'error');
        }
    } catch (error) {
        console.error('Login failed:', error);
        showToast('Login failed', 'error');
    }
}

function logout() {
    localStorage.removeItem('token');
    isAuthenticated = false;
    currentUser = null;
    currentRole = null;
    updateUI();
    loginModal.show();
}

// UI Updates
function updateUI() {
    const navLinks = document.querySelectorAll('.nav-link');
    const logoutBtn = document.getElementById('logoutBtn');
    
    navLinks.forEach(link => link.style.display = isAuthenticated ? 'block' : 'none');
    logoutBtn.style.display = isAuthenticated ? 'block' : 'none';
}

function showSection(section) {
    document.querySelectorAll('.content-section').forEach(s => s.style.display = 'none');
    document.getElementById(`${section}Section`).style.display = 'block';
}

// Data Loading
async function loadData() {
    await Promise.all([
        loadBooks(),
        loadMembers(),
        loadBorrowings()
    ]);
}

async function loadBooks() {
    try {
        const response = await fetch(BOOKS_API, {
            headers: getAuthHeaders()
        });
        if (response.ok) {
            const books = await response.json();
            renderBooksTable(books);
        }
    } catch (error) {
        console.error('Failed to load books:', error);
        showToast('Failed to load books', 'error');
    }
}

async function loadMembers() {
    try {
        const response = await fetch(`${MEMBERS_API}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to load members');
        const data = await response.json();
        console.log('Members API response:', data);
        
        // Extract the member list - the API returns an object with memberList property
        const members = data.memberList || [];
        
        if (!Array.isArray(members)) {
            console.error('Invalid members data format. Expected array, got:', typeof members);
            showToast('Failed to process members data', 'error');
            return;
        }
        
        renderMembersTable(members);
    } catch (error) {
        console.error('Failed to load members:', error);
        showToast('Failed to load members', 'error');
    }
}

async function loadBorrowings() {
    try {
        const response = await fetch(`${BORROWINGS_API}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to load borrowings');
        const data = await response.json();
        
        // Handle both possible response formats
        const borrowings = data.borrowingList || data;
        
        if (!Array.isArray(borrowings)) {
            console.error('Expected array of borrowings, got:', data);
            showToast('Failed to load borrowings: invalid response format', 'error');
            return;
        }
        
        renderBorrowingsTable(borrowings);
    } catch (error) {
        console.error('Failed to load borrowings:', error);
        showToast('Failed to load borrowings', 'error');
    }
}

// Table Rendering
function renderBooksTable(books) {
    const tbody = document.getElementById('booksTableBody');
    if (!Array.isArray(books) || books.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6">No books found</td></tr>';
        return;
    }
    
    tbody.innerHTML = books.map(book => `
        <tr>
            <td>${book.id}</td>
            <td>${book.title}</td>
            <td>${book.author}</td>
            <td>${book.isbn}</td>
            <td><span class="badge ${getStatusBadgeClass(book.status)}">${book.status}</span></td>
            <td>
                <button class="btn btn-sm btn-primary me-1" onclick="editBook(${book.id})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn btn-sm btn-danger" onclick="deleteBook(${book.id})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
}

function renderMembersTable(members) {
    const tbody = document.getElementById('membersTableBody');
    if (!Array.isArray(members) || members.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5">No members found</td></tr>';
        return;
    }
    
    tbody.innerHTML = members.map(member => `
        <tr>
            <td>${member.id}</td>
            <td>${member.firstName || ''} ${member.lastName || ''}</td>
            <td>${member.email || ''}</td>
            <td>${member.phoneNumber || ''}</td>
            <td>
                <button class="btn btn-sm btn-primary me-1" onclick="editMember(${member.id})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn btn-sm btn-danger" onclick="deleteMember(${member.id})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
}

function renderBorrowingsTable(borrowings) {
    const tbody = document.getElementById('borrowingsTableBody');
    if (!Array.isArray(borrowings) || borrowings.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7">No borrowings found</td></tr>';
        return;
    }
    
    tbody.innerHTML = borrowings.map(borrowing => {
        // Extract member name, handling all possible data structures
        let memberName = 'N/A';
        if (borrowing.member) {
            memberName = `${borrowing.member.firstName || ''} ${borrowing.member.lastName || ''}`.trim();
        } else if (borrowing.memberName) {
            memberName = borrowing.memberName;
        } else if (borrowing.memberFirstName || borrowing.memberLastName) {
            memberName = `${borrowing.memberFirstName || ''} ${borrowing.memberLastName || ''}`.trim();
        }
        
        // Extract book title, handling all possible data structures
        const bookTitle = borrowing.book?.title || borrowing.bookTitle || 'N/A';
        
        // Handle return date display
        let returnDateDisplay;
        if (borrowing.status === 'Returned' && borrowing.returnDate) {
            returnDateDisplay = formatDate(borrowing.returnDate);
        } else if (borrowing.status === 'Borrowed' || borrowing.status === 'Overdue') {
            // For borrowed books, calculate due date (14 days from borrow date)
            const borrowDate = new Date(borrowing.borrowDate);
            const dueDate = new Date(borrowDate);
            dueDate.setDate(dueDate.getDate() + 14);
            returnDateDisplay = `Due: ${formatDate(dueDate)}`;
        } else {
            returnDateDisplay = 'N/A';
        }
        
        return `
        <tr>
            <td>${borrowing.id}</td>
            <td>${bookTitle}</td>
            <td>${memberName}</td>
            <td>${formatDate(borrowing.borrowDate)}</td>
            <td>${returnDateDisplay}</td>
            <td><span class="badge ${getStatusBadgeClass(borrowing.status)}">${borrowing.status}</span></td>
            <td>
                <button class="btn btn-sm btn-primary" onclick="editBorrowing(${borrowing.id})">
                    <i class="fas fa-edit"></i>
                </button>
            </td>
        </tr>
        `;
    }).join('');
}

// Helper Functions
function getAuthHeaders() {
    return {
        'Authorization': `Bearer ${localStorage.getItem('token')}`,
        'Content-Type': 'application/json'
    };
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString();
}

function getStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
        case 'available':
        case 'active':
            return 'badge-success';
        case 'borrowed':
        case 'pending':
            return 'badge-warning';
        case 'overdue':
        case 'inactive':
            return 'badge-danger';
        default:
            return 'badge-secondary';
    }
}

function showToast(message, type = 'info') {
    const toastContainer = document.querySelector('.toast-container');
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    
    toast.addEventListener('hidden.bs.toast', () => {
        toast.remove();
    });
}

// Form Handling
function showBookModal(bookId = null) {
    const form = document.getElementById('bookForm');
    form.reset();
    document.getElementById('bookId').value = bookId || '';
    
    if (bookId) {
        // Load book data for editing
        fetch(`${BOOKS_API}/${bookId}`, {
            headers: getAuthHeaders()
        })
        .then(response => response.json())
        .then(book => {
            document.getElementById('bookTitle').value = book.title;
            document.getElementById('bookAuthor').value = book.author;
            document.getElementById('bookIsbn').value = book.isbn;
            document.getElementById('bookPublisher').value = book.publisher || '';
            document.getElementById('bookPublicationYear').value = book.publicationYear || new Date().getFullYear();
            document.getElementById('bookGenre').value = book.genre || '';
            document.getElementById('bookAvailableCopies').value = book.availableCopies || 0;
            document.getElementById('bookTotalCopies').value = book.totalCopies || 1;
        });
    }
    
    bookModal.show();
}

function showMemberModal() {
    const form = document.getElementById('memberForm');
    form.reset();
    document.getElementById('memberId').value = '';
    
    // Set default date to today for new members
    document.getElementById('membershipDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('memberStatus').value = 'Active';
    
    memberModal.show();
}

async function showBorrowingModal() {
    const form = document.getElementById('borrowingForm');
    form.reset();
    document.getElementById('borrowingId').value = '';
    
    // Set default dates
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date();
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    const nextMonthFormatted = nextMonth.toISOString().split('T')[0];
    
    document.getElementById('borrowingDate').value = today;
    document.getElementById('borrowingStatus').value = 'Borrowed';
    
    // Load books and members for dropdowns
    Promise.all([
        fetch(BOOKS_API, { headers: getAuthHeaders() }).then(r => {
            if (!r.ok) throw new Error('Failed to load books');
            return r.json();
        }),
        fetch(MEMBERS_API, { headers: getAuthHeaders() }).then(r => {
            if (!r.ok) throw new Error('Failed to load members');
            return r.json();
        })
    ]).then(([books, membersData]) => {
        // Ensure books is an array
        const booksArray = Array.isArray(books) ? books : [];
        
        // Ensure members is an array - extract from memberList property
        let members = [];
        if (membersData && typeof membersData === 'object') {
            if (Array.isArray(membersData)) {
                members = membersData;
            } else if (Array.isArray(membersData.memberList)) {
                members = membersData.memberList;
            }
        }
        
        if (!Array.isArray(members)) {
            console.error('Invalid members data format for dropdown');
            showToast('Failed to load members for dropdown', 'error');
            return;
        }
        
        console.log('Members for dropdown:', members);
        
        const bookSelect = document.getElementById('borrowingBook');
        const memberSelect = document.getElementById('borrowingMember');
        
        // Clear existing options
        bookSelect.innerHTML = '';
        memberSelect.innerHTML = '';
        
        // Add placeholder options
        const bookPlaceholder = document.createElement('option');
        bookPlaceholder.value = '';
        bookPlaceholder.textContent = '-- Select a book --';
        bookPlaceholder.disabled = true;
        bookPlaceholder.selected = true;
        bookSelect.appendChild(bookPlaceholder);
        
        const memberPlaceholder = document.createElement('option');
        memberPlaceholder.value = '';
        memberPlaceholder.textContent = '-- Select a member --';
        memberPlaceholder.disabled = true;
        memberPlaceholder.selected = true;
        memberSelect.appendChild(memberPlaceholder);
        
        // Add book options
        booksArray.forEach(book => {
            const option = document.createElement('option');
            option.value = book.id;
            option.textContent = book.title;
            bookSelect.appendChild(option);
        });
        
        // Add member options
        members.forEach(member => {
            const option = document.createElement('option');
            option.value = member.id;
            option.textContent = `${member.firstName || ''} ${member.lastName || ''}`.trim() || 'Member ' + member.id;
            memberSelect.appendChild(option);
        });
        
        borrowingModal.show();
    }).catch(error => {
        console.error('Failed to load dropdown data:', error);
        showToast('Failed to load dropdown data', 'error');
    });
}

function showSearchModal() {
    const form = document.getElementById('searchForm');
    form.reset();
    searchModal.show();
}

// Form Validation
function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function validateForm(formId) {
    const form = document.getElementById(formId);
    let isValid = true;
    
    // Reset previous validation errors
    form.querySelectorAll('.invalid-feedback').forEach(el => el.remove());
    form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
    
    // Validate each required field
    form.querySelectorAll('[required]').forEach(field => {
        if (!field.value.trim()) {
            isValid = false;
            field.classList.add('is-invalid');
            const feedback = document.createElement('div');
            feedback.className = 'invalid-feedback';
            feedback.textContent = `${field.labels[0]?.textContent || 'This field'} is required`;
            field.parentNode.appendChild(feedback);
        }
    });
    
    // Additional validation for email fields
    form.querySelectorAll('input[type="email"]').forEach(emailField => {
        if (emailField.value.trim() && !validateEmail(emailField.value)) {
            isValid = false;
            emailField.classList.add('is-invalid');
            const feedback = document.createElement('div');
            feedback.className = 'invalid-feedback';
            feedback.textContent = 'Please enter a valid email address (e.g., user@example.com)';
            emailField.parentNode.appendChild(feedback);
        }
    });
    
    return isValid;
}

// CRUD Operations
async function submitBook() {
    if (!validateForm('bookForm')) {
        return;
    }
    
    const bookId = document.getElementById('bookId').value;
    const book = {
        title: document.getElementById('bookTitle').value,
        author: document.getElementById('bookAuthor').value,
        isbn: document.getElementById('bookIsbn').value,
        publisher: document.getElementById('bookPublisher').value,
        publicationYear: parseInt(document.getElementById('bookPublicationYear').value),
        genre: document.getElementById('bookGenre').value,
        availableCopies: parseInt(document.getElementById('bookAvailableCopies').value),
        totalCopies: parseInt(document.getElementById('bookTotalCopies').value)
    };
    
    try {
        const response = await fetch(bookId ? `${BOOKS_API}/${bookId}` : BOOKS_API, {
            method: bookId ? 'PUT' : 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(book)
        });
        
        if (response.ok) {
            bookModal.hide();
            loadBooks();
            showToast(`Book ${bookId ? 'updated' : 'added'} successfully`, 'success');
        } else {
            const errorText = await response.text();
            console.error('Operation failed:', errorText);
            showToast(`Operation failed: ${response.status}`, 'error');
        }
    } catch (error) {
        console.error('Book operation failed:', error);
        showToast('Operation failed', 'error');
    }
}

async function submitMember() {
    if (!validateForm('memberForm')) {
        return;
    }
    
    const memberId = document.getElementById('memberId').value;
    const member = {
        firstName: document.getElementById('memberFirstName').value,
        lastName: document.getElementById('memberLastName').value,
        email: document.getElementById('memberEmail').value,
        phoneNumber: document.getElementById('memberPhone').value,
        address: document.getElementById('memberAddress').value,
        membershipDate: document.getElementById('membershipDate').value,
        status: document.getElementById('memberStatus').value
    };
    
    try {
        const response = await fetch(memberId ? `${MEMBERS_API}/${memberId}` : MEMBERS_API, {
            method: memberId ? 'PUT' : 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(member)
        });
        
        if (response.ok) {
            memberModal.hide();
            loadMembers();
            showToast(`Member ${memberId ? 'updated' : 'added'} successfully`, 'success');
        } else {
            const errorText = await response.text();
            console.error('Operation failed:', errorText);
            showToast(`Operation failed: ${response.status}`, 'error');
        }
    } catch (error) {
        console.error('Member operation failed:', error);
        showToast('Operation failed', 'error');
    }
}

async function submitBorrowing() {
    if (!validateForm('borrowingForm')) {
        return;
    }
    
    const borrowingId = document.getElementById('borrowingId').value;
    
    // For new borrowings
    if (!borrowingId) {
        const borrowing = {
            bookId: parseInt(document.getElementById('borrowingBook').value),
            memberId: parseInt(document.getElementById('borrowingMember').value),
            borrowDate: document.getElementById('borrowingDate').value
        };
        
        try {
            const response = await fetch(BORROWINGS_API, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify(borrowing)
            });
            
            if (response.ok) {
                borrowingModal.hide();
                loadBorrowings();
                showToast('Borrowing added successfully', 'success');
            } else {
                const errorText = await response.text();
                console.error('Operation failed:', errorText);
                showToast(`Operation failed: ${response.status}`, 'error');
            }
        } catch (error) {
            console.error('Borrowing operation failed:', error);
            showToast('Operation failed', 'error');
        }
    } 
    // For existing borrowings (returning a book)
    else {
        const status = document.getElementById('borrowingStatus').value;
        
        // If status is changed to "Returned", use the return endpoint
        if (status === 'Returned') {
            try {
                const response = await fetch(`${BORROWINGS_API}/${borrowingId}/return`, {
                    method: 'PUT',
                    headers: getAuthHeaders()
                });
                
                if (response.ok) {
                    borrowingModal.hide();
                    loadBorrowings();
                    showToast('Book returned successfully', 'success');
                } else {
                    const errorText = await response.text();
                    console.error('Return operation failed:', errorText);
                    showToast(`Return failed: ${response.status}`, 'error');
                }
            } catch (error) {
                console.error('Return operation failed:', error);
                showToast('Return operation failed', 'error');
            }
        } else {
            // We can't update other borrowing statuses directly
            showToast('Cannot update borrowing. Use Return option to return a book.', 'warning');
            borrowingModal.hide();
        }
    }
}

async function searchBooks() {
    const title = document.getElementById('searchTitle').value;
    const author = document.getElementById('searchAuthor').value;
    const isbn = document.getElementById('searchIsbn').value;
    
    try {
        const queryParams = new URLSearchParams();
        if (title) queryParams.append('title', title);
        if (author) queryParams.append('author', author);
        if (isbn) queryParams.append('isbn', isbn);
        
        const response = await fetch(`${BOOKS_API}/search?${queryParams}`, {
            headers: getAuthHeaders()
        });
        
        if (!response.ok) throw new Error('Failed to search books');
        const books = await response.json();
        renderBooksTable(books);
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('searchModal'));
        modal.hide();
        
        showToast('Search completed', 'success');
    } catch (error) {
        console.error('Failed to search books:', error);
        showToast('Failed to search books', 'error');
    }
}

async function deleteBook(id) {
    if (confirm('Are you sure you want to delete this book?')) {
        try {
            const response = await fetch(`${BOOKS_API}/${id}`, {
                method: 'DELETE',
                headers: getAuthHeaders()
            });
            
            if (response.ok) {
                loadBooks();
                showToast('Book deleted successfully', 'success');
            } else {
                showToast('Deletion failed', 'error');
            }
        } catch (error) {
            console.error('Deletion failed:', error);
            showToast('Deletion failed', 'error');
        }
    }
}

async function deleteMember(id) {
    if (confirm('Are you sure you want to delete this member?')) {
        try {
            // First check if this member has any active borrowings
            const borrowingsResponse = await fetch(`${BORROWINGS_API}`, {
                headers: getAuthHeaders()
            });
            
            if (!borrowingsResponse.ok) throw new Error('Failed to check borrowings');
            
            const borrowingsData = await borrowingsResponse.json();
            const borrowings = borrowingsData.borrowingList || borrowingsData;
            
            // Check if member has active borrowings
            const activeBorrowings = borrowings.filter(b => 
                b.memberId == id && (b.status === 'Borrowed' || b.status === 'Overdue'));
            
            if (activeBorrowings.length > 0) {
                showToast('Cannot delete member with active borrowings. Please return all books first.', 'warning');
                return;
            }
            
            const response = await fetch(`${MEMBERS_API}/${id}`, {
                method: 'DELETE',
                headers: getAuthHeaders()
            });
            
            if (response.ok) {
                loadMembers();
                showToast('Member deleted successfully', 'success');
            } else {
                showToast('Deletion failed', 'error');
            }
        } catch (error) {
            console.error('Deletion failed:', error);
            showToast('Deletion failed', 'error');
        }
    }
}

async function deleteBorrowing(id) {
    if (confirm('Are you sure you want to delete this borrowing?')) {
        try {
            const response = await fetch(`${BORROWINGS_API}/${id}`, {
                method: 'DELETE',
                headers: getAuthHeaders()
            });
            
            if (response.ok) {
                loadBorrowings();
                showToast('Borrowing deleted successfully', 'success');
            } else {
                showToast('Deletion failed', 'error');
            }
        } catch (error) {
            console.error('Deletion failed:', error);
            showToast('Deletion failed', 'error');
        }
    }
}

async function editBook(id) {
    try {
        const response = await fetch(`${BOOKS_API}/${id}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to load book');
        const book = await response.json();
        
        document.getElementById('bookId').value = book.id;
        document.getElementById('bookTitle').value = book.title;
        document.getElementById('bookAuthor').value = book.author;
        document.getElementById('bookIsbn').value = book.isbn;
        document.getElementById('bookPublisher').value = book.publisher || '';
        document.getElementById('bookPublicationYear').value = book.publicationYear || new Date().getFullYear();
        document.getElementById('bookGenre').value = book.genre || '';
        document.getElementById('bookAvailableCopies').value = book.availableCopies || 0;
        document.getElementById('bookTotalCopies').value = book.totalCopies || 1;
        
        const modal = new bootstrap.Modal(document.getElementById('bookModal'));
        modal.show();
    } catch (error) {
        console.error('Failed to load book:', error);
        showToast('Failed to load book', 'error');
    }
}

async function editMember(id) {
    try {
        const response = await fetch(`${MEMBERS_API}/${id}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to load member');
        const member = await response.json();
        
        document.getElementById('memberId').value = member.id;
        document.getElementById('memberFirstName').value = member.firstName || '';
        document.getElementById('memberLastName').value = member.lastName || '';
        document.getElementById('memberEmail').value = member.email || '';
        document.getElementById('memberPhone').value = member.phoneNumber || '';
        document.getElementById('memberAddress').value = member.address || '';
        
        // Format date if it exists, otherwise use today's date
        const membershipDate = member.membershipDate ? 
            new Date(member.membershipDate).toISOString().split('T')[0] : 
            new Date().toISOString().split('T')[0];
        document.getElementById('membershipDate').value = membershipDate;
        document.getElementById('memberStatus').value = member.status || 'Active';
        
        const modal = new bootstrap.Modal(document.getElementById('memberModal'));
        modal.show();
    } catch (error) {
        console.error('Failed to load member:', error);
        showToast('Failed to load member', 'error');
    }
}

async function editBorrowing(id) {
    try {
        const response = await fetch(`${BORROWINGS_API}/${id}`, {
            headers: getAuthHeaders()
        });
        if (!response.ok) throw new Error('Failed to load borrowing');
        const borrowing = await response.json();
        
        document.getElementById('borrowingId').value = borrowing.id;
        
        // Load books and members, then set the borrowing details
        Promise.all([
            fetch(BOOKS_API, { headers: getAuthHeaders() }).then(r => r.json()),
            fetch(MEMBERS_API, { headers: getAuthHeaders() }).then(r => r.json())
        ]).then(([books, membersData]) => {
            // Ensure books is an array
            const booksArray = Array.isArray(books) ? books : [];
            
            // Ensure members is an array - extract from memberList property 
            let members = [];
            if (membersData && typeof membersData === 'object') {
                if (Array.isArray(membersData)) {
                    members = membersData;
                } else if (Array.isArray(membersData.memberList)) {
                    members = membersData.memberList;
                }
            }
            
            console.log('Members for dropdown in edit mode:', members);
            
            const bookSelect = document.getElementById('borrowingBook');
            const memberSelect = document.getElementById('borrowingMember');
            
            // Clear existing options
            bookSelect.innerHTML = '';
            memberSelect.innerHTML = '';
            
            // Add book options
            booksArray.forEach(book => {
                const option = document.createElement('option');
                option.value = book.id;
                option.textContent = book.title;
                bookSelect.appendChild(option);
            });
            
            // Add member options
            members.forEach(member => {
                const option = document.createElement('option');
                option.value = member.id;
                option.textContent = `${member.firstName || ''} ${member.lastName || ''}`.trim() || 'Member ' + member.id;
                memberSelect.appendChild(option);
            });
            
            // Set selected values
            bookSelect.value = borrowing.bookId;
            memberSelect.value = borrowing.memberId;
            
            // Set dates and status
            document.getElementById('borrowingDate').value = borrowing.borrowDate.split('T')[0];
            document.getElementById('borrowingStatus').value = borrowing.status || 'Borrowed';
            
            // Handle return date field visibility
            const returnDateGroup = document.getElementById('returnDateGroup');
            if (borrowing.status === 'Returned') {
                returnDateGroup.style.display = 'block';
                document.getElementById('returnDate').required = true;
                document.getElementById('returnDate').value = borrowing.returnDate ? 
                    borrowing.returnDate.split('T')[0] : 
                    new Date().toISOString().split('T')[0];
            } else {
                returnDateGroup.style.display = 'none';
                document.getElementById('returnDate').required = false;
            }
            
            borrowingModal.show();
        }).catch(error => {
            console.error('Failed to load dropdown data:', error);
            showToast('Failed to load dropdown data for borrowing', 'error');
        });
    } catch (error) {
        console.error('Failed to load borrowing:', error);
        showToast('Failed to load borrowing', 'error');
    }
}

// Report Functions
async function generateReport() {
    try {
        showToast('Generating report...', 'info');
        
        const reportContent = document.getElementById('reportContent');
        reportContent.innerHTML = '<div class="text-center"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div><p class="mt-3">Generating report...</p></div>';
        
        const response = await fetch(`${API_BASE_URL}/api/books/report`, {
            headers: getAuthHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`Failed to generate report: ${response.status}`);
        }
        
        const htmlContent = await response.text();
        reportContent.innerHTML = htmlContent;
        
        showToast('Report generated successfully', 'success');
    } catch (error) {
        console.error('Failed to generate report:', error);
        document.getElementById('reportContent').innerHTML = `
            <div class="alert alert-danger">
                Failed to generate report: ${error.message}
                <button class="btn btn-sm btn-outline-danger mt-3" onclick="generateReport()">Try Again</button>
            </div>
        `;
        showToast('Failed to generate report', 'error');
    }
} 