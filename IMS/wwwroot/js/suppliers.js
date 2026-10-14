document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('supplierSearch');
    const typeFilter = document.getElementById('supplierTypeFilter');
    const statusFilter = document.getElementById('supplierStatusFilter');
    const rows = Array.from(document.querySelectorAll('.supplier-row'));

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
            document.getElementById('edit-lead').value = button.dataset.lead || '0';
            document.getElementById('edit-payment').value = button.dataset.payment || '';
            document.getElementById('edit-status').value = button.dataset.status || '';
            document.getElementById('edit-returnemail').value = button.dataset.returnemail || '';
            document.getElementById('edit-carrier').value = button.dataset.preferredcarrier || '';
            document.getElementById('edit-returnpolicy').value = button.dataset.returnpolicy || '';
        });
    });

    const avlRows = Array.from(document.querySelectorAll('.avl-row'));
    const avlEmptyRow = document.querySelector('.avl-empty-row');

    document.querySelectorAll('.avl-btn').forEach((button) => {
        button.addEventListener('click', () => {
            const supplierId = button.dataset.supplierId || '';
            document.getElementById('avl-supplier-id').value = supplierId;
            document.getElementById('supplierProductsSubtitle').textContent = button.dataset.supplierName || 'Supplier approved vendor list';

            let visibleCount = 0;
            avlRows.forEach((row) => {
                const visible = row.dataset.supplierId === supplierId;
                row.hidden = !visible;
                if (visible) visibleCount += 1;
            });

            if (avlEmptyRow) avlEmptyRow.hidden = visibleCount > 0;
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
