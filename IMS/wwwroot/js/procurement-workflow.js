document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('workflowSearch');
    const statusFilter = document.getElementById('workflowStatusFilter');
    const supplierFilter = document.getElementById('workflowSupplierFilter');
    const responseFilter = document.getElementById('workflowResponseFilter');
    const resetBtn = document.getElementById('workflowResetBtn');
    const orderItems = document.querySelectorAll('.order-option');
    const countLabel = document.getElementById('orderCountLabel');
    const emptyNotice = document.getElementById('sidebarEmptyNotice');

    const filterOrders = () => {
        const query = (searchInput?.value || '').trim().toLowerCase();
        const selectedStatus = statusFilter?.value || 'All PO Statuses';
        const selectedSupplier = supplierFilter?.value || 'All Suppliers';
        const selectedResponse = responseFilter?.value || 'All Responses';

        let visibleCount = 0;

        orderItems.forEach((item) => {
            const searchData = (item.dataset.search || '').toLowerCase();
            const poStatus = item.dataset.status || '';
            const supplier = item.dataset.supplier || '';
            const response = item.dataset.response || '';

            const matchesQuery = !query || searchData.includes(query);
            const matchesStatus = selectedStatus === 'All PO Statuses' || poStatus === selectedStatus;
            const matchesSupplier = selectedSupplier === 'All Suppliers' || supplier === selectedSupplier;
            const matchesResponse = selectedResponse === 'All Responses' || response === selectedResponse;

            if (matchesQuery && matchesStatus && matchesSupplier && matchesResponse) {
                item.style.display = 'block';
                visibleCount++;
            } else {
                item.style.display = 'none';
            }
        });

        if (countLabel) {
            countLabel.textContent = `Showing ${visibleCount} of ${orderItems.length}`;
        }

        if (emptyNotice) {
            emptyNotice.style.display = visibleCount === 0 ? 'block' : 'none';
        }
    };

    if (searchInput) searchInput.addEventListener('input', filterOrders);
    if (statusFilter) statusFilter.addEventListener('change', filterOrders);
    if (supplierFilter) supplierFilter.addEventListener('change', filterOrders);
    if (responseFilter) responseFilter.addEventListener('change', filterOrders);

    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            if (searchInput) searchInput.value = '';
            if (statusFilter) statusFilter.value = 'All PO Statuses';
            if (supplierFilter) supplierFilter.value = 'All Suppliers';
            if (responseFilter) responseFilter.value = 'All Responses';
            filterOrders();
        });
    }

    // Run initial filter count
    filterOrders();
});
