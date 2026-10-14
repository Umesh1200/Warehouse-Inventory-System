// Cycle Counts Scripts
document.addEventListener('DOMContentLoaded', function () {
    console.log('Cycle Counts Loaded');

    // ==========================================
    // 1. Modal Toggle Fix
    // ==========================================
    const createBtn = document.querySelector('[data-bs-target="#createCountModal"]');
    if (createBtn) {
        createBtn.addEventListener('click', function(e) {
            if (!document.querySelector('.modal-backdrop')) {
                const modalEl = document.getElementById('createCountModal');
                if (modalEl) {
                    const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
                    modal.show();
                }
            }
        });
    }

    // ==========================================
    // 2. DOM Elements & State
    // ==========================================
    const warehouseSelect = document.getElementById('NewCount_WarehouseID');
    const floorSelect = document.getElementById('NewCount_FloorID');
    const shelfSelect = document.getElementById('NewCount_ShelfID');
    const hiddenProductId = document.getElementById('NewCount_ProductID');
    const scopeHintBadge = document.getElementById('productScopeHint');

    // Searchable Dropdown elements
    const dropdownContainer = document.getElementById('productSearchableDropdown');
    const selectTrigger = document.getElementById('productSelectTrigger');
    const selectLabel = document.getElementById('productSelectLabel');
    const clearSelectionBtn = document.getElementById('productClearBtn');
    const dropdownPanel = document.getElementById('productDropdownPanel');
    const searchInput = document.getElementById('productSearchInput');
    const searchClearBtn = document.getElementById('productSearchClearBtn');
    const resultCountEl = document.getElementById('productResultCount');
    const optionsListEl = document.getElementById('productListOptions');

    // Internal state
    let allProducts = [];
    let filteredProducts = [];
    let selectedProductId = '';
    let selectedProductObj = null;
    let highlightedIndex = -1;
    let isDropdownOpen = false;

    // Helper: Escape HTML to avoid XSS
    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    // Helper: Highlight matching substring
    function highlightMatch(text, query) {
        if (!text) return '';
        if (!query) return escapeHtml(text);
        
        const escapedText = escapeHtml(text);
        const escapedQuery = escapeHtml(query);
        const regex = new RegExp(`(${escapedQuery.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
        return escapedText.replace(regex, '<span class="match-highlight">$1</span>');
    }

    // ==========================================
    // 3. Data Fetching
    // ==========================================
    async function fetchData(handler, paramName, paramValue) {
        const query = paramName ? `?handler=${handler}&${paramName}=${paramValue}` : `?handler=${handler}`;
        try {
            const response = await fetch(query);
            if (!response.ok) return [];
            return await response.json();
        } catch (e) {
            console.error("Fetch failed for handler: " + handler, e);
            return [];
        }
    }

    async function loadProducts(warehouseId, floorId, shelfId) {
        showLoadingState();
        
        let query = `?handler=ProductsByLocation`;
        if (warehouseId) query += `&warehouseId=${encodeURIComponent(warehouseId)}`;
        if (floorId) query += `&floorId=${encodeURIComponent(floorId)}`;
        if (shelfId) query += `&shelfId=${encodeURIComponent(shelfId)}`;

        try {
            const response = await fetch(query);
            if (response.ok) {
                const data = await response.json();
                allProducts = Array.isArray(data) ? data : [];
            } else {
                allProducts = [];
            }
        } catch (e) {
            console.error("Failed to load products:", e);
            allProducts = [];
        }

        // If previous selection is no longer in products list, reset selection
        if (selectedProductId) {
            const stillExists = allProducts.some(p => String(p.id || p.ID) === String(selectedProductId));
            if (!stillExists) {
                clearProductSelection(false);
            }
        }

        filterAndRenderProducts();
        updateScopeHint();
    }

    function updateScopeHint() {
        if (!scopeHintBadge) return;
        
        const whName = warehouseSelect && warehouseSelect.value ? warehouseSelect.options[warehouseSelect.selectedIndex]?.text : '';
        const floorName = floorSelect && floorSelect.value ? floorSelect.options[floorSelect.selectedIndex]?.text : '';
        const shelfName = shelfSelect && shelfSelect.value ? shelfSelect.options[shelfSelect.selectedIndex]?.text : '';

        if (shelfName && shelfSelect.value) {
            scopeHintBadge.textContent = `Scope: ${shelfName}`;
        } else if (floorName && floorSelect.value) {
            scopeHintBadge.textContent = `Scope: ${floorName}`;
        } else if (whName && warehouseSelect.value) {
            scopeHintBadge.textContent = `Scope: ${whName}`;
        } else {
            scopeHintBadge.textContent = `Scope: All Products`;
        }
    }

    // ==========================================
    // 4. Render & Filter Searchable Dropdown
    // ==========================================
    function showLoadingState() {
        if (!optionsListEl) return;
        if (resultCountEl) resultCountEl.textContent = "Loading products...";
        optionsListEl.innerHTML = `
            <div class="product-loading-state">
                <span class="material-symbols-outlined spin-icon">progress_activity</span>
                <span>Fetching products for location...</span>
            </div>
        `;
    }

    function filterAndRenderProducts() {
        if (!optionsListEl) return;

        const query = (searchInput ? searchInput.value : '').trim().toLowerCase();
        
        if (!query) {
            filteredProducts = [...allProducts];
        } else {
            filteredProducts = allProducts.filter(p => {
                const sku = (p.sku || p.SKU || '').toLowerCase();
                const name = (p.name || p.Name || p.productName || '').toLowerCase();
                const category = (p.category || p.Category || p.categoryName || '').toLowerCase();
                const unit = (p.unit || p.Unit || p.unitCode || '').toLowerCase();
                return sku.includes(query) || name.includes(query) || category.includes(query) || unit.includes(query);
            });
        }

        // Update Counter
        if (resultCountEl) {
            if (query) {
                resultCountEl.textContent = `${filteredProducts.length} product${filteredProducts.length === 1 ? '' : 's'} found`;
            } else {
                resultCountEl.textContent = `Showing all ${filteredProducts.length} product${filteredProducts.length === 1 ? '' : 's'}`;
            }
        }

        // Render Options List
        optionsListEl.innerHTML = '';

        // Default "All Products" option at top
        const defaultOption = document.createElement('div');
        defaultOption.className = `product-option-default ${!selectedProductId ? 'selected' : ''}`;
        defaultOption.setAttribute('data-id', '');
        defaultOption.setAttribute('role', 'option');
        defaultOption.setAttribute('aria-selected', !selectedProductId ? 'true' : 'false');
        defaultOption.innerHTML = `
            <span class="material-symbols-outlined" style="font-size: 18px; color: var(--secondary);">all_inclusive</span>
            <span>All Products in Location (Optional)</span>
        `;
        defaultOption.addEventListener('click', () => {
            clearProductSelection(true);
            closeDropdown();
        });
        optionsListEl.appendChild(defaultOption);

        if (filteredProducts.length === 0) {
            const emptyEl = document.createElement('div');
            emptyEl.className = 'product-empty-state';
            emptyEl.innerHTML = `
                <span class="material-symbols-outlined">search_off</span>
                <strong>No matching products</strong>
                <span>Try a different SKU or product name</span>
            `;
            optionsListEl.appendChild(emptyEl);
            highlightedIndex = -1;
            return;
        }

        filteredProducts.forEach((p, index) => {
            const pId = String(p.id || p.ID);
            const pSku = p.sku || p.SKU || 'SKU-N/A';
            const pName = p.name || p.Name || p.productName || 'Unnamed Product';
            const pCategory = p.category || p.Category || p.categoryName || '';
            const pUnit = p.unit || p.Unit || p.unitCode || '';
            const isSelected = selectedProductId && String(selectedProductId) === pId;

            const optEl = document.createElement('div');
            optEl.className = `product-option-item ${isSelected ? 'selected' : ''}`;
            optEl.setAttribute('data-id', pId);
            optEl.setAttribute('data-index', index);
            optEl.setAttribute('role', 'option');
            optEl.setAttribute('aria-selected', isSelected ? 'true' : 'false');

            const skuHighlighted = highlightMatch(pSku, query);
            const nameHighlighted = highlightMatch(pName, query);
            const catHighlighted = pCategory ? highlightMatch(pCategory, query) : '';

            optEl.innerHTML = `
                <div class="option-left">
                    <div class="option-title-row">
                        <span class="product-sku-pill">${skuHighlighted}</span>
                        <span class="product-name-text">${nameHighlighted}</span>
                    </div>
                    <div class="option-meta-row">
                        ${pCategory ? `<span class="product-tag">${catHighlighted}</span>` : ''}
                        ${pUnit ? `<span>Unit: ${escapeHtml(pUnit)}</span>` : ''}
                    </div>
                </div>
                <span class="material-symbols-outlined option-check">check</span>
            `;

            optEl.addEventListener('click', () => {
                selectProduct(p);
                closeDropdown();
            });

            optionsListEl.appendChild(optEl);
        });

        highlightedIndex = -1;
    }

    function selectProduct(product) {
        if (!product) {
            clearProductSelection(true);
            return;
        }

        const pId = String(product.id || product.ID);
        const pSku = product.sku || product.SKU || '';
        const pName = product.name || product.Name || product.productName || '';

        selectedProductId = pId;
        selectedProductObj = product;

        if (hiddenProductId) {
            hiddenProductId.value = pId;
        }

        if (selectLabel) {
            selectLabel.innerHTML = `<strong>[${escapeHtml(pSku)}]</strong> ${escapeHtml(pName)}`;
            selectLabel.classList.add('has-value');
        }

        if (clearSelectionBtn) {
            clearSelectionBtn.style.display = 'flex';
        }
    }

    function clearProductSelection(updateUI = true) {
        selectedProductId = '';
        selectedProductObj = null;

        if (hiddenProductId) {
            hiddenProductId.value = '';
        }

        if (updateUI && selectLabel) {
            selectLabel.textContent = 'All Products in Location (Optional)';
            selectLabel.classList.remove('has-value');
        }

        if (clearSelectionBtn) {
            clearSelectionBtn.style.display = 'none';
        }
    }

    // ==========================================
    // 5. Open / Close / Keyboard Controls
    // ==========================================
    function openDropdown() {
        if (isDropdownOpen) return;
        isDropdownOpen = true;

        if (dropdownContainer) dropdownContainer.classList.add('open');
        if (dropdownPanel) dropdownPanel.style.display = 'block';
        if (selectTrigger) selectTrigger.setAttribute('aria-expanded', 'true');

        if (searchInput) {
            searchInput.value = '';
            if (searchClearBtn) searchClearBtn.style.display = 'none';
            filterAndRenderProducts();
            setTimeout(() => searchInput.focus(), 50);
        }
    }

    function closeDropdown() {
        if (!isDropdownOpen) return;
        isDropdownOpen = false;

        if (dropdownContainer) dropdownContainer.classList.remove('open');
        if (dropdownPanel) dropdownPanel.style.display = 'none';
        if (selectTrigger) selectTrigger.setAttribute('aria-expanded', 'false');
        highlightedIndex = -1;
    }

    function toggleDropdown() {
        if (isDropdownOpen) {
            closeDropdown();
        } else {
            openDropdown();
        }
    }

    if (selectTrigger) {
        selectTrigger.addEventListener('click', function(e) {
            // Prevent trigger click when clicking clear button
            if (e.target.closest('#productClearBtn')) {
                e.stopPropagation();
                return;
            }
            toggleDropdown();
        });

        selectTrigger.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' || e.key === ' ' || e.key === 'ArrowDown') {
                e.preventDefault();
                openDropdown();
            }
        });
    }

    if (clearSelectionBtn) {
        clearSelectionBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            clearProductSelection(true);
            filterAndRenderProducts();
        });
    }

    // Search Input Events
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            if (searchClearBtn) {
                searchClearBtn.style.display = this.value ? 'flex' : 'none';
            }
            filterAndRenderProducts();
        });

        searchInput.addEventListener('keydown', function(e) {
            const items = optionsListEl ? optionsListEl.querySelectorAll('.product-option-item, .product-option-default') : [];
            if (!items || items.length === 0) return;

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                highlightedIndex++;
                if (highlightedIndex >= items.length) highlightedIndex = 0;
                updateHighlight(items);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                highlightedIndex--;
                if (highlightedIndex < 0) highlightedIndex = items.length - 1;
                updateHighlight(items);
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (highlightedIndex >= 0 && items[highlightedIndex]) {
                    items[highlightedIndex].click();
                }
            } else if (e.key === 'Escape') {
                e.preventDefault();
                closeDropdown();
                if (selectTrigger) selectTrigger.focus();
            }
        });
    }

    function updateHighlight(items) {
        items.forEach((item, idx) => {
            if (idx === highlightedIndex) {
                item.classList.add('keyboard-active');
                item.scrollIntoView({ block: 'nearest' });
            } else {
                item.classList.remove('keyboard-active');
            }
        });
    }

    if (searchClearBtn) {
        searchClearBtn.addEventListener('click', function() {
            if (searchInput) {
                searchInput.value = '';
                this.style.display = 'none';
                searchInput.focus();
                filterAndRenderProducts();
            }
        });
    }

    // Close when clicking outside
    document.addEventListener('click', function(e) {
        if (dropdownContainer && !dropdownContainer.contains(e.target)) {
            closeDropdown();
        }
    });

    // ==========================================
    // 6. Cascading Location Scope
    // ==========================================
    async function updateDropdown(handler, paramName, paramValue, targetSelect, defaultOptionText, resetSelects = []) {
        resetSelects.forEach(id => {
            const select = document.getElementById(id);
            if (select) {
                const placeholder = select.getAttribute('data-placeholder') || 'All';
                select.innerHTML = `<option value="">${placeholder}</option>`;
                select.disabled = true;
            }
        });

        if (!targetSelect) return;

        if (!paramValue) {
            targetSelect.innerHTML = `<option value="">${defaultOptionText}</option>`;
            targetSelect.disabled = true;
            loadProducts(warehouseSelect?.value, floorSelect?.value, shelfSelect?.value);
            return;
        }

        const data = await fetchData(handler, paramName, paramValue);
        targetSelect.innerHTML = `<option value="">${defaultOptionText}</option>`;
        
        if (data && data.length > 0) {
            data.forEach(item => {
                const option = document.createElement('option');
                option.value = item.id !== undefined ? item.id : (item.ID !== undefined ? item.ID : '');
                option.textContent = item.name || item.Name || item.shelfCode || item.code || `ID ${option.value}`;
                targetSelect.appendChild(option);
            });
            targetSelect.disabled = false;
        } else {
            targetSelect.disabled = false;
        }
        
        loadProducts(warehouseSelect?.value, floorSelect?.value, shelfSelect?.value);
    }

    if (warehouseSelect) {
        warehouseSelect.addEventListener('change', async function() {
            const whId = this.value;
            await updateDropdown('Floors', 'warehouseId', whId, floorSelect, 'All Floors', ['NewCount_ShelfID']);
            
            // Also fetch shelves for the warehouse so staff can select shelf directly if desired
            if (whId && shelfSelect) {
                const shelfData = await fetchData('Shelves', 'warehouseId', whId);
                shelfSelect.innerHTML = '<option value="">All Shelves</option>';
                if (shelfData && shelfData.length > 0) {
                    shelfData.forEach(item => {
                        const opt = document.createElement('option');
                        opt.value = item.id !== undefined ? item.id : (item.ID !== undefined ? item.ID : '');
                        opt.textContent = item.name || item.Name || item.shelfCode || item.code || `ID ${opt.value}`;
                        shelfSelect.appendChild(opt);
                    });
                    shelfSelect.disabled = false;
                }
            }
        });
    }
    
    if (floorSelect) {
        floorSelect.addEventListener('change', async function() {
            const flId = this.value;
            if (flId) {
                await updateDropdown('Shelves', 'floorId', flId, shelfSelect, 'All Shelves');
            } else if (warehouseSelect && warehouseSelect.value) {
                // If floor cleared, reload shelves for the selected warehouse
                const shelfData = await fetchData('Shelves', 'warehouseId', warehouseSelect.value);
                shelfSelect.innerHTML = '<option value="">All Shelves</option>';
                if (shelfData && shelfData.length > 0) {
                    shelfData.forEach(item => {
                        const opt = document.createElement('option');
                        opt.value = item.id !== undefined ? item.id : (item.ID !== undefined ? item.ID : '');
                        opt.textContent = item.name || item.Name || item.shelfCode || item.code || `ID ${opt.value}`;
                        shelfSelect.appendChild(opt);
                    });
                    shelfSelect.disabled = false;
                }
            } else {
                shelfSelect.innerHTML = '<option value="">All Shelves</option>';
                shelfSelect.disabled = true;
                loadProducts(null, null, null);
            }
        });
    }

    if (shelfSelect) {
        shelfSelect.addEventListener('change', function() {
            loadProducts(warehouseSelect?.value, floorSelect?.value, this.value);
        });
    }

    async function initWarehouses() {
        const warehouses = await fetchData('Warehouses');
        
        // 1. Populate Create Count Modal Warehouse Select
        if (warehouseSelect) {
            warehouseSelect.innerHTML = '<option value="">Select Warehouse...</option>';
            if (warehouses && warehouses.length > 0) {
                warehouses.forEach(wh => {
                    const option = document.createElement('option');
                    option.value = wh.id !== undefined ? wh.id : (wh.ID !== undefined ? wh.ID : '');
                    option.textContent = wh.name || wh.Name || `Warehouse #${option.value}`;
                    warehouseSelect.appendChild(option);
                });
            }
        }

        // 2. Populate any other warehouse selects
        const whSelects = document.querySelectorAll('[data-type="warehouse"]');
        whSelects.forEach(select => {
            select.innerHTML = '<option value="">All Warehouses</option>';
            if (warehouses && warehouses.length > 0) {
                warehouses.forEach(wh => {
                    const option = document.createElement('option');
                    option.value = wh.id !== undefined ? wh.id : (wh.ID !== undefined ? wh.ID : '');
                    option.textContent = wh.name || wh.Name;
                    select.appendChild(option);
                });
            }
        });

        // Initial product load for all locations
        loadProducts(null, null, null);
    }

    initWarehouses();

    const createModalEl = document.getElementById('createCountModal');
    if (createModalEl) {
        createModalEl.addEventListener('show.bs.modal', function() {
            if (!warehouseSelect || warehouseSelect.options.length <= 1) {
                initWarehouses();
            }
        });
    }

    // ==========================================
    // 7. Form Submission Safety
    // ==========================================
    const countForm = document.getElementById('createCycleCountForm') || document.querySelector('#createCountModal form');
    if (countForm) {
        countForm.addEventListener('submit', function() {
            countForm.querySelectorAll('select').forEach(s => {
                s.disabled = false;
            });
        });
    }

    // ==========================================
    // 8. Main Page Table Filter Integration
    // ==========================================
    const tableSearchInput = document.querySelector('.filters-panel input[placeholder*="No, SKU"]');
    const tableWarehouseFilter = document.querySelector('.filters-panel select:nth-of-type(1)');
    const tableStatusFilter = document.querySelector('.filters-panel select:nth-of-type(2)');
    const tableTypeFilter = document.querySelector('.filters-panel select:nth-of-type(3)');
    const tableRows = document.querySelectorAll('.counts-table-panel tbody tr');

    function filterTable() {
        if (!tableRows || tableRows.length === 0) return;

        const searchVal = tableSearchInput ? tableSearchInput.value.toLowerCase().trim() : '';
        const whVal = tableWarehouseFilter && tableWarehouseFilter.value !== 'All Warehouses' ? tableWarehouseFilter.value.toLowerCase() : '';
        const statusVal = tableStatusFilter && tableStatusFilter.value !== 'All Statuses' ? tableStatusFilter.value.toLowerCase() : '';
        const typeVal = tableTypeFilter && tableTypeFilter.value !== 'All Types' ? tableTypeFilter.value.toLowerCase() : '';

        tableRows.forEach(row => {
            const text = row.textContent.toLowerCase();
            const matchesSearch = !searchVal || text.includes(searchVal);
            const matchesWh = !whVal || text.includes(whVal);
            const matchesStatus = !statusVal || text.includes(statusVal);
            const matchesType = !typeVal || text.includes(typeVal);

            if (matchesSearch && matchesWh && matchesStatus && matchesType) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
    }

    if (tableSearchInput) tableSearchInput.addEventListener('input', filterTable);
    if (tableWarehouseFilter) tableWarehouseFilter.addEventListener('change', filterTable);
    if (tableStatusFilter) tableStatusFilter.addEventListener('change', filterTable);
    if (tableTypeFilter) tableTypeFilter.addEventListener('change', filterTable);

    // ==========================================
    // 9. View Cycle Count Details Modal
    // ==========================================
    document.querySelectorAll('.js-view-count').forEach(btn => {
        btn.addEventListener('click', function() {
            const data = this.dataset;
            
            const setText = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.textContent = val || '-';
            };

            setText('detailCountNo', data.no);
            setText('detailCountStatus', data.status);
            setText('detailCountType', data.type);
            setText('detailCountProduct', data.product);
            setText('detailCountSku', data.sku);
            setText('detailCountShelf', data.shelf);
            setText('detailCountStaff', data.staff);
            setText('detailCountDate', data.date);
            setText('detailCountSystem', data.system);
            setText('detailCountPhysical', data.status === 'Scheduled' ? 'Not Counted Yet' : (data.counted || '0'));
            setText('detailCountSubtitle', `${data.product || '-'} (${data.sku || '-'})`);

            // Format variance
            const varianceEl = document.getElementById('detailCountVariance');
            if (varianceEl) {
                const numVar = parseFloat(data.variance) || 0;
                if (data.status === 'Scheduled' || data.status === 'In Progress') {
                    varianceEl.textContent = 'Pending Count';
                    varianceEl.className = 'text-muted';
                } else if (numVar > 0) {
                    varianceEl.textContent = `+${numVar} (Surplus)`;
                    varianceEl.className = 'text-success font-mono fw-bold';
                } else if (numVar < 0) {
                    varianceEl.textContent = `${numVar} (Deficit)`;
                    varianceEl.className = 'text-danger font-mono fw-bold';
                } else {
                    varianceEl.textContent = '0 (Matched)';
                    varianceEl.className = 'text-secondary font-mono';
                }
            }

            // Action section in modal
            const actionDesc = document.getElementById('detailActionDescription');
            const actionButtons = document.getElementById('detailActionButtons');

            if (actionDesc && actionButtons) {
                actionButtons.innerHTML = '';

                if (data.status === 'Variance Found') {
                    actionDesc.textContent = `Discrepancy of ${data.variance} units identified. An adjustment request can be generated.`;
                    actionButtons.innerHTML = `
                        <form method="post" action="?handler=CreateAdjustment" style="display:inline;">
                            <input type="hidden" name="countId" value="${data.countId}" />
                            <button type="submit" class="btn btn-warning btn-sm d-inline-flex align-items-center gap-1 font-weight-bold" style="background-color: #f59e0b; color: white; border: none; padding: 0.4rem 0.8rem; border-radius: 4px;">
                                <span class="material-symbols-outlined" style="font-size: 18px;">edit_square</span>
                                Create Adjustment
                            </button>
                        </form>
                    `;
                } else if (data.status === 'Scheduled') {
                    actionDesc.textContent = `Audit is scheduled for ${data.date}. You can start the count now.`;
                    actionButtons.innerHTML = `
                        <form method="post" action="?handler=StartCount" style="display:inline;">
                            <input type="hidden" name="countId" value="${data.countId}" />
                            <button type="submit" class="btn btn-primary btn-sm d-inline-flex align-items-center gap-1 font-weight-bold" style="background-color: #0058be; color: white; border: none; padding: 0.4rem 0.8rem; border-radius: 4px;">
                                <span class="material-symbols-outlined" style="font-size: 18px;">play_circle</span>
                                Start Count
                            </button>
                        </form>
                    `;
                } else if (data.status === 'In Progress') {
                    actionDesc.textContent = `Count is currently active. Enter the physical counted quantity in the table.`;
                } else if (data.status === 'Completed') {
                    actionDesc.textContent = `Cycle count is completed and reconciled with inventory stock.`;
                }
            }
        });
    });
});
