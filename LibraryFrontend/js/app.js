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

// Bootstrap modals
let loginModal, bookModal, memberModal, borrowingModal, searchBookModal;

// Initialize the application
document.addEventListener('DOMContentLoaded', () => {
    initializeModals();
    setupEventListeners();
    checkAuthentication();
});

// Initialize Bootstrap modals
function initializeModals() {
    loginModal = new bootstrap.Modal(document.getElementById('loginModal'));
    bookModal = new bootstrap.Modal(document.getElementById('bookModal'));
    memberModal = new bootstrap.Modal(document.getElementById('memberModal'));
    borrowingModal = new bootstrap.Modal(document.getElementById('borrowingModal'));
    searchBookModal = new bootstrap.Modal(document.getElementById('searchBookModal'));
}

// Setup event listeners
function setupEventListeners() {
    // Navigation
    document.querySelectorAll('.nav-link[data-section]').forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            showSection(e.target.dataset.section);
        });
    });

    // Logout
    document.getElementById('logoutBtn').addEventListener('click', logout);

    // Login form
    document.getElementById('loginForm').addEventListener('submit', handleLogin);

    // Book form
    document.getElementById('bookForm').addEventListener('submit', handleBookSubmit);
    document.getElementById('addBookBtn').addEventListener('click', () => showBookModal());
    document.getElementById('searchBookBtn').addEventListener('click', () => searchBookModal.show());

    // Member form
    document.getElementById('memberForm').addEventListener('submit', handleMemberSubmit);
    document.getElementById('addMemberBtn').addEventListener('click', () => showMemberModal());

    // Borrowing form
    document.getElementById('borrowingForm').addEventListener('submit', handleBorrowingSubmit);
    document.getElementById('addBorrowingBtn').addEventListener('click', () => showBorrowingModal());

    // Search form
    document.getElementById('searchBookForm').addEventListener('submit', handleSearchSubmit);
}

