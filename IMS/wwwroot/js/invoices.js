document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('invoiceSearch');
    const statusFilter = document.getElementById('invoiceStatusFilter');
    const matchFilter = document.getElementById('invoiceMatchFilter');
    const supplierFilter = document.getElementById('invoiceSupplierFilter');
    const resetBtn = document.getElementById('invoiceResetBtn');
    const rows = document.querySelectorAll('.invoice-row');
    const countLabel = document.getElementById('invoiceCountLabel');
    const emptyRow = document.getElementById('invoiceEmptyRow');

    const filterInvoices = () => {
        const query = (searchInput?.value || '').trim().toLowerCase();
        const selectedStatus = statusFilter?.value || 'All Statuses';
        const selectedMatch = matchFilter?.value || 'All Match Statuses';
        const selectedSupplier = supplierFilter?.value || 'All Suppliers';

        let visibleCount = 0;

        rows.forEach((row) => {
            const searchData = (row.dataset.search || '').toLowerCase();
            const status = row.dataset.status || '';
            const match = row.dataset.match || '';
            const supplier = row.dataset.supplier || '';

            const matchesQuery = !query || searchData.includes(query);
            const matchesStatus = selectedStatus === 'All Statuses' || status === selectedStatus;
            const matchesMatch = selectedMatch === 'All Match Statuses' || match === selectedMatch;
            const matchesSupplier = selectedSupplier === 'All Suppliers' || supplier === selectedSupplier;

            if (matchesQuery && matchesStatus && matchesMatch && matchesSupplier) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        if (countLabel) {
            countLabel.textContent = `Showing ${visibleCount} of ${rows.length} invoices`;
        }

        if (emptyRow) {
            emptyRow.style.display = (visibleCount === 0 && rows.length > 0) ? '' : 'none';
        }
    };

    if (searchInput) searchInput.addEventListener('input', filterInvoices);
    if (statusFilter) statusFilter.addEventListener('change', filterInvoices);
    if (matchFilter) matchFilter.addEventListener('change', filterInvoices);
    if (supplierFilter) supplierFilter.addEventListener('change', filterInvoices);

    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            if (searchInput) searchInput.value = '';
            if (statusFilter) statusFilter.value = 'All Statuses';
            if (matchFilter) matchFilter.value = 'All Match Statuses';
            if (supplierFilter) supplierFilter.value = 'All Suppliers';
            filterInvoices();
        });
    }

    // Payment modal binding
    document.querySelectorAll('.pay-button').forEach((btn) => {
        btn.addEventListener('click', () => {
            const balance = parseFloat(btn.dataset.balance) || 0;
            const id = btn.dataset.id;
            const number = btn.dataset.number;

            const idInput = document.getElementById('payment-invoice-id');
            const amountInput = document.getElementById('payment-amount');
            const contextText = document.getElementById('payment-context');

            if (idInput) idInput.value = id;
            if (amountInput) {
                amountInput.value = balance.toFixed(2);
                amountInput.max = balance.toFixed(2);
            }
            if (contextText) {
                contextText.innerHTML = `<strong>Invoice:</strong> ${number} · <strong>Outstanding Balance:</strong> Rs. ${balance.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
            }
        });
    });

    filterInvoices();
});
