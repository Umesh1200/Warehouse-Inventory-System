// Production Planning Scripts
document.addEventListener('DOMContentLoaded', function () {
    const bomSelect = document.getElementById('createBomSelect');
    const qtyInput = document.getElementById('createPlannedQty');
    const inspectionBox = document.getElementById('bomInspectionBox');
    const inspFinishedName = document.getElementById('inspFinishedName');
    const inspFinishedSku = document.getElementById('inspFinishedSku');
    const inspTotalCost = document.getElementById('inspTotalCost');
    const inspReadinessAlert = document.getElementById('inspReadinessAlert');
    const inspComponentsBody = document.getElementById('inspComponentsBody');

    let debounceTimer;

    function fetchBomInspection() {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(async () => {
            const bomId = bomSelect ? bomSelect.value : null;
            let qty = qtyInput ? parseFloat(qtyInput.value) : 0;
            if (!qty || qty <= 0) qty = 1;

            if (!bomId || bomId === '') {
                if (inspectionBox) inspectionBox.style.display = 'none';
                return;
            }

            try {
                const response = await fetch(`/Production/Planning?handler=BomInspection&bomId=${bomId}&plannedQty=${qty}`);
                if (!response.ok) {
                    if (inspectionBox) inspectionBox.style.display = 'none';
                    return;
                }

                const data = await response.json();
                if (!data) {
                    if (inspectionBox) inspectionBox.style.display = 'none';
                    return;
                }

                // Render Finished Product info
                if (inspFinishedName) inspFinishedName.textContent = data.finishedProductName;
                if (inspFinishedSku) inspFinishedSku.textContent = `${data.finishedProductSKU} (v${data.bomVersion})`;
                if (inspTotalCost) inspTotalCost.textContent = `NPR ${data.estimatedBatchMaterialCost.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

                // Readiness Alert
                if (inspReadinessAlert) {
                    if (data.isReady) {
                        inspReadinessAlert.className = 'p-2 mb-2 rounded d-flex align-items-center gap-2 bg-success-subtle text-success border border-success-subtle';
                        inspReadinessAlert.innerHTML = `
                            <span class="material-symbols-outlined fs-6">check_circle</span>
                            <strong>Material Stock Verified:</strong> All ${data.totalComponents} required raw materials are available on warehouse shelves.
                        `;
                    } else {
                        inspReadinessAlert.className = 'p-2 mb-2 rounded d-flex align-items-center gap-2 bg-danger-subtle text-danger border border-danger-subtle';
                        inspReadinessAlert.innerHTML = `
                            <span class="material-symbols-outlined fs-6">warning</span>
                            <strong>Material Shortage Alert:</strong> ${data.shortageCount} of ${data.totalComponents} raw materials have insufficient stock for this batch size.
                        `;
                    }
                }

                // Components Table
                if (inspComponentsBody) {
                    inspComponentsBody.innerHTML = '';
                    data.components.forEach(item => {
                        const tr = document.createElement('tr');
                        const isShort = item.isShortage;
                        tr.className = isShort ? 'table-danger' : '';

                        const reqPerUnit = item.requiredPerUnit.toLocaleString('en-US', { maximumFractionDigits: 4 });
                        const totalReq = item.totalRequiredQty.toLocaleString('en-US', { maximumFractionDigits: 2 });
                        const avail = item.availableStock.toLocaleString('en-US', { maximumFractionDigits: 2 });

                        tr.innerHTML = `
                            <td>
                                <div class="fw-semibold">${item.productName}</div>
                                <span class="text-muted font-mono" style="font-size: 11px;">${item.sku}</span>
                            </td>
                            <td class="text-end font-mono">${reqPerUnit} ${item.unitCode}</td>
                            <td class="text-end font-mono fw-bold">${totalReq} ${item.unitCode}</td>
                            <td class="text-end font-mono ${isShort ? 'text-danger fw-bold' : 'text-success'}">${avail} ${item.unitCode}</td>
                            <td class="text-center">
                                ${isShort 
                                    ? `<span class="badge bg-danger">Short: -${item.shortageQty.toLocaleString('en-US', { maximumFractionDigits: 2 })}</span>` 
                                    : `<span class="badge bg-success">In Stock</span>`}
                            </td>
                        `;
                        inspComponentsBody.appendChild(tr);
                    });
                }

                if (inspectionBox) inspectionBox.style.display = 'block';
            } catch (err) {
                console.error('Error fetching BOM inspection:', err);
                if (inspectionBox) inspectionBox.style.display = 'none';
            }
        }, 200);
    }

    if (bomSelect) {
        bomSelect.addEventListener('change', fetchBomInspection);
    }
    if (qtyInput) {
        qtyInput.addEventListener('input', fetchBomInspection);
    }

    // Modal Edit Form Auto-populate
    document.querySelectorAll('.edit-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = document.getElementById('edit-id');
            const number = document.getElementById('edit-number');
            const bom = document.getElementById('edit-bom');
            const qty = document.getElementById('edit-qty');
            const start = document.getElementById('edit-start');
            const due = document.getElementById('edit-due');
            const prio = document.getElementById('edit-prio');
            const supervisor = document.getElementById('edit-supervisor');

            if (id) id.value = this.dataset.id || '';
            if (number) number.value = this.dataset.number || '';
            if (bom) bom.value = this.dataset.bom || '';
            if (qty) qty.value = this.dataset.qty || '';
            if (start) start.value = this.dataset.start || '';
            if (due) due.value = this.dataset.due || '';
            if (prio) prio.value = this.dataset.prio || 'Normal';
            if (supervisor) supervisor.value = this.dataset.supervisor || '';
        });
    });
});