// Authentication functions
async function checkAuthentication() {
    const token = localStorage.getItem('token');
    if (!token) {
        showLoginModal();
        return;
    }

    try {
        const response = await fetch(`${AUTH_API}/verify`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (response.ok) {
            const data = await response.json();
            currentUser = data.username;
            currentRole = data.role;
            isAuthenticated = true;
            updateAuthUI();
            showSection('books');
            loadAllData();
        } else {
            throw new Error('Invalid token');
        }
    } catch (error) {
        console.error('Authentication error:', error);
        logout();
        showLoginModal();
    }
}

async function handleLogin(e) {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    try {
        const response = await fetch(`${AUTH_API}/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            const data = await response.json();
            localStorage.setItem('token', data.token);
            currentUser = data.username;
            currentRole = data.role;
            isAuthenticated = true;
            loginModal.hide();
            updateAuthUI();
            showSection('books');
            loadAllData();
            showToast('Login successful', 'success');
        } else {
            throw new Error('Invalid credentials');
        }
    } catch (error) {
        console.error('Login error:', error);
        showToast('Invalid username or password', 'error');
    }
}

function logout() {
    localStorage.removeItem('token');
    isAuthenticated = false;
    currentUser = null;
    currentRole = null;
    showLoginModal();
    showToast('Logged out successfully', 'success');
}

// UI functions
function updateAuthUI() {
    document.getElementById('userInfo').textContent = `Welcome, ${currentUser}`;
    document.querySelectorAll('.nav-link').forEach(link => {
        link.classList.remove('active');
    });
    document.querySelector('.nav-link[data-section="books"]').classList.add('active');
}

function showSection(section) {
    document.querySelectorAll('.content-section').forEach(el => {
        el.classList.add('d-none');
    });
    document.getElementById(`${section}Section`).classList.remove('d-none');
    document.querySelectorAll('.nav-link').forEach(link => {
        link.classList.remove('active');
    });
    document.querySelector(`.nav-link[data-section="${section}"]`).classList.add('active');
}

function showToast(message, type = 'info') {
    const toastContainer = document.querySelector('.toast-container') || createToastContainer();
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
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

function createToastContainer() {
    const container = document.createElement('div');
    container.className = 'toast-container';
    document.body.appendChild(container);
    return container;
}

// Data loading functions
async function loadAllData() {
    try {
        await Promise.all([
            loadBooks(),
            loadMembers(),
            loadBorrowings()
        ]);
    } catch (error) {
        console.error('Error loading data:', error);
        showToast('Error loading data', 'error');
    }
}

async function loadBooks() {
    try {
        const response = await fetch(BOOKS_API, {
            headers: getAuthHeaders()
        });
        const data = await response.json();
        renderBooksTable(data);
    } catch (error) {
        console.error('Error loading books:', error);
        showToast('Error loading books', 'error');
    }
}

async function loadMembers() {
    try {
        const response = await fetch(MEMBERS_API, {
            headers: getAuthHeaders()
        });
        const data = await response.json();
        renderMembersTable(data);
    } catch (error) {
        console.error('Error loading members:', error);
        showToast('Error loading members', 'error');
    }
}

async function loadBorrowings() {
    try {
        const response = await fetch(BORROWINGS_API, {
            headers: getAuthHeaders()
        });
        const data = await response.json();
        renderBorrowingsTable(data);
    } catch (error) {
        console.error('Error loading borrowings:', error);
        showToast('Error loading borrowings', 'error');
    }
}

// Table rendering functions
function renderBooksTable(books) {
    const tbody = document.querySelector('#booksTable tbody');
    tbody.innerHTML = books.map(book => `
        <tr>
            <td>${book.isbn}</td>
            <td>${book.title}</td>
            <td>${book.author}</td>
            <td>${book.publisher}</td>
            <td>${book.publicationYear}</td>
            <td>${book.genre}</td>
            <td>${book.availableCopies}/${book.totalCopies}</td>
            <td class="action-buttons">
                <button class="btn btn-sm btn-primary" onclick="editBook(${book.id})">
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
    const tbody = document.querySelector('#membersTable tbody');
    tbody.innerHTML = members.map(member => `
        <tr>
            <td>${member.id}</td>
            <td>${member.name}</td>
            <td>${member.email}</td>
            <td>${member.phone}</td>
            <td class="action-buttons">
                <button class="btn btn-sm btn-primary" onclick="editMember(${member.id})">
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
    const tbody = document.querySelector('#borrowingsTable tbody');
    tbody.innerHTML = borrowings.map(borrowing => `
        <tr>
            <td>${borrowing.id}</td>
            <td>${borrowing.bookTitle}</td>
            <td>${borrowing.memberName}</td>
            <td>${formatDate(borrowing.borrowDate)}</td>
            <td>${formatDate(borrowing.returnDate)}</td>
            <td>
                <span class="badge ${getStatusBadgeClass(borrowing.status)}">
                    ${borrowing.status}
                </span>
            </td>
            <td class="action-buttons">
                <button class="btn btn-sm btn-primary" onclick="editBorrowing(${borrowing.id})">
                    <i class="fas fa-edit"></i>
                </button>
                <button class="btn btn-sm btn-danger" onclick="deleteBorrowing(${borrowing.id})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
    `).join('');
}

// Helper functions
function getAuthHeaders() {
    const token = localStorage.getItem('token');
    if (!token) throw new Error('Not authenticated');
    return {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    };
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString();
}

function getStatusBadgeClass(status) {
    switch (status.toLowerCase()) {
        case 'active':
            return 'badge-success';
        case 'overdue':
            return 'badge-danger';
        case 'returned':
            return 'badge-warning';
        default:
            return 'badge-secondary';
    }
}

// Form handling functions
async function handleBookSubmit(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const bookData = Object.fromEntries(formData.entries());
    const bookId = document.getElementById('bookId').value;

    try {
        const response = await fetch(BOOKS_API + (bookId ? `/${bookId}` : ''), {
            method: bookId ? 'PUT' : 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(bookData)
        });

        if (response.ok) {
            bookModal.hide();
            loadBooks();
            showToast(`Book ${bookId ? 'updated' : 'added'} successfully`, 'success');
        } else {
            throw new Error('Failed to save book');
        }
    } catch (error) {
        console.error('Error saving book:', error);
        showToast('Error saving book', 'error');
    }
}

async function handleMemberSubmit(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const memberData = Object.fromEntries(formData.entries());
    const memberId = document.getElementById('memberId').value;

    try {
        const response = await fetch(MEMBERS_API + (memberId ? `/${memberId}` : ''), {
            method: memberId ? 'PUT' : 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(memberData)
        });

        if (response.ok) {
            memberModal.hide();
            loadMembers();
            showToast(`Member ${memberId ? 'updated' : 'added'} successfully`, 'success');
        } else {
            throw new Error('Failed to save member');
        }
    } catch (error) {
        console.error('Error saving member:', error);
        showToast('Error saving member', 'error');
    }
}

async function handleBorrowingSubmit(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const borrowingData = Object.fromEntries(formData.entries());
    const borrowingId = document.getElementById('borrowingId').value;

    try {
        const response = await fetch(BORROWINGS_API + (borrowingId ? `/${borrowingId}` : ''), {
            method: borrowingId ? 'PUT' : 'POST',
            headers: getAuthHeaders(),
            body: JSON.stringify(borrowingData)
        });

        if (response.ok) {
            borrowingModal.hide();
            loadBorrowings();
            showToast(`Borrowing ${borrowingId ? 'updated' : 'added'} successfully`, 'success');
        } else {
            throw new Error('Failed to save borrowing');
        }
    } catch (error) {
        console.error('Error saving borrowing:', error);
        showToast('Error saving borrowing', 'error');
    }
}

async function handleSearchSubmit(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const searchParams = new URLSearchParams();

    for (const [key, value] of formData.entries()) {
        if (value) {
            searchParams.append(key.replace('search', '').toLowerCase(), value);
        }
    }

    try {
        const response = await fetch(`${BOOKS_API}/search?${searchParams.toString()}`, {
            headers: getAuthHeaders()
        });
        const data = await response.json();
        renderBooksTable(data);
        searchBookModal.hide();
    } catch (error) {
        console.error('Error searching books:', error);
        showToast('Error searching books', 'error');
    }
}

// Modal show functions
function showBookModal(book = null) {
    const form = document.getElementById('bookForm');
    const title = document.querySelector('#bookModal .modal-title');
    
    form.reset();
    if (book) {
        title.textContent = 'Edit Book';
        document.getElementById('bookId').value = book.id;
        Object.keys(book).forEach(key => {
            const input = form.elements[key];
            if (input) input.value = book[key];
        });
    } else {
        title.textContent = 'Add Book';
        document.getElementById('bookId').value = '';
    }
    
    bookModal.show();
}

function showMemberModal(member = null) {
    const form = document.getElementById('memberForm');
    const title = document.querySelector('#memberModal .modal-title');
    
    form.reset();
    if (member) {
        title.textContent = 'Edit Member';
        document.getElementById('memberId').value = member.id;
        Object.keys(member).forEach(key => {
            const input = form.elements[key];
            if (input) input.value = member[key];
        });
    } else {
        title.textContent = 'Add Member';
        document.getElementById('memberId').value = '';
    }
    
    memberModal.show();
}

function showBorrowingModal(borrowing = null) {
    const form = document.getElementById('borrowingForm');
    const title = document.querySelector('#borrowingModal .modal-title');
    
    form.reset();
    if (borrowing) {
        title.textContent = 'Edit Borrowing';
        document.getElementById('borrowingId').value = borrowing.id;
        Object.keys(borrowing).forEach(key => {
            const input = form.elements[key];
            if (input) input.value = borrowing[key];
        });
    } else {
        title.textContent = 'New Borrowing';
        document.getElementById('borrowingId').value = '';
    }
    
    borrowingModal.show();
}

// CRUD operations
async function editBook(id) {
    try {
        const response = await fetch(`${BOOKS_API}/${id}`, {
            headers: getAuthHeaders()
        });
        const book = await response.json();
        showBookModal(book);
    } catch (error) {
        console.error('Error loading book:', error);
        showToast('Error loading book', 'error');
    }
}

async function deleteBook(id) {
    if (!confirm('Are you sure you want to delete this book?')) return;

    try {
        const response = await fetch(`${BOOKS_API}/${id}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        if (response.ok) {
            loadBooks();
            showToast('Book deleted successfully', 'success');
        } else {
            throw new Error('Failed to delete book');
        }
    } catch (error) {
        console.error('Error deleting book:', error);
        showToast('Error deleting book', 'error');
    }
}

async function editMember(id) {
    try {
        const response = await fetch(`${MEMBERS_API}/${id}`, {
            headers: getAuthHeaders()
        });
        const member = await response.json();
        showMemberModal(member);
    } catch (error) {
        console.error('Error loading member:', error);
        showToast('Error loading member', 'error');
    }
}

async function deleteMember(id) {
    if (!confirm('Are you sure you want to delete this member?')) return;

    try {
        const response = await fetch(`${MEMBERS_API}/${id}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        if (response.ok) {
            loadMembers();
            showToast('Member deleted successfully', 'success');
        } else {
            throw new Error('Failed to delete member');
        }
    } catch (error) {
        console.error('Error deleting member:', error);
        showToast('Error deleting member', 'error');
    }
}

async function editBorrowing(id) {
    try {
        const response = await fetch(`${BORROWINGS_API}/${id}`, {
            headers: getAuthHeaders()
        });
        const borrowing = await response.json();
        showBorrowingModal(borrowing);
    } catch (error) {
        console.error('Error loading borrowing:', error);
        showToast('Error loading borrowing', 'error');
    }
}

async function deleteBorrowing(id) {
    if (!confirm('Are you sure you want to delete this borrowing?')) return;

    try {
        const response = await fetch(`${BORROWINGS_API}/${id}`, {
            method: 'DELETE',
            headers: getAuthHeaders()
        });

        if (response.ok) {
            loadBorrowings();
            showToast('Borrowing deleted successfully', 'success');
        } else {
            throw new Error('Failed to delete borrowing');
        }
    } catch (error) {
        console.error('Error deleting borrowing:', error);
        showToast('Error deleting borrowing', 'error');
    }
} 