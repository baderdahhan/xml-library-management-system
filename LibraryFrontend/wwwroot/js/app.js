// Global State
let isAuthenticated = false;
let currentUser = null;
let currentRole = null;

// API URLs
const API_BASE_URL = 'https://localhost:5004';
const AUTH_API = `${API_BASE_URL}/api/auth`;
const BOOKS_API = `${API_BASE_URL}/api/books`;
const MEMBERS_API = `${API_BASE_URL}/api/members`;
const BORROWINGS_API = `${API_BASE_URL}/api/borrowings`;

// Initialize Bootstrap Modals
const loginModal = new bootstrap.Modal(document.getElementById('loginModal'));
const bookModal = new bootstrap.Modal(document.getElementById('bookModal'));
const memberModal = new bootstrap.Modal(document.getElementById('memberModal'));
const borrowingModal = new bootstrap.Modal(document.getElementById('borrowingModal'));
const searchModal = new bootstrap.Modal(document.getElementById('searchModal'));

// Event Listeners

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.nav-link').forEach(link => {
        link.addEventListener('click', e => {
            e.preventDefault();
            showSection(e.target.dataset.section);
        });
    });

    document.getElementById('logoutBtn').addEventListener('click', logout);
    document.getElementById('loginForm').addEventListener('submit', handleLogin);

    ['bookForm', 'memberForm', 'borrowingForm', 'searchForm'].forEach(formId => {
        document.getElementById(formId).addEventListener('submit', e => e.preventDefault());
    });

    isAuthenticated = false;
    localStorage.removeItem('token');
    updateUI();
    loginModal.show();

    const borrowingStatus = document.getElementById('borrowingStatus');
    if (borrowingStatus) {
        borrowingStatus.addEventListener('change', handleBorrowingStatusChange);
    }
});

// Authentication

