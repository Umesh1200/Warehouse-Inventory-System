// Users & Access Management Scripts

function openAddUserModal() {
    document.getElementById('addUserModal').style.display = 'flex';
}

function openEditUserModal(id, name, email, phone, roleId, isActive) {
    document.getElementById('editUserID').value = id;
    document.getElementById('editFullName').value = name;
    document.getElementById('editEmail').value = email;
    document.getElementById('editPhone').value = phone;
    document.getElementById('editRoleID').value = roleId;
    document.getElementById('editIsActive').value = isActive.toString();
    
    document.getElementById('editUserModal').style.display = 'flex';
}

function openDeactivateModal(id, name) {
    document.getElementById('deactivateUserID').value = id;
    document.getElementById('deactivateUserName').innerText = name;
    document.getElementById('deactivateModal').style.display = 'flex';
}

function openReactivateModal(id, name) {
    document.getElementById('reactivateUserID').value = id;
    document.getElementById('reactivateUserName').innerText = name;
    document.getElementById('reactivateModal').style.display = 'flex';
}

function closeModal(modalId) {
    document.getElementById(modalId).style.display = 'none';
}

// Close modal when clicking outside
window.onclick = function(event) {
    if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
    }
}

// Live Search and Filter
function filterUsers() {
    const searchText = document.getElementById('userSearch').value.toLowerCase();
    const roleFilter = document.getElementById('roleFilter').value;
    const statusFilter = document.getElementById('statusFilter').value;
    
    const rows = document.querySelectorAll('.user-row');
    
    rows.forEach(row => {
        const text = row.innerText.toLowerCase();
        const role = row.getAttribute('data-role');
        const status = row.getAttribute('data-status');
        
        const matchesSearch = text.includes(searchText);
        const matchesRole = !roleFilter || role === roleFilter;
        const matchesStatus = !statusFilter || status === statusFilter;
        
        if (matchesSearch && matchesRole && matchesStatus) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}
