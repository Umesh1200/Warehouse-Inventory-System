// Stock Transfers Scripts
document.addEventListener('DOMContentLoaded', function () {
    let isCurrentShelfCompatible = true;

    async function updateDropdown(handler, paramName, paramValue, targetSelect, nextSelects = []) {
        if (!targetSelect) return;
        targetSelect.innerHTML = '<option value="">Select...</option>';
        targetSelect.disabled = true;

        if (!paramValue) return;

        try {
            const response = await fetch(`?handler=${handler}&${paramName}=${paramValue}`);
            const data = await response.json();

            if (data && data.length > 0) {
                const selectedProdOpt = srcProduct?.selectedOptions[0];
                const requiredShelfType = selectedProdOpt?.dataset.requiredShelfType || '';

                data.forEach(item => {
                    const option = document.createElement('option');
                    option.value = item.id;
                    option.textContent = item.name;
                    option.dataset.name = item.name;
                    if (item.code) option.dataset.code = item.code;
                    if (item.type) option.dataset.type = item.type;

                    // Annotate compatibility if we know the product storage rule
                    if (requiredShelfType && item.type && targetSelect === destShelf) {
                        if (item.type !== requiredShelfType) {
                            option.textContent = `${item.name} ❌ Incompatible (${item.type})`;
                            option.style.color = '#dc2626';
                        } else {
                            option.textContent = `${item.name} ✓ Compatible`;
                        }
                    }

                    targetSelect.appendChild(option);
                });
                targetSelect.disabled = false;
            }
        } catch (error) {
            console.error(`Error fetching ${handler}:`, error);
        }

        // Clear sub-dropdowns
        nextSelects.forEach(nextId => {
            const nextSelect = document.getElementById(nextId);
            if (nextSelect) {
                nextSelect.innerHTML = '<option value="">Select...</option>';
                nextSelect.disabled = true;
            }
        });
    }

    const srcWarehouse = document.getElementById('srcWarehouse');
    const srcFloor = document.getElementById('srcFloor');
    const srcRoom = document.getElementById('srcRoom');
    const srcRack = document.getElementById('srcRack');
    const srcShelf = document.getElementById('srcShelf');
    const srcProduct = document.getElementById('srcProduct');
    const destWarehouse = document.getElementById('destWarehouse');
    const destFloor = document.getElementById('destFloor');
    const destRoom = document.getElementById('destRoom');
    const destRack = document.getElementById('destRack');
    const destShelf = document.getElementById('destShelf');
    const transferQty = document.getElementById('transferQty');
    const destinationAdvice = document.getElementById('destinationAdvice');
    const sameShelfAlert = document.getElementById('sameShelfAlert');
    const incompatibleAlert = document.getElementById('incompatibleShelfAlert');
    const incompatibleReason = document.getElementById('incompatibleShelfReason');
    const destCapacityCard = document.getElementById('destShelfCapacityCard');
    const capCompatibilityAlert = document.getElementById('capCompatibilityAlert');
    const productStorageRuleInfo = document.getElementById('productStorageRuleInfo');
    const prodTypeBadge = document.getElementById('prodTypeBadge');
    const prodRuleText = document.getElementById('prodRuleText');
    const submitBtn = document.querySelector('#addTransferModal button[type="submit"]');

    // Validation
    function validateForm() {
        const fromShelfId = srcShelf?.value;
        const toShelfId = destShelf?.value;

        // Check same shelf
        if (fromShelfId && toShelfId && fromShelfId === toShelfId) {
            if (sameShelfAlert) sameShelfAlert.style.display = 'block';
            if (submitBtn) submitBtn.disabled = true;
            if (srcShelf) srcShelf.classList.add('is-invalid');
            if (destShelf) destShelf.classList.add('is-invalid');
            return false;
        } else {
            if (sameShelfAlert) sameShelfAlert.style.display = 'none';
            if (srcShelf) srcShelf.classList.remove('is-invalid');
        }

        // Check compatibility
        if (toShelfId && !isCurrentShelfCompatible) {
            if (submitBtn) submitBtn.disabled = true;
            if (destShelf) destShelf.classList.add('is-invalid');
            return false;
        }

        if (destShelf) destShelf.classList.remove('is-invalid');
        if (submitBtn) submitBtn.disabled = false;
        return true;
    }

    // Fetch and display Destination Shelf Capacity & Compatibility Card
    async function updateDestinationShelfCapacity(shelfId) {
        if (!destCapacityCard) return;

        if (!shelfId) {
            destCapacityCard.style.display = 'none';
            if (incompatibleAlert) incompatibleAlert.style.display = 'none';
            isCurrentShelfCompatible = true;
            validateForm();
            return;
        }

        try {
            const productId = srcProduct?.value || 0;
            const data = await fetchData('ShelfCapacityDetails', `shelfId=${shelfId}&productId`, productId);
            if (data && data.shelfID) {
                document.getElementById('capShelfCode').textContent = data.shelfCode;
                document.getElementById('capShelfName').textContent = data.shelfName ? `(${data.shelfName})` : '';
                document.getElementById('capShelfType').textContent = data.shelfType || 'General';
                document.getElementById('capMaxQty').textContent = Number(data.maxQty).toLocaleString();
                document.getElementById('capAvailQty').textContent = Number(data.availableQty).toLocaleString();
                document.getElementById('capMaxWeight').textContent = `${Number(data.maxWeightKg).toLocaleString()} kg`;
                document.getElementById('capAvailWeight').textContent = `${Number(data.availableWeightKg).toLocaleString()} kg`;
                
                const util = Math.min(100, Math.max(0, Number(data.utilizationPercentage || 0)));
                const bar = document.getElementById('capUtilBar');
                if (bar) {
                    bar.style.width = `${util}%`;
                    bar.className = util > 85 ? 'progress-bar bg-danger' : (util > 60 ? 'progress-bar bg-warning' : 'progress-bar bg-primary');
                }
                const utilText = document.getElementById('capUtilText');
                if (utilText) utilText.textContent = `Utilization: ${util.toFixed(1)}%`;
                
                const locPath = document.getElementById('capLocationPath');
                if (locPath) locPath.textContent = data.fullPath;

                // Compatibility check evaluation
                if (data.isCompatible !== null && data.isCompatible !== undefined) {
                    if (!data.isCompatible) {
                        isCurrentShelfCompatible = false;
                        if (incompatibleAlert) {
                            incompatibleAlert.style.display = 'block';
                            if (incompatibleReason) incompatibleReason.textContent = data.incompatibilityReason || `Product cannot be stored on [${data.shelfType}] shelves.`;
                        }
                        if (capCompatibilityAlert) {
                            capCompatibilityAlert.style.display = 'block';
                            capCompatibilityAlert.className = 'mt-2 p-1 rounded text-center bg-danger text-white';
                            capCompatibilityAlert.innerHTML = `<span class="material-symbols-outlined" style="font-size: 14px; vertical-align: middle;">cancel</span> Incompatible Storage Rule (${data.shelfType})`;
                        }
                    } else {
                        isCurrentShelfCompatible = true;
                        if (incompatibleAlert) incompatibleAlert.style.display = 'none';
                        if (capCompatibilityAlert) {
                            capCompatibilityAlert.style.display = 'block';
                            capCompatibilityAlert.className = 'mt-2 p-1 rounded text-center bg-success text-white';
                            capCompatibilityAlert.innerHTML = `<span class="material-symbols-outlined" style="font-size: 14px; vertical-align: middle;">check_circle</span> Compatible with Product Storage Rules`;
                        }
                    }
                } else {
                    isCurrentShelfCompatible = true;
                    if (incompatibleAlert) incompatibleAlert.style.display = 'none';
                    if (capCompatibilityAlert) capCompatibilityAlert.style.display = 'none';
                }

                destCapacityCard.style.display = 'block';
            } else {
                destCapacityCard.style.display = 'none';
                isCurrentShelfCompatible = true;
            }
        } catch (err) {
            console.error('Error fetching shelf capacity details:', err);
            destCapacityCard.style.display = 'none';
            isCurrentShelfCompatible = true;
        }

        validateForm();
    }

    srcWarehouse?.addEventListener('change', async function() {
        await updateDropdown('Floors', 'warehouseId', this.value, document.getElementById('srcFloor'), ['srcRoom', 'srcRack', 'srcShelf', 'srcProduct']);
    });
    srcFloor?.addEventListener('change', async function() {
        await updateDropdown('Rooms', 'floorId', this.value, document.getElementById('srcRoom'), ['srcRack', 'srcShelf', 'srcProduct']);
    });
    srcRoom?.addEventListener('change', async function() {
        await updateDropdown('Racks', 'roomId', this.value, document.getElementById('srcRack'), ['srcShelf', 'srcProduct']);
    });
    srcRack?.addEventListener('change', async function() {
        await updateDropdown('Shelves', 'rackId', this.value, document.getElementById('srcShelf'), ['srcProduct']);
    });
    srcShelf?.addEventListener('change', async function() {
        const productSelect = srcProduct;
        productSelect.innerHTML = '<option value="">Select Product on Shelf</option>';
        productSelect.disabled = true;
        if (productStorageRuleInfo) productStorageRuleInfo.style.display = 'none';

        const data = await fetchData('ShelfProducts', 'shelfId', this.value);
        if (data && data.length > 0) {
            data.forEach(p => {
                const option = document.createElement('option');
                option.value = p.id;
                option.textContent = p.name;
                option.dataset.name = p.name.toLowerCase();
                option.dataset.availableQty = p.availableQty;
                option.dataset.productType = p.productType || '';
                option.dataset.requiredShelfType = p.requiredShelfType || '';
                productSelect.appendChild(option);
            });
            productSelect.disabled = false;
        } else {
            productSelect.innerHTML = '<option value="">No available stock on this shelf</option>';
        }
        updateDestinationAdvice();
        validateForm();
    });

    srcProduct?.addEventListener('change', function() {
        const selectedOpt = this.selectedOptions[0];
        if (selectedOpt && selectedOpt.value) {
            const prodType = selectedOpt.dataset.productType || 'General';
            const reqShelf = selectedOpt.dataset.requiredShelfType || prodType;
            if (productStorageRuleInfo) {
                if (prodTypeBadge) prodTypeBadge.textContent = prodType;
                if (prodRuleText) prodRuleText.textContent = `Requires [${reqShelf}] Shelf`;
                productStorageRuleInfo.style.display = 'block';
            }
        } else {
            if (productStorageRuleInfo) productStorageRuleInfo.style.display = 'none';
        }

        if (destShelf?.value) {
            updateDestinationShelfCapacity(destShelf.value);
        }
        updateDestinationAdvice();
    });

    destWarehouse?.addEventListener('change', async function() {
        await updateDropdown('Floors', 'warehouseId', this.value, document.getElementById('destFloor'), ['destRoom', 'destRack', 'destShelf']);
        updateDestinationShelfCapacity(null);
    });
    destFloor?.addEventListener('change', async function() {
        await updateDropdown('Rooms', 'floorId', this.value, document.getElementById('destRoom'), ['destRack', 'destShelf']);
        updateDestinationShelfCapacity(null);
    });
    destRoom?.addEventListener('change', async function() {
        await updateDropdown('Racks', 'roomId', this.value, document.getElementById('destRack'), ['destShelf']);
        updateDestinationShelfCapacity(null);
    });
    destRack?.addEventListener('change', async function() {
        await updateDropdown('Shelves', 'rackId', this.value, document.getElementById('destShelf'));
        updateDestinationShelfCapacity(null);
    });
    destShelf?.addEventListener('change', async function() {
        await updateDestinationShelfCapacity(this.value);
    });

    const productSearchInput = document.getElementById('productSearch');
    const productSelect = srcProduct;

    productSearchInput?.addEventListener('input', function() {
        const filter = this.value.toLowerCase();
        Array.from(productSelect.options).forEach(option => {
            const text = option.textContent.toLowerCase();
            option.style.display = text.includes(filter) ? '' : 'none';
        });
    });

    async function fetchData(handler, paramName, paramValue) {
        const query = paramName ? `?handler=${handler}&${paramName}=${paramValue}` : `?handler=${handler}`;
        const response = await fetch(query);
        return await response.json();
    }

    async function updateDestinationAdvice() {
        if (!destinationAdvice) return;

        const productId = srcProduct?.value;
        const quantity = Number(transferQty?.value || 0);
        if (!productId || quantity <= 0) {
            destinationAdvice.textContent = 'Select a source product and enter quantity to review compatible destination shelves.';
            destinationAdvice.classList.remove('is-warning');
            return;
        }

        try {
            const shelves = await fetchData('DestinationShelves', `productId=${productId}&quantity`, quantity);
            const recommended = shelves.filter(shelf => shelf.isRecommended).slice(0, 3);
            if (recommended.length === 0) {
                destinationAdvice.textContent = 'Warning: No compatible destination shelf currently has enough quantity/weight capacity.';
                destinationAdvice.classList.add('is-warning');
                return;
            }

            destinationAdvice.innerHTML = `<strong>Recommended Compatible Shelves:</strong><br/>` + 
                recommended.map(shelf => `<span class="badge bg-light text-dark border me-1">${shelf.shelfCode} [${shelf.shelfType}] (${Number(shelf.quantityAvailable).toLocaleString()} qty avail, ${Number(shelf.weightAvailableKg).toLocaleString()} kg)</span>`).join(' ');
            destinationAdvice.classList.remove('is-warning');
        } catch {
            destinationAdvice.textContent = 'Destination shelf advice could not be loaded.';
            destinationAdvice.classList.add('is-warning');
        }
    }

    transferQty?.addEventListener('input', updateDestinationAdvice);
    
    // Initial Load Warehouses into both dropdowns
    async function initWarehouses() {
        try {
            const warehouses = await fetchData('Warehouses');
            const whSelects = document.querySelectorAll('[data-type="warehouse"]');
            whSelects.forEach(select => {
                select.innerHTML = '<option value="">Select Warehouse</option>';
                warehouses.forEach(wh => {
                    const option = document.createElement('option');
                    option.value = wh.id;
                    option.textContent = wh.name;
                    select.appendChild(option);
                });
            });
        } catch (err) {
            console.error('Error initializing warehouses:', err);
        }
    }

    initWarehouses();

    const transferForm = document.querySelector('#addTransferModal form');
    if (transferForm) {
        transferForm.addEventListener('submit', function(e) {
            if (!validateForm()) {
                e.preventDefault();
                return false;
            }
            // Enable all selects before submission so they are sent in the POST request
            transferForm.querySelectorAll('select').forEach(s => {
                s.disabled = false;
            });
        });
    }

    // --- Table Filtering Logic ---
    const searchInput = document.getElementById('transferSearchInput');
    const warehouseFilter = document.getElementById('warehouseFilter');
    const typeFilter = document.getElementById('typeFilter');
    const statusFilter = document.getElementById('statusFilter');
    const tableRows = document.querySelectorAll('.transfer-row');

    function filterTable() {
        const query = (searchInput?.value || '').toLowerCase().trim();
        const selectedWh = (warehouseFilter?.value || 'All Warehouses').trim();
        const selectedType = (typeFilter?.value || 'All Types').trim();
        const selectedStatus = (statusFilter?.value || 'All Statuses').trim();

        tableRows.forEach(row => {
            const rowSearch = row.getAttribute('data-search') || '';
            const rowWh = row.getAttribute('data-warehouse') || '';
            const rowType = row.getAttribute('data-type') || '';
            const rowStatus = row.getAttribute('data-status') || '';

            const matchesQuery = !query || rowSearch.includes(query);
            const matchesWh = selectedWh === 'All Warehouses' || rowWh.toLowerCase().includes(selectedWh.toLowerCase());
            const matchesType = selectedType === 'All Types' || rowType.toLowerCase() === selectedType.toLowerCase();
            const matchesStatus = selectedStatus === 'All Statuses' || rowStatus.toLowerCase() === selectedStatus.toLowerCase();

            if (matchesQuery && matchesWh && matchesType && matchesStatus) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    searchInput?.addEventListener('input', filterTable);
    warehouseFilter?.addEventListener('change', filterTable);
    typeFilter?.addEventListener('change', filterTable);
    statusFilter?.addEventListener('change', filterTable);

    // --- Transfer Details Modal ---
    const detailModalEl = document.getElementById('transferDetailModal');
    const modalTransferNo = document.getElementById('modalTransferNo');
    const detailLoading = document.getElementById('transferDetailLoading');
    const detailContent = document.getElementById('transferDetailContent');
    const itemsTableBody = document.getElementById('transferItemsTableBody');

    document.querySelectorAll('.btn-view-details').forEach(btn => {
        btn.addEventListener('click', async function() {
            const transferId = this.getAttribute('data-id');
            const transferNo = this.getAttribute('data-no');

            if (modalTransferNo) modalTransferNo.textContent = transferNo;
            if (detailLoading) detailLoading.style.display = 'block';
            if (detailContent) detailContent.style.display = 'none';
            if (itemsTableBody) itemsTableBody.innerHTML = '';

            const modal = new bootstrap.Modal(detailModalEl);
            modal.show();

            try {
                const response = await fetch(`?handler=TransferItems&transferId=${transferId}`);
                const items = await response.json();

                if (detailLoading) detailLoading.style.display = 'none';
                if (detailContent) detailContent.style.display = 'block';

                if (items && items.length > 0) {
                    items.forEach(item => {
                        const tr = document.createElement('tr');
                        tr.innerHTML = `
                            <td>
                                <div style="font-weight: 600; color: var(--primary);">${item.productName}</div>
                                <div class="font-mono" style="font-size: 11px; color: var(--text-muted);">${item.sku}</div>
                            </td>
                            <td class="text-right font-mono" style="font-weight: 700;">${item.quantity}</td>
                            <td>
                                <div><strong class="font-mono" style="color: var(--primary);">${item.fromShelfCode}</strong></div>
                                <div style="font-size: 11px; color: var(--text-muted);">${item.fromWarehouseName} &gt; ${item.fromFloorName} &gt; ${item.fromRoomName} &gt; ${item.fromRackName}</div>
                            </td>
                            <td>
                                <div><strong class="font-mono" style="color: var(--secondary);">${item.toShelfCode}</strong></div>
                                <div style="font-size: 11px; color: var(--text-muted);">${item.toWarehouseName} &gt; ${item.toFloorName} &gt; ${item.toRoomName} &gt; ${item.toRackName}</div>
                            </td>
                        `;
                        itemsTableBody.appendChild(tr);
                    });
                } else {
                    itemsTableBody.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-3">No line items found.</td></tr>`;
                }
            } catch (err) {
                console.error('Error fetching transfer details:', err);
                if (detailLoading) detailLoading.style.display = 'none';
                if (detailContent) detailContent.style.display = 'block';
                itemsTableBody.innerHTML = `<tr><td colspan="4" class="text-center text-danger py-3">Failed to load transfer items.</td></tr>`;
            }
        });
    });
});