async function handleLogin(e) {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    try {
        const res = await fetch(`${AUTH_API}/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (res.ok) {
            const data = await res.json();
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
    } catch (err) {
        console.error('Login failed:', err);
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

function updateUI() {
    document.querySelectorAll('.nav-link').forEach(link => link.style.display = isAuthenticated ? 'block' : 'none');
    document.getElementById('logoutBtn').style.display = isAuthenticated ? 'block' : 'none';
}

function showSection(section) {
    document.querySelectorAll('.content-section').forEach(s => s.style.display = 'none');
    document.getElementById(`${section}Section`).style.display = 'block';
}

// Load Data

async function loadData() {
    await Promise.all([loadBooks(), loadMembers(), loadBorrowings()]);
}

async function loadBooks() {
    try {
        const res = await fetch(BOOKS_API, { headers: getAuthHeaders() });
        if (res.ok) {
            const books = await res.json();
            renderBooksTable(books);
        }
    } catch (err) {
        console.error('Failed to load books:', err);
        showToast('Failed to load books', 'error');
    }
}

async function loadMembers() {
    try {
        const res = await fetch(MEMBERS_API, { headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to load members');
        const data = await res.json();
        const members = data.memberList || [];
        if (!Array.isArray(members)) throw new Error('Invalid members format');
        renderMembersTable(members);
    } catch (err) {
        console.error('Failed to load members:', err);
        showToast('Failed to load members', 'error');
    }
}

async function loadBorrowings() {
    try {
        const res = await fetch(BORROWINGS_API, { headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to load borrowings');
        const data = await res.json();
        const borrowings = data.borrowingList || data;
        if (!Array.isArray(borrowings)) throw new Error('Invalid borrowings format');
        renderBorrowingsTable(borrowings);
    } catch (err) {
        console.error('Failed to load borrowings:', err);
        showToast('Failed to load borrowings', 'error');
    }
}

// Rendering Tables

function renderBooksTable(books) {
    const tbody = document.getElementById('booksTableBody');
    tbody.innerHTML = books.length ? books.map(book => `
        <tr>
            <td>${book.id}</td>
            <td>${book.title}</td>
            <td>${book.author}</td>
            <td>${book.isbn}</td>     
            <td><span class="badge ${getStatusBadgeClass(book.status)}">${book.status}</span></td>
            <td>
                <button class="btn btn-sm btn-primary me-1" onclick="editBook(${book.id})"><i class="fas fa-edit"></i></button>
                <button class="btn btn-sm btn-danger" onclick="deleteBook(${book.id})"><i class="fas fa-trash"></i></button>
            </td>
        </tr>
    `).join('') : '<tr><td colspan="6">No books found</td></tr>';
}

function renderMembersTable(members) {
    const tbody = document.getElementById('membersTableBody');
    tbody.innerHTML = members.length ? members.map(member => `
        <tr>
            <td>${member.id}</td>
            <td>${member.firstName || ''} ${member.lastName || ''}</td>
            <td>${member.email || ''}</td>
            <td>${member.phoneNumber || ''}</td>
            <td>
                <button class="btn btn-sm btn-primary me-1" onclick="editMember(${member.id})"><i class="fas fa-edit"></i></button>
                <button class="btn btn-sm btn-danger" onclick="deleteMember(${member.id})"><i class="fas fa-trash"></i></button>
            </td>
        </tr>
    `).join('') : '<tr><td colspan="5">No members found</td></tr>';
}

function renderBorrowingsTable(borrowings) {
    const tbody = document.getElementById('borrowingsTableBody');
    tbody.innerHTML = borrowings.length ? borrowings.map(b => `
        <tr>
            <td>${b.id}</td>
            <td>${b.book?.title || b.bookTitle || 'N/A'}</td>
            <td>${b.member?.firstName || b.memberName || 'N/A'}</td>
            <td>${formatDate(b.borrowDate)}</td>
            <td>${b.status === 'Returned' ? formatDate(b.returnDate) : calculateDueDate(b)}</td>
            <td><span class="badge ${getStatusBadgeClass(b.status)}">${b.status}</span></td>
            <td><button class="btn btn-sm btn-primary" onclick="editBorrowing(${b.id})"><i class="fas fa-edit"></i></button></td>
        </tr>
    `).join('') : '<tr><td colspan="7">No borrowings found</td></tr>';
}

// Helpers

function getAuthHeaders() {
    return { 'Authorization': `Bearer ${localStorage.getItem('token')}`, 'Content-Type': 'application/json' };
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString();
}

function calculateDueDate(b) {
    if (b.status === 'Borrowed' || b.status === 'Overdue') {
        const borrowDate = new Date(b.borrowDate);
        borrowDate.setDate(borrowDate.getDate() + 14);
        return `Due: ${formatDate(borrowDate)}`;
    }
    return 'N/A';
}

function getStatusBadgeClass(status) {
    const s = status.toLowerCase();
    if (['available', 'active'].includes(s)) return 'badge-success';
    if (['borrowed', 'pending'].includes(s)) return 'badge-warning';
    if (['overdue', 'inactive'].includes(s)) return 'badge-danger';
    return 'badge-secondary';
}

function showToast(msg, type = 'info') {
    const container = document.querySelector('.toast-container');
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.role = 'alert';
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${msg}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>`;
    container.appendChild(toast);
    new bootstrap.Toast(toast).show();
    toast.addEventListener('hidden.bs.toast', () => toast.remove());
}

function handleBorrowingStatusChange() {
    const status = this.value;
    const returnDateGroup = document.getElementById('returnDateGroup');
    if (status === 'Returned') {
        returnDateGroup.style.display = 'block';
        document.getElementById('returnDate').required = true;
        document.getElementById('returnDate').value = new Date().toISOString().split('T')[0];
    } else {
        returnDateGroup.style.display = 'none';
        document.getElementById('returnDate').required = false;
    }
}

// --- CRUD and Modal Functions ---
// BOOKS CRUD
function addBook() {
    document.getElementById('bookForm').reset();
    document.getElementById('bookId').value = '';
    bookModal.show();
}
async function editBook(id) {
    try {
        const res = await fetch(`${BOOKS_API}/${id}`, { headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to fetch book');
        const book = await res.json();
        document.getElementById('bookId').value = book.id;
        document.getElementById('bookTitle').value = book.title;
        document.getElementById('bookAuthor').value = book.author;
        document.getElementById('bookIsbn').value = book.isbn;
        document.getElementById('bookPublisher').value = book.publisher || '';
        document.getElementById('bookPublicationYear').value = book.publicationYear || '';
        document.getElementById('bookGenre').value = book.genre || '';
        document.getElementById('bookAvailableCopies').value = book.availableCopies || 0;
        document.getElementById('bookTotalCopies').value = book.totalCopies || 1;
        bookModal.show();
    } catch (err) {
        showToast('Failed to load book for editing', 'error');
    }
}
async function deleteBook(id) {
    if (!confirm('Are you sure you want to delete this book?')) return;
    try {
        const res = await fetch(`${BOOKS_API}/${id}`, { method: 'DELETE', headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to delete book');
        showToast('Book deleted', 'success');
        await loadBooks();
    } catch (err) {
        showToast('Failed to delete book', 'error');
    }
}
function showSearchModal() {
    searchModal.show();
}
function showBookModal() {
    // Clear form if adding new
    if (!document.getElementById('bookId').value) {
        document.getElementById('bookForm').reset();
    }
    bookModal.show();
}
async function submitBook() {
    const id = document.getElementById('bookId').value;
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
        let res;
        if (id) {
            res = await fetch(`${BOOKS_API}/${id}`, {
                method: 'PUT',
                headers: getAuthHeaders(),
                body: JSON.stringify({ ...book, id: parseInt(id) })
            });
        } else {
            res = await fetch(BOOKS_API, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify(book)
            });
        }
        if (!res.ok) throw new Error('Failed to save book');
        showToast('Book saved', 'success');
        bookModal.hide();
        await loadBooks();
    } catch (err) {
        showToast('Failed to save book', 'error');
    }
}
async function searchBooks() {
    const title = document.getElementById('searchTitle').value;
    const author = document.getElementById('searchAuthor').value;
    const isbn = document.getElementById('searchIsbn').value;
    let url = `${BOOKS_API}/search?`;
    if (title) url += `title=${encodeURIComponent(title)}&`;
    if (author) url += `author=${encodeURIComponent(author)}&`;
    if (isbn) url += `isbn=${encodeURIComponent(isbn)}&`;
    try {
        const res = await fetch(url, { headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to search books');
        const books = await res.json();
        renderBooksTable(books);
        searchModal.hide();
    } catch (err) {
        showToast('Failed to search books', 'error');
    }
}

// MEMBERS CRUD
function addMember() {
    document.getElementById('memberForm').reset();
    document.getElementById('memberId').value = '';
    memberModal.show();
}
async function editMember(id) {
    try {
        const res = await fetch(`${MEMBERS_API}/${id}`, { headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to fetch member');
        const member = await res.json();
        document.getElementById('memberId').value = member.id;
        document.getElementById('memberFirstName').value = member.firstName || '';
        document.getElementById('memberLastName').value = member.lastName || '';
        document.getElementById('memberEmail').value = member.email || '';
        document.getElementById('memberPhone').value = member.phoneNumber || '';
        document.getElementById('memberAddress').value = member.address || '';
        document.getElementById('membershipDate').value = member.membershipDate ? member.membershipDate.split('T')[0] : '';
        document.getElementById('memberStatus').value = member.status || 'Active';
        memberModal.show();
    } catch (err) {
        showToast('Failed to load member for editing', 'error');
    }
}
async function deleteMember(id) {
    if (!confirm('Are you sure you want to delete this member?')) return;
    try {
        const res = await fetch(`${MEMBERS_API}/${id}`, { method: 'DELETE', headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to delete member');
        showToast('Member deleted', 'success');
        await loadMembers();
    } catch (err) {
        showToast('Failed to delete member', 'error');
    }
}
function showMemberModal() {
    // Clear form if adding new
    if (!document.getElementById('memberId').value) {
        document.getElementById('memberForm').reset();
    }
    memberModal.show();
}
async function submitMember() {
    const id = document.getElementById('memberId').value;
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
        let res;
        if (id) {
            res = await fetch(`${MEMBERS_API}/${id}`, {
                method: 'PUT',
                headers: getAuthHeaders(),
                body: JSON.stringify({ ...member, id: parseInt(id) })
            });
        } else {
            res = await fetch(MEMBERS_API, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify(member)
            });
        }
        if (!res.ok) throw new Error('Failed to save member');
        showToast('Member saved', 'success');
        memberModal.hide();
        await loadMembers();
    } catch (err) {
        showToast('Failed to save member', 'error');
    }
}

// BORROWINGS CRUD
async function addBorrowing() {
    await populateBorrowingDropdowns();
    document.getElementById('borrowingForm').reset();
    document.getElementById('borrowingId').value = '';
    document.getElementById('returnDateGroup').style.display = 'none';
    borrowingModal.show();
}
async function editBorrowing(id) {
    try {
        await populateBorrowingDropdowns();
        const res = await fetch(`${BORROWINGS_API}/${id}`, { headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to fetch borrowing');
        const borrowing = await res.json();
        document.getElementById('borrowingId').value = borrowing.id;
        document.getElementById('borrowingBook').value = borrowing.bookId;
        document.getElementById('borrowingMember').value = borrowing.memberId;
        document.getElementById('borrowingDate').value = borrowing.borrowDate ? borrowing.borrowDate.split('T')[0] : '';
        document.getElementById('borrowingStatus').value = borrowing.status || 'Borrowed';
        if (borrowing.status === 'Returned') {
            document.getElementById('returnDateGroup').style.display = 'block';
            document.getElementById('returnDate').value = borrowing.returnDate ? borrowing.returnDate.split('T')[0] : '';
        } else {
            document.getElementById('returnDateGroup').style.display = 'none';
            document.getElementById('returnDate').value = '';
        }
        borrowingModal.show();
    } catch (err) {
        showToast('Failed to load borrowing for editing', 'error');
    }
}

// Populate dropdowns for borrowing modal
async function populateBorrowingDropdowns() {
    // Books
    try {
        const booksRes = await fetch(BOOKS_API, { headers: getAuthHeaders() });
        const books = booksRes.ok ? await booksRes.json() : [];
        const bookSelect = document.getElementById('borrowingBook');
        bookSelect.innerHTML = books.length ? books.map(b => `<option value="${b.id}">${b.id} - ${b.title}</option>`).join('') : '<option value="">No books</option>';
    } catch {
        document.getElementById('borrowingBook').innerHTML = '<option value="">No books</option>';
    }
    // Members
    try {
        const membersRes = await fetch(MEMBERS_API, { headers: getAuthHeaders() });
        const data = membersRes.ok ? await membersRes.json() : { memberList: [] };
        const members = data.memberList || [];
        const memberSelect = document.getElementById('borrowingMember');
        memberSelect.innerHTML = members.length ? members.map(m => `<option value="${m.id}">${m.id} - ${m.firstName} ${m.lastName}</option>`).join('') : '<option value="">No members</option>';
    } catch {
        document.getElementById('borrowingMember').innerHTML = '<option value="">No members</option>';
    }
}

// Update modal openers in window for global access
window.addBook = addBook;
window.editBook = editBook;
window.addMember = addMember;
window.editMember = editMember;
window.addBorrowing = addBorrowing;
window.editBorrowing = editBorrowing;

// REPORTS
async function generateReport() {
    try {
        const res = await fetch(`${BOOKS_API}/report`, { headers: getAuthHeaders() });
        if (!res.ok) throw new Error('Failed to generate report');
        const reportHtml = await res.text();
        document.getElementById('reportContent').innerHTML = reportHtml;
        showToast('Report generated', 'success');
    } catch (err) {
        showToast('Failed to generate report', 'error');
    }
}

async function submitBorrowing() {
    const id = document.getElementById('borrowingId').value;
    if (!id) {
        // ADD: Only send bookId and memberId
        const borrowing = {
            bookId: parseInt(document.getElementById('borrowingBook').value),
            memberId: parseInt(document.getElementById('borrowingMember').value)
        };
        try {
            const res = await fetch(BORROWINGS_API, {
                method: 'POST',
                headers: getAuthHeaders(),
                body: JSON.stringify(borrowing)
            });
            if (!res.ok) throw new Error('Failed to save borrowing');
            showToast('Borrowing saved', 'success');
            borrowingModal.hide();
            await loadBorrowings();
        } catch (err) {
            showToast('Failed to save borrowing', 'error');
        }
    } else {
        // EDIT/RETURN: Keep existing logic
        const borrowing = {
            bookId: parseInt(document.getElementById('borrowingBook').value),
            memberId: parseInt(document.getElementById('borrowingMember').value),
            borrowDate: document.getElementById('borrowingDate').value,
            status: document.getElementById('borrowingStatus').value,
            returnDate: document.getElementById('returnDate').value || null
        };
        try {
            let res;
            // Only support returning a book (PUT /return)
            if (borrowing.status === 'Returned') {
                res = await fetch(`${BORROWINGS_API}/${id}/return`, {
                    method: 'PUT',
                    headers: getAuthHeaders(),
                    body: JSON.stringify(borrowing)
                });
            } else {
                showToast('Only returning is supported for editing borrowings', 'info');
                return;
            }
            if (!res.ok) throw new Error('Failed to save borrowing');
            showToast('Borrowing saved', 'success');
            borrowingModal.hide();
            await loadBorrowings();
        } catch (err) {
            showToast('Failed to save borrowing', 'error');
        }
    }
}