document.addEventListener('DOMContentLoaded', function () {
    const rows = Array.from(document.querySelectorAll('.qc-row'));
    const searchInput = document.getElementById('qcSearch');
    const statusFilter = document.getElementById('statusFilter');
    const timeFilter = document.getElementById('timeFilter');
    let receivedQty = 0;

    function applyFilters() {
        const search = (searchInput?.value || '').trim().toLowerCase();
        const status = statusFilter?.value || 'All Statuses';
        const time = timeFilter?.value || 'Today';

        const todayStr = new Date().toISOString().split('T')[0];

        rows.forEach(row => {
            const matchesSearch = !search || (row.dataset.search || '').includes(search);
            const matchesStatus = status === 'All Statuses' || row.dataset.status === status;
            
            let matchesTime = true;
            if (time === 'Today' && (row.dataset.status === 'Accepted' || row.dataset.status === 'Rejected')) {
                matchesTime = (row.dataset.date === todayStr);
            }

            row.hidden = !(matchesSearch && matchesStatus && matchesTime);
        });
    }

    function updateTotal() {
        const accepted = Number(document.getElementById('input-accepted')?.value || 0);
        const damaged = Number(document.getElementById('input-damaged')?.value || 0);
        const rejected = Number(document.getElementById('input-rejected')?.value || 0);
        const total = accepted + damaged + rejected;
        const totalInput = document.getElementById('input-total');

        if (totalInput) {
            totalInput.value = `${total.toLocaleString()} / ${receivedQty.toLocaleString()}`;
            totalInput.classList.toggle('is-invalid', total !== receivedQty);
        }
    }

    [searchInput, statusFilter, timeFilter].forEach(control => {
        control?.addEventListener(control.tagName === 'INPUT' ? 'input' : 'change', applyFilters);
    });

    applyFilters();

    document.querySelectorAll('.qc-submit-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            receivedQty = Number(this.dataset.received || 0);

            const source = this.dataset.source || 'Purchase';
            document.getElementById('qc-receiptid').value = this.dataset.receiptid || '';
            document.getElementById('qc-fgreceiptid').value = this.dataset.fgreceiptid || '';
            document.getElementById('qc-itemsource').value = source;

            const labelPo = document.getElementById('label-po');
            if (labelPo) labelPo.innerText = source === 'Production' ? 'Plan Number' : 'PO Number';

            const labelSupplier = document.getElementById('label-supplier');
            if (labelSupplier) labelSupplier.innerText = source === 'Production' ? 'Source' : 'Supplier';

            document.getElementById('disp-po').innerText = this.dataset.po;
            document.getElementById('disp-supplier').innerText = this.dataset.supplier;
            document.getElementById('disp-product').innerText = this.dataset.product;
            document.getElementById('disp-sku').innerText = `SKU: ${this.dataset.sku}`;
            document.getElementById('disp-batch').innerText = `Batch: ${this.dataset.batch}`;
            document.getElementById('disp-received').innerText = receivedQty.toLocaleString();

            document.getElementById('input-accepted').value = receivedQty;
            document.getElementById('input-damaged').value = 0;
            document.getElementById('input-rejected').value = 0;
            updateTotal();
        });
    });

    ['input-accepted', 'input-damaged', 'input-rejected'].forEach(id => {
        document.getElementById(id)?.addEventListener('input', updateTotal);
    });

    document.querySelector('#qcModal form')?.addEventListener('submit', function (event) {
        const accepted = Number(document.getElementById('input-accepted')?.value || 0);
        const damaged = Number(document.getElementById('input-damaged')?.value || 0);
        const rejected = Number(document.getElementById('input-rejected')?.value || 0);

        if (accepted + damaged + rejected !== receivedQty) {
            event.preventDefault();
            alert('Accepted, damaged, and rejected quantities must exactly equal the received quantity.');
        }
    });
});
