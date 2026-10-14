// Products Scripts
document.addEventListener('DOMContentLoaded', function () {
    console.log('Product Master Loaded');

    const productSupplierRows = Array.from(document.querySelectorAll('.product-avl-row'));
    const productSupplierEmptyRow = document.querySelector('.product-avl-empty-row');
    const avlForm = document.getElementById('avlMappingForm');

    // UI Elements
    const formTitle = document.getElementById('avlFormTitle');
    const submitBtn = document.getElementById('avlSubmitBtn');
    const resetBtn = document.getElementById('avlResetBtn');

    // Form inputs
    const supplierProductInput = document.getElementById('product-avl-supplier-product-id');
    const supplierSelect = document.getElementById('avlSupplierID');
    const skuInput = document.getElementById('avlSupplierSKU');
    const priceInput = document.getElementById('avlLastQuotedPrice');
    const leadInput = document.getElementById('avlLeadTimeDays');
    const moqInput = document.getElementById('avlMinOrderQty');
    const packInput = document.getElementById('avlPackSize');
    const preferredInput = document.getElementById('avlIsPreferred');
    const statusInput = document.getElementById('product-avl-status');

    // Open modal & Filter approved suppliers per product
    document.querySelectorAll('.product-suppliers-btn').forEach((button) => {
        button.addEventListener('click', () => {
            const productId = button.dataset.productId || '';
            const productName = button.dataset.productName || 'Product';
            const productSku = button.dataset.productSku || '';
            const productIdInput = document.getElementById('product-avl-product-id');
            const subtitle = document.getElementById('productSuppliersSubtitle');

            if (productIdInput) productIdInput.value = productId;
            if (subtitle) subtitle.textContent = productSku ? `${productName} - ${productSku}` : productName;

            // Reset form to Add Mapping mode
            resetAvlForm();

            // Filter mappings for the selected product
            let visibleCount = 0;
            productSupplierRows.forEach((row) => {
                const visible = row.dataset.productId === productId;
                row.hidden = !visible;
                if (visible) visibleCount += 1;
            });

            if (productSupplierEmptyRow) productSupplierEmptyRow.hidden = visibleCount > 0;
        });
    });

    // Edit supplier mapping handler
    document.querySelectorAll('.avl-edit-btn').forEach((btn) => {
        btn.addEventListener('click', (e) => {
            const row = btn.closest('.product-avl-row');
            if (!row) return;

            // Highlight editing row
            productSupplierRows.forEach(r => r.classList.remove('row-editing'));
            row.classList.add('row-editing');

            // Populate form values
            supplierProductInput.value = row.dataset.supplierProductId || '0';
            supplierSelect.value = row.dataset.supplierId || '';
            skuInput.value = row.dataset.supplierSku || '';
            priceInput.value = row.dataset.quotedPrice || '';
            leadInput.value = row.dataset.leadTime || '0';
            moqInput.value = row.dataset.moq || '1';
            packInput.value = row.dataset.packSize || '1';
            preferredInput.checked = row.dataset.isPreferred === 'true';
            statusInput.value = row.dataset.status || 'Approved';

            // Change UI text to edit mode
            formTitle.textContent = 'Edit Supplier Mapping';
            submitBtn.textContent = 'Update Mapping';
            resetBtn.style.display = 'inline-block';
            supplierSelect.disabled = true;
        });
    });

    // Reset/Cancel handler
    if (resetBtn) {
        resetBtn.addEventListener('click', resetAvlForm);
    }

    // Enable select before submit so POST receives the value
    if (avlForm) {
        avlForm.addEventListener('submit', () => {
            supplierSelect.disabled = false;
        });
    }

    function resetAvlForm() {
        if (!avlForm) return;

        // Clear values
        supplierProductInput.value = '0';
        supplierSelect.value = '';
        skuInput.value = '';
        priceInput.value = '';
        leadInput.value = '';
        moqInput.value = '1';
        packInput.value = '1';
        preferredInput.checked = false;
        statusInput.value = 'Approved';
        supplierSelect.disabled = false;

        // Clear highlights
        productSupplierRows.forEach(r => r.classList.remove('row-editing'));

        // Restore layout text
        formTitle.textContent = 'Add Supplier Mapping';
        submitBtn.textContent = 'Save Mapping';
        resetBtn.style.display = 'none';
    }


    // ==========================================
    // Product Storage Rules Page Scripts
    // ==========================================
    const ruleForm = document.getElementById('storageRuleForm');
    const ruleProductIdInput = document.getElementById('rule-product-id');
    const ruleIdInput = document.getElementById('rule-id');
    const ruleAllowedShelfType = document.getElementById('ruleAllowedShelfType');
    const ruleMaxStackQty = document.getElementById('ruleMaxStackQty');
    const ruleRequiresQCHold = document.getElementById('ruleRequiresQCHold');
    const ruleIsFragile = document.getElementById('ruleIsFragile');
    const ruleIsHazardous = document.getElementById('ruleIsHazardous');
    const ruleWarehouse = document.getElementById('ruleWarehouse');
    const ruleFloor = document.getElementById('ruleFloor');
    const ruleRoom = document.getElementById('ruleRoom');
    const ruleRack = document.getElementById('ruleRack');
    const ruleShelf = document.getElementById('ruleShelf');
    const rulesTableBody = document.getElementById('storageRulesTableBody');
    const ruleFormTitle = document.getElementById('ruleFormTitle');
    const ruleSubmitBtn = document.getElementById('ruleSubmitBtn');
    const ruleResetBtn = document.getElementById('ruleResetBtn');

    // Fetch helper
    async function fetchJson(url) {
        try {
            const res = await fetch(url);
            if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);
            return await res.json();
        } catch (err) {
            console.error('Error fetching data: ', err);
            return [];
        }
    }

    // Populate dropdown helper
    function populateDropdown(select, data, defaultText) {
        select.innerHTML = `<option value="">${defaultText}</option>`;
        data.forEach(item => {
            // Support both anonymous objects with capitalized properties and DTO objects
            const id = item.id !== undefined ? item.id : item.ID;
            const name = item.name !== undefined ? item.name : (item.Name || item.shelfCode || item.ShelfCode);
            select.innerHTML += `<option value="${id}">${name}</option>`;
        });
        select.disabled = data.length === 0;
    }

    // Cascading selection listeners
    ruleWarehouse.addEventListener('change', async () => {
        const whId = ruleWarehouse.value;
        ruleFloor.innerHTML = '<option value="">Any Floor</option>';
        ruleFloor.disabled = true;
        ruleRoom.innerHTML = '<option value="">Any Room</option>';
        ruleRoom.disabled = true;
        ruleRack.innerHTML = '<option value="">Any Rack</option>';
        ruleRack.disabled = true;
        ruleShelf.innerHTML = '<option value="">Any Shelf</option>';
        ruleShelf.disabled = true;

        if (whId) {
            const floors = await fetchJson(`?handler=Floors&warehouseId=${whId}`);
            populateDropdown(ruleFloor, floors, 'Any Floor');
        }
    });

    ruleFloor.addEventListener('change', async () => {
        const floorId = ruleFloor.value;
        ruleRoom.innerHTML = '<option value="">Any Room</option>';
        ruleRoom.disabled = true;
        ruleRack.innerHTML = '<option value="">Any Rack</option>';
        ruleRack.disabled = true;
        ruleShelf.innerHTML = '<option value="">Any Shelf</option>';
        ruleShelf.disabled = true;

        if (floorId) {
            const rooms = await fetchJson(`?handler=Rooms&floorId=${floorId}`);
            populateDropdown(ruleRoom, rooms, 'Any Room');
        }
    });

    ruleRoom.addEventListener('change', async () => {
        const roomId = ruleRoom.value;
        ruleRack.innerHTML = '<option value="">Any Rack</option>';
        ruleRack.disabled = true;
        ruleShelf.innerHTML = '<option value="">Any Shelf</option>';
        ruleShelf.disabled = true;

        if (roomId) {
            const racks = await fetchJson(`?handler=Racks&roomId=${roomId}`);
            populateDropdown(ruleRack, racks, 'Any Rack');
        }
    });

    ruleRack.addEventListener('change', async () => {
        const rackId = ruleRack.value;
        ruleShelf.innerHTML = '<option value="">Any Shelf</option>';
        ruleShelf.disabled = true;

        if (rackId) {
            const shelves = await fetchJson(`?handler=Shelves&rackId=${rackId}`);
            populateDropdown(ruleShelf, shelves, 'Any Shelf');
        }
    });

    // Open storage rules modal
    document.querySelectorAll('.product-storage-rules-btn').forEach(btn => {
        btn.addEventListener('click', async () => {
            const productId = btn.dataset.productId;
            const productName = btn.dataset.productName || 'Product';
            const productSku = btn.dataset.productSku || '';
            const subtitle = document.getElementById('productStorageRulesSubtitle');

            if (ruleProductIdInput) ruleProductIdInput.value = productId;
            if (subtitle) subtitle.textContent = productSku ? `${productName} - ${productSku}` : productName;

            resetStorageRuleForm();
            await loadStorageRules(productId);
        });
    });

    async function loadStorageRules(productId) {
        if (!rulesTableBody) return;
        rulesTableBody.innerHTML = '<tr><td colspan="5" class="text-center py-3">Loading rules...</td></tr>';
        
        const rules = await fetchJson(`?handler=StorageRules&productId=${productId}`);
        rulesTableBody.innerHTML = '';

        if (rules.length === 0) {
            rulesTableBody.innerHTML = `
                <tr>
                    <td colspan="5">
                        <div class="empty-state" style="padding: 2rem; text-align: center;">
                            <span class="material-symbols-outlined" style="font-size: 48px; color: var(--muted);">shelves</span>
                            <div style="font-weight: 600; margin-top: 0.5rem;">No storage rules defined for this product</div>
                            <p style="color: var(--muted); font-size: 13px; margin: 0.25rem 0 0 0;">Default layout fallback rules will apply.</p>
                        </div>
                    </td>
                </tr>`;
            return;
        }

        rules.forEach(rule => {
            // Flag badges
            let flagsHtml = '';
            if (rule.requiresQCHold) flagsHtml += '<span class="badge-status status-warning me-1 mb-1" style="background: #fef3c7; color: #d97706; padding: 2px 6px; border-radius: 4px; font-size: 11px;">QC Hold</span>';
            if (rule.isFragile) flagsHtml += '<span class="badge-status status-info me-1 mb-1" style="background: #e0f2fe; color: #0284c7; padding: 2px 6px; border-radius: 4px; font-size: 11px;">Fragile</span>';
            if (rule.isHazardous) flagsHtml += '<span class="badge-status status-danger me-1 mb-1" style="background: #fee2e2; color: #dc2626; padding: 2px 6px; border-radius: 4px; font-size: 11px;">Hazardous</span>';
            if (!flagsHtml) flagsHtml = '<span class="muted-cell">-</span>';

            // Path construction
            let path = 'Any Staging';
            if (rule.preferredWarehouseID) {
                path = rule.preferredWarehouseName;
                if (rule.preferredRackName && rule.preferredRackName !== '-') path += ` &rarr; ${rule.preferredRackName}`;
                if (rule.preferredShelfCode && rule.preferredShelfCode !== '-') path += ` &rarr; ${rule.preferredShelfCode}`;
            }

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td><strong>${rule.allowedShelfType}</strong></td>
                <td class="text-right font-mono">${rule.maxStackQty ? parseFloat(rule.maxStackQty).toLocaleString() : '-'}</td>
                <td>${flagsHtml}</td>
                <td style="font-size: 12px; max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${path}</td>
                <td class="text-center">
                    <div style="display: flex; gap: 0.25rem; justify-content: center; align-items: center;">
                        <button type="button" class="action-icon-link rule-edit-btn" title="Edit rule">
                            <span class="material-symbols-outlined">edit</span>
                        </button>
                        <form method="post" asp-page-handler="DeactivateStorageRule" style="margin:0;" onsubmit="return confirm('Deactivate this storage rule?');">
                            <input type="hidden" name="DeactivateStorageRuleID" value="${rule.ruleID}" />
                            <button type="submit" class="action-icon-link danger-action" title="Deactivate rule">
                                <span class="material-symbols-outlined">block</span>
                            </button>
                        </form>
                    </div>
                </td>`;

            // Bind edit click event
            tr.querySelector('.rule-edit-btn').addEventListener('click', async () => {
                // Highlight editing row
                tr.parentNode.querySelectorAll('tr').forEach(r => r.classList.remove('row-editing'));
                tr.classList.add('row-editing');

                // Populate form basic inputs
                ruleIdInput.value = rule.ruleID;
                ruleAllowedShelfType.value = rule.allowedShelfType;
                ruleMaxStackQty.value = rule.maxStackQty || '';
                ruleRequiresQCHold.checked = rule.requiresQCHold;
                ruleIsFragile.checked = rule.isFragile;
                ruleIsHazardous.checked = rule.isHazardous;

                // Load cascade selects via API
                if (rule.preferredWarehouseID) {
                    ruleWarehouse.value = rule.preferredWarehouseID;
                    
                    // Call API to fetch hierarchy
                    const hierarchy = await fetchJson(`?handler=LocationHierarchy&shelfId=${rule.preferredShelfID || 0}&rackId=${rule.preferredRackID || 0}`);
                    
                    if (hierarchy.warehouseId) {
                        // Load Floors
                        const floors = await fetchJson(`?handler=Floors&warehouseId=${hierarchy.warehouseId}`);
                        populateDropdown(ruleFloor, floors, 'Any Floor');
                        ruleFloor.value = hierarchy.floorId || '';

                        if (hierarchy.floorId) {
                            // Load Rooms
                            const rooms = await fetchJson(`?handler=Rooms&floorId=${hierarchy.floorId}`);
                            populateDropdown(ruleRoom, rooms, 'Any Room');
                            ruleRoom.value = hierarchy.roomId || '';

                            if (hierarchy.roomId) {
                                // Load Racks
                                const racks = await fetchJson(`?handler=Racks&roomId=${hierarchy.roomId}`);
                                populateDropdown(ruleRack, racks, 'Any Rack');
                                ruleRack.value = hierarchy.rackId || '';

                                if (hierarchy.rackId) {
                                    // Load Shelves
                                    const shelves = await fetchJson(`?handler=Shelves&rackId=${hierarchy.rackId}`);
                                    populateDropdown(ruleShelf, shelves, 'Any Shelf');
                                    ruleShelf.value = hierarchy.shelfId || '';
                                }
                            }
                        }
                    }
                } else {
                    ruleWarehouse.value = '';
                }

                // Change headers/buttons
                ruleFormTitle.textContent = 'Edit Storage Rule';
                ruleSubmitBtn.textContent = 'Update Rule';
                ruleResetBtn.style.display = 'inline-block';
                ruleAllowedShelfType.disabled = true;
            });

            rulesTableBody.appendChild(tr);
        });
    }

    if (ruleResetBtn) {
        ruleResetBtn.addEventListener('click', resetStorageRuleForm);
    }

    function resetStorageRuleForm() {
        if (!ruleForm) return;
        ruleIdInput.value = '0';
        ruleAllowedShelfType.value = '';
        ruleMaxStackQty.value = '';
        ruleRequiresQCHold.checked = false;
        ruleIsFragile.checked = false;
        ruleIsHazardous.checked = false;
        ruleWarehouse.value = '';
        ruleFloor.innerHTML = '<option value="">Any Floor</option>';
        ruleFloor.disabled = true;
        ruleRoom.innerHTML = '<option value="">Any Room</option>';
        ruleRoom.disabled = true;
        ruleRack.innerHTML = '<option value="">Any Rack</option>';
        ruleRack.disabled = true;
        ruleShelf.innerHTML = '<option value="">Any Shelf</option>';
        ruleShelf.disabled = true;
        
        ruleAllowedShelfType.disabled = false;
        ruleFormTitle.textContent = 'Add Storage Rule';
        ruleSubmitBtn.textContent = 'Save Rule';
        if (ruleResetBtn) ruleResetBtn.style.display = 'none';

        if (rulesTableBody) {
            rulesTableBody.querySelectorAll('tr').forEach(r => r.classList.remove('row-editing'));
        }
    }

    if (ruleForm) {
        ruleForm.addEventListener('submit', () => {
            ruleAllowedShelfType.disabled = false;
            ruleWarehouse.disabled = false;
            ruleFloor.disabled = false;
            ruleRoom.disabled = false;
            ruleRack.disabled = false;
            ruleShelf.disabled = false;
        });
    }

    // ==========================================
    // Product Insights Modal Scripts
    // ==========================================
    let stockTrendsChartInstance = null;
    let movementsChartInstance = null;
    let volumeChartInstance = null;
    let valuationChartInstance = null;

    document.querySelectorAll('.product-insights-btn').forEach(btn => {
        btn.addEventListener('click', async () => {
            const productId = btn.dataset.productId;
            const productName = btn.dataset.productName || 'Product';
            const productSku = btn.dataset.productSku || '';
            
            const subtitle = document.getElementById('productInsightsSubtitle');
            if (subtitle) {
                subtitle.textContent = productSku ? `${productName} - ${productSku}` : productName;
            }

            document.getElementById('insightsLoading').style.display = 'block';
            document.getElementById('insightsContent').style.display = 'none';

            // Destroy existing charts to prevent overlap
            if (stockTrendsChartInstance) stockTrendsChartInstance.destroy();
            if (movementsChartInstance) movementsChartInstance.destroy();
            if (volumeChartInstance) volumeChartInstance.destroy();
            if (valuationChartInstance) valuationChartInstance.destroy();

            try {
                const data = await fetchJson(`?handler=ProductInsights&productId=${productId}`);
                
                if (data && data.labels) {
                    // Render Stock Trends (Line)
                    const ctx1 = document.getElementById('stockTrendsChart').getContext('2d');
                    stockTrendsChartInstance = new Chart(ctx1, {
                        type: 'line',
                        data: {
                            labels: data.labels,
                            datasets: [{
                                label: 'Stock Level',
                                data: data.stockLevels,
                                borderColor: '#2563eb',
                                backgroundColor: 'rgba(37, 99, 235, 0.1)',
                                tension: 0.3,
                                fill: true
                            }]
                        }
                    });

                    // Render Movements (Bar)
                    const ctx2 = document.getElementById('movementsChart').getContext('2d');
                    movementsChartInstance = new Chart(ctx2, {
                        type: 'bar',
                        data: {
                            labels: data.labels,
                            datasets: [
                                { label: 'Stock In', data: data.stockIn, backgroundColor: '#16a34a' },
                                { label: 'Stock Out', data: data.stockOut, backgroundColor: '#dc2626' }
                            ]
                        }
                    });

                    // Render Volume (Bar)
                    const ctx3 = document.getElementById('volumeChart').getContext('2d');
                    volumeChartInstance = new Chart(ctx3, {
                        type: 'bar',
                        data: {
                            labels: data.labels,
                            datasets: [
                                { label: 'Purchase Vol (NPR)', data: data.purchaseVolume, backgroundColor: '#0284c7' },
                                { label: 'Sales Vol (NPR)', data: data.salesVolume, backgroundColor: '#f59e0b' }
                            ]
                        }
                    });

                    // Render Valuation (Line)
                    const ctx4 = document.getElementById('valuationChart').getContext('2d');
                    valuationChartInstance = new Chart(ctx4, {
                        type: 'line',
                        data: {
                            labels: data.labels,
                            datasets: [{
                                label: 'Inventory Value (NPR)',
                                data: data.valuation,
                                borderColor: '#8b5cf6',
                                backgroundColor: 'rgba(139, 92, 246, 0.1)',
                                tension: 0.3,
                                fill: true
                            }]
                        }
                    });
                }
            } catch (err) {
                console.error('Error loading insights:', err);
            }

            document.getElementById('insightsLoading').style.display = 'none';
            document.getElementById('insightsContent').style.display = 'block';
        });
    });
});

// Category Form Management
window.editCategory = function(id, name, desc, isActive) {
    document.querySelectorAll('[id^="cat-row-"]').forEach(r => r.classList.remove('row-editing'));
    const row = document.getElementById('cat-row-' + id);
    if (row) row.classList.add('row-editing');

    const formId = document.getElementById('categoryFormId');
    const formName = document.getElementById('categoryFormName');
    const formDesc = document.getElementById('categoryFormDesc');
    const formActive = document.getElementById('categoryFormActive');
    const formTitle = document.getElementById('categoryFormTitle');
    const submitBtn = document.getElementById('categorySubmitBtn');
    const resetBtn = document.getElementById('categoryResetBtn');

    if (formId) formId.value = id;
    if (formName) formName.value = name;
    if (formDesc) formDesc.value = desc || '';
    if (formActive) formActive.checked = isActive;
    if (formTitle) formTitle.textContent = 'Edit Category';
    if (submitBtn) submitBtn.textContent = 'Update Category';
    if (resetBtn) resetBtn.style.display = 'inline-block';
};

window.resetCategoryForm = function() {
    document.querySelectorAll('[id^="cat-row-"]').forEach(r => r.classList.remove('row-editing'));
    const formId = document.getElementById('categoryFormId');
    const formName = document.getElementById('categoryFormName');
    const formDesc = document.getElementById('categoryFormDesc');
    const formActive = document.getElementById('categoryFormActive');
    const formTitle = document.getElementById('categoryFormTitle');
    const submitBtn = document.getElementById('categorySubmitBtn');
    const resetBtn = document.getElementById('categoryResetBtn');

    if (formId) formId.value = '0';
    if (formName) formName.value = '';
    if (formDesc) formDesc.value = '';
    if (formActive) formActive.checked = true;
    if (formTitle) formTitle.textContent = 'Add New Category';
    if (submitBtn) submitBtn.textContent = 'Save Category';
    if (resetBtn) resetBtn.style.display = 'none';
};

// Unit Form Management
window.editUnit = function(id, name, code, isActive) {
    document.querySelectorAll('[id^="unit-row-"]').forEach(r => r.classList.remove('row-editing'));
    const row = document.getElementById('unit-row-' + id);
    if (row) row.classList.add('row-editing');

    const formId = document.getElementById('unitFormId');
    const formName = document.getElementById('unitFormName');
    const formCode = document.getElementById('unitFormCode');
    const formActive = document.getElementById('unitFormActive');
    const formTitle = document.getElementById('unitFormTitle');
    const submitBtn = document.getElementById('unitSubmitBtn');
    const resetBtn = document.getElementById('unitResetBtn');

    if (formId) formId.value = id;
    if (formName) formName.value = name;
    if (formCode) formCode.value = code;
    if (formActive) formActive.checked = isActive;
    if (formTitle) formTitle.textContent = 'Edit Unit';
    if (submitBtn) submitBtn.textContent = 'Update Unit';
    if (resetBtn) resetBtn.style.display = 'inline-block';
};

window.resetUnitForm = function() {
    document.querySelectorAll('[id^="unit-row-"]').forEach(r => r.classList.remove('row-editing'));
    const formId = document.getElementById('unitFormId');
    const formName = document.getElementById('unitFormName');
    const formCode = document.getElementById('unitFormCode');
    const formActive = document.getElementById('unitFormActive');
    const formTitle = document.getElementById('unitFormTitle');
    const submitBtn = document.getElementById('unitSubmitBtn');
    const resetBtn = document.getElementById('unitResetBtn');

    if (formId) formId.value = '0';
    if (formName) formName.value = '';
    if (formCode) formCode.value = '';
    if (formActive) formActive.checked = true;
    if (formTitle) formTitle.textContent = 'Add New Unit';
    if (submitBtn) submitBtn.textContent = 'Save Unit';
    if (resetBtn) resetBtn.style.display = 'none';
};


