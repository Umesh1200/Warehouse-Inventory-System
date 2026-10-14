document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('customerSearch');
    const typeFilter = document.getElementById('customerTypeFilter');
    const statusFilter = document.getElementById('customerStatusFilter');
    const rows = Array.from(document.querySelectorAll('.customer-row'));

    const applyFilters = () => {
        const query = (searchInput?.value || '').trim().toLowerCase();
        const type = typeFilter?.value || '';
        const status = statusFilter?.value || '';

        rows.forEach((row) => {
            const matchesQuery = !query || (row.dataset.search || '').toLowerCase().includes(query);
            const matchesType = !type || row.dataset.type === type;
            const matchesStatus = !status || row.dataset.status === status;
            row.hidden = !(matchesQuery && matchesType && matchesStatus);
        });
    };

    searchInput?.addEventListener('input', applyFilters);
    typeFilter?.addEventListener('change', applyFilters);
    statusFilter?.addEventListener('change', applyFilters);

    document.querySelectorAll('.edit-btn').forEach((button) => {
        button.addEventListener('click', () => {
            document.getElementById('edit-id').value = button.dataset.id || '';
            document.getElementById('edit-code').value = button.dataset.code || '';
            document.getElementById('edit-name').value = button.dataset.name || '';
            document.getElementById('edit-contact').value = button.dataset.contact || '';
            document.getElementById('edit-phone').value = button.dataset.phone || '';
            document.getElementById('edit-email').value = button.dataset.email || '';
            document.getElementById('edit-type').value = button.dataset.type || '';
            document.getElementById('edit-address').value = button.dataset.address || '';
            document.getElementById('edit-status').value = button.dataset.status || '';
        });
    });

    document.querySelectorAll('form[data-confirm-message]').forEach((form) => {
        form.addEventListener('submit', (event) => {
            if (!window.confirm(form.dataset.confirmMessage || 'Confirm this action?')) {
                event.preventDefault();
            }
        });
    });
});
