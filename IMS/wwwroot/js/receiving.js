document.addEventListener('DOMContentLoaded', function () {
    const rows = Array.from(document.querySelectorAll('.receiving-row'));
    const searchInput = document.getElementById('receiptSearch');
    const supplierFilter = document.getElementById('supplierFilter');
    const statusFilter = document.getElementById('statusFilter');
    const timeFilter = document.getElementById('timeFilter');

    function applyFilters() {
        const search = (searchInput?.value || '').trim().toLowerCase();
        const supplier = supplierFilter?.value || 'All Suppliers';
        const status = statusFilter?.value || 'All Statuses';
        const time = timeFilter?.value || 'Today';

        const todayStr = new Date().toISOString().split('T')[0];

        rows.forEach(row => {
            const matchesSearch = !search || (row.dataset.search || '').includes(search);
            const matchesSupplier = supplier === 'All Suppliers' || row.dataset.supplier === supplier;
            const matchesStatus = status === 'All Statuses' || row.dataset.status === status;
            
            let matchesTime = true;
            if (time === 'Today' && (row.dataset.status === 'QC Required' || row.dataset.status === 'Accepted' || row.dataset.status === 'Rejected')) {
                matchesTime = (row.dataset.date === todayStr);
            }

            row.hidden = !(matchesSearch && matchesSupplier && matchesStatus && matchesTime);
        });
    }

    [searchInput, supplierFilter, statusFilter, timeFilter].forEach(control => {
        control?.addEventListener(control.tagName === 'INPUT' ? 'input' : 'change', applyFilters);
    });

    applyFilters();

    document.querySelectorAll('.receive-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const remaining = Number(this.dataset.remaining || 0);
            const receivedInput = document.getElementById('input-received');
            const damagedInput = document.getElementById('input-damaged');
            const rejectedInput = document.getElementById('input-rejected');

            document.getElementById('receive-poitemid').value = this.dataset.poitemid;
            document.getElementById('disp-po').innerText = this.dataset.po;
            document.getElementById('disp-supplier').innerText = this.dataset.supplier;
            document.getElementById('disp-product').innerText = this.dataset.product;
            document.getElementById('disp-sku').innerText = `SKU: ${this.dataset.sku}`;
            document.getElementById('disp-ordered').innerText = Number(this.dataset.ordered || 0).toLocaleString();
            document.getElementById('disp-received').innerText = Number(this.dataset.received || 0).toLocaleString();
            document.getElementById('disp-remaining').innerText = remaining.toLocaleString();

            receivedInput.value = remaining;
            receivedInput.max = remaining;
            damagedInput.value = 0;
            rejectedInput.value = 0;
        });
    });

    const receiveForm = document.querySelector('#receiveModal form');
    receiveForm?.addEventListener('submit', function (event) {
        const received = Number(document.getElementById('input-received')?.value || 0);
        const damaged = Number(document.getElementById('input-damaged')?.value || 0);
        const rejected = Number(document.getElementById('input-rejected')?.value || 0);

        if (damaged + rejected > received) {
            event.preventDefault();
            alert('Damaged and rejected quantity cannot exceed received quantity.');
        }
    });
});
