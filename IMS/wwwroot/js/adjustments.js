// Adjustments Scripts - Fully Connected
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('adjustmentSearch');
    const warehouseFilter = document.getElementById('adjustmentWarehouseFilter');
    const statusFilter = document.getElementById('adjustmentStatusFilter');
    const typeFilter = document.getElementById('adjustmentTypeFilter');
    const resetFilters = document.getElementById('resetAdjustmentFilters');
    const resultCount = document.getElementById('adjustmentResultCount');
    const emptyRow = document.getElementById('adjustmentEmptyRow');
    const rows = Array.from(document.querySelectorAll('.adjustment-row'));

    const normalize = (value) => String(value || '').trim().toLowerCase();

    function applyFilters() {
        const search = normalize(searchInput?.value);
        const selectedWarehouse = normalize(warehouseFilter?.value);
        const selectedStatus = normalize(statusFilter?.value);
        const selectedType = normalize(typeFilter?.value);
        let visibleCount = 0;

        rows.forEach((row) => {
            const matchesSearch = !search || normalize(row.dataset.search).includes(search);
            const matchesWarehouse = !selectedWarehouse || selectedWarehouse === 'all warehouses' || normalize(row.dataset.warehouse) === selectedWarehouse;
            const matchesStatus = !selectedStatus || selectedStatus === 'all statuses' || normalize(row.dataset.status) === selectedStatus;
            const matchesType = !selectedType || selectedType === 'all types' || normalize(row.dataset.type) === selectedType;
            const isVisible = matchesSearch && matchesWarehouse && matchesStatus && matchesType;

            row.style.display = isVisible ? '' : 'none';
            if (isVisible) visibleCount += 1;
        });

        if (emptyRow) emptyRow.style.display = visibleCount === 0 ? '' : 'none';
        if (resultCount) resultCount.textContent = `${visibleCount} adjustment${visibleCount === 1 ? '' : 's'} shown`;
    }

    [searchInput, warehouseFilter, statusFilter, typeFilter].forEach((control) => {
        control?.addEventListener('input', applyFilters);
        control?.addEventListener('change', applyFilters);
    });

    resetFilters?.addEventListener('click', () => {
        if (searchInput) searchInput.value = '';
        if (warehouseFilter) warehouseFilter.value = 'All Warehouses';
        if (statusFilter) statusFilter.value = 'All Statuses';
        if (typeFilter) typeFilter.value = 'All Types';
        applyFilters();
    });

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) element.textContent = value || '-';
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // ==========================================
    // 1. Adjustment Details Modal (AJAX Loaded)
    // ==========================================
    document.querySelectorAll('.js-view-adjustment').forEach((button) => {
        button.addEventListener('click', async () => {
            const adjId = button.dataset.adjustmentId;
            const row = rows.find((item) => item.dataset.adjustmentId === adjId);
            if (!row) return;

            const idCell = row.querySelector('.id-cell')?.textContent?.trim() || '-';
            setText('adjustmentDetailSubtitle', `${row.dataset.product || '-'} - ${row.dataset.sku || '-'}`);
            setText('detailAdjustmentNo', idCell);
            setText('detailAdjustmentStatus', row.dataset.status);
            setText('detailAdjustmentType', row.dataset.type);
            setText('detailAdjustmentDifference', row.dataset.difference);
            setText('detailAdjustmentWarehouse', row.dataset.warehouse);
            setText('detailAdjustmentShelf', row.dataset.shelf);
            setText('detailAdjustmentRequested', row.dataset.requested);
            setText('detailAdjustmentApproved', row.dataset.approved || 'Not approved yet');
            setText('detailAdjustmentReason', row.dataset.reason);

            const tbody = document.getElementById('detailItemsTableBody');
            if (tbody) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-center py-3 text-muted"><span class="spinner-border spinner-border-sm me-2"></span>Loading items from database...</td></tr>';
            }

            try {
                const response = await fetch(`?handler=AdjustmentDetails&id=${adjId}`);
                if (!response.ok) throw new Error('Failed to fetch details');
                const data = await response.json();

                if (data.success && data.items && data.items.length > 0) {
                    let rowsHtml = '';
                    data.items.forEach(item => {
                        const diff = item.differenceQty || 0;
                        const diffClass = diff > 0 ? 'diff-positive' : (diff < 0 ? 'diff-negative' : 'diff-zero');
                        const diffSign = diff > 0 ? `+${diff}` : `${diff}`;
                        
                        rowsHtml += `
                            <tr>
                                <td>
                                    <strong>${escapeHtml(item.productName || item.sku)}</strong>
                                    <div class="font-mono text-muted" style="font-size: 11px;">${escapeHtml(item.sku)}</div>
                                </td>
                                <td>
                                    <div>${escapeHtml(item.shelfCode)}</div>
                                    <div class="text-muted" style="font-size: 11px;">${escapeHtml(item.warehouseName)}</div>
                                </td>
                                <td class="text-right font-mono">${item.systemQty}</td>
                                <td class="text-right font-mono fw-bold">${item.countedQty}</td>
                                <td class="text-right font-mono"><span class="${diffClass}">${diffSign}</span></td>
                            </tr>
                        `;
                    });
                    if (tbody) tbody.innerHTML = rowsHtml;
                } else {
                    if (tbody) {
                        tbody.innerHTML = `
                            <tr>
                                <td><strong>${escapeHtml(row.dataset.product)}</strong><div class="font-mono text-muted" style="font-size: 11px;">${escapeHtml(row.dataset.sku)}</div></td>
                                <td><div>${escapeHtml(row.dataset.shelf)}</div><div class="text-muted" style="font-size: 11px;">${escapeHtml(row.dataset.warehouse)}</div></td>
                                <td class="text-right font-mono">${escapeHtml(row.dataset.system)}</td>
                                <td class="text-right font-mono fw-bold">${escapeHtml(row.dataset.counted)}</td>
                                <td class="text-right font-mono">${escapeHtml(row.dataset.difference)}</td>
                            </tr>
                        `;
                    }
                }
            } catch (err) {
                console.error("Error loading adjustment items:", err);
                if (tbody) {
                    tbody.innerHTML = `
                        <tr>
                            <td><strong>${escapeHtml(row.dataset.product)}</strong></td>
                            <td>${escapeHtml(row.dataset.shelf)}</td>
                            <td class="text-right font-mono">${escapeHtml(row.dataset.system)}</td>
                            <td class="text-right font-mono">${escapeHtml(row.dataset.counted)}</td>
                            <td class="text-right font-mono">${escapeHtml(row.dataset.difference)}</td>
                        </tr>
                    `;
                }
            }
        });
    });

    applyFilters();

    // Auto-open modal if productId is in query string
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.has('productId')) {
        const modalEl = document.getElementById('addAdjustmentModal');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    }

    // ==========================================
    // 2. Request Adjustment Form Live Stock & Variance
    // ==========================================
    const productDropdown = document.getElementById('NewItemProductID');
    const shelfDropdown = document.getElementById('NewItemShelfID');
    const typeDropdown = document.getElementById('NewAdjustment_AdjustmentType');
    const countedInput = document.getElementById('NewItemCountedQty');
    const systemStockDisplay = document.getElementById('systemStockDisplay');
    const varianceBox = document.getElementById('variancePreviewBox');
    const varianceText = document.getElementById('variancePreviewText');

    let currentSystemStock = null;

    async function fetchSystemStock() {
        const pId = productDropdown ? productDropdown.value : '';
        const sId = shelfDropdown ? shelfDropdown.value : '';

        if (!pId || !sId) {
            currentSystemStock = null;
            if (systemStockDisplay) systemStockDisplay.value = '-';
            updateVariancePreview();
            return;
        }

        if (systemStockDisplay) systemStockDisplay.value = 'Fetching...';

        try {
            const response = await fetch(`?handler=CurrentStock&productId=${encodeURIComponent(pId)}&shelfId=${encodeURIComponent(sId)}`);
            if (response.ok) {
                const data = await response.json();
                currentSystemStock = parseFloat(data.stock) || 0;
                if (systemStockDisplay) systemStockDisplay.value = currentSystemStock.toFixed(2);
            } else {
                currentSystemStock = 0;
                if (systemStockDisplay) systemStockDisplay.value = '0.00';
            }
        } catch (e) {
            console.error("Error fetching system stock:", e);
            currentSystemStock = 0;
            if (systemStockDisplay) systemStockDisplay.value = '0.00';
        }

        updateVariancePreview();
    }

    function updateVariancePreview() {
        if (!varianceBox || !varianceText) return;

        if (currentSystemStock === null || !countedInput || countedInput.value === '') {
            varianceBox.className = 'alert alert-info py-2 px-3 mb-0';
            varianceText.textContent = 'Select product and shelf, then enter physical counted stock to calculate variance.';
            return;
        }

        const counted = parseFloat(countedInput.value);
        if (isNaN(counted)) {
            varianceBox.className = 'alert alert-info py-2 px-3 mb-0';
            varianceText.textContent = 'Enter a valid physical counted quantity.';
            return;
        }

        const diff = counted - currentSystemStock;
        const adjType = typeDropdown ? typeDropdown.value : '';

        if (diff > 0) {
            if (adjType === 'AdjustmentOut') {
                varianceBox.className = 'alert alert-warning py-2 px-3 mb-0';
                varianceText.innerHTML = `<strong>Warning:</strong> Counted quantity (${counted}) is higher than system stock (${currentSystemStock}). For positive variance (+${diff.toFixed(2)} units), please choose <strong>Adjustment In</strong>.`;
            } else {
                varianceBox.className = 'alert alert-success py-2 px-3 mb-0';
                varianceText.innerHTML = `<strong>Positive Variance (+${diff.toFixed(2)} units):</strong> Stock will be increased from ${currentSystemStock.toFixed(2)} to ${counted.toFixed(2)} units upon posting.`;
            }
        } else if (diff < 0) {
            if (adjType === 'AdjustmentIn') {
                varianceBox.className = 'alert alert-warning py-2 px-3 mb-0';
                varianceText.innerHTML = `<strong>Warning:</strong> Counted quantity (${counted}) is lower than system stock (${currentSystemStock}). For negative variance (${diff.toFixed(2)} units), please choose <strong>Adjustment Out</strong>.`;
            } else {
                varianceBox.className = 'alert alert-danger py-2 px-3 mb-0';
                varianceText.innerHTML = `<strong>Negative Variance (${diff.toFixed(2)} units):</strong> Stock will be reduced from ${currentSystemStock.toFixed(2)} to ${counted.toFixed(2)} units upon posting.`;
            }
        } else {
            varianceBox.className = 'alert alert-secondary py-2 px-3 mb-0';
            varianceText.textContent = 'Count matches system stock exactly (Variance: 0 units).';
        }
    }

    function updateShelves() {
        if (!productDropdown || !shelfDropdown || !typeDropdown) return;

        const productId = productDropdown.value;
        const adjType = typeDropdown.value;

        if (productId && adjType === 'AdjustmentOut') {
            fetch(`?handler=ShelvesForProduct&productId=${productId}`)
                .then(response => response.json())
                .then(data => {
                    shelfDropdown.innerHTML = '<option value="">Select Shelf</option>';
                    if (data && data.length > 0) {
                        data.forEach(shelf => {
                            const option = document.createElement('option');
                            option.value = shelf.id || shelf.ID;
                            option.textContent = shelf.name || shelf.Name;
                            shelfDropdown.appendChild(option);
                        });
                    } else {
                        const option = document.createElement('option');
                        option.value = "";
                        option.textContent = "No available stock on any shelf";
                        option.disabled = true;
                        shelfDropdown.appendChild(option);
                    }
                    fetchSystemStock();
                })
                .catch(error => {
                    console.error('Error fetching shelves:', error);
                    fetchSystemStock();
                });
        } else {
            if (window.initialShelfOptions) {
                shelfDropdown.innerHTML = window.initialShelfOptions;
            }
            fetchSystemStock();
        }
    }

    if (shelfDropdown) {
        window.initialShelfOptions = shelfDropdown.innerHTML;
        shelfDropdown.addEventListener('change', fetchSystemStock);
    }

    if (productDropdown) {
        productDropdown.addEventListener('change', () => {
            updateShelves();
            fetchSystemStock();
        });
    }

    if (typeDropdown) {
        typeDropdown.addEventListener('change', () => {
            updateShelves();
            updateVariancePreview();
        });
    }

    if (countedInput) {
        countedInput.addEventListener('input', updateVariancePreview);
    }

    // Trigger initial check if values are preset
    if (productDropdown && productDropdown.value && shelfDropdown && shelfDropdown.value) {
        fetchSystemStock();
    }
});
