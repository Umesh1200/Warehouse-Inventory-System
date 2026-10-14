document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('poSearch');
    const supplierFilter = document.getElementById('supplierFilter');
    const statusFilter = document.getElementById('statusFilter');
    const poList = document.getElementById('poList');
    const poListItems = document.getElementById('poListItems');
    const poCountLabel = document.getElementById('poCountLabel');
    const poEmptyState = document.getElementById('poEmptyState');
    const poListLoader = document.getElementById('poListLoader');
    const supplierProducts = Array.isArray(window.purchaseOrderSupplierProducts) ? window.purchaseOrderSupplierProducts : [];

    const urlParams = new URLSearchParams(window.location.search);
    const selectedPoId = parseInt(urlParams.get('poid')) || 0;

    let totalCount = parseInt(poCountLabel?.dataset.totalCount) || 0;
    let offset = Math.min(50, totalCount);
    const limit = 50;
    let isLoading = false;

    const normalize = (value) => (value || '').toString().trim().toLowerCase();
    const escapeHtml = (value) => String(value || '').replace(/[&<>"']/g, (char) => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[char]));

    const updateCountLabel = () => {
        if (!poCountLabel) return;
        const loadedCount = poListItems ? poListItems.querySelectorAll('.order-row').length : 0;
        if (totalCount > 0) {
            poCountLabel.innerHTML = `Showing 1-${loadedCount} of ${totalCount}`;
        } else {
            poCountLabel.innerHTML = 'Showing 0 of 0';
        }
    };

    const renderPoRowHtml = (po) => {
        const isSelected = po.poid === selectedPoId;
        const statusClass = po.status.toLowerCase().replace(/\s+/g, '');
        const progressPercent = po.progressPercent;
        const totalReceivedQty = Number(po.totalReceivedQty).toFixed(2);
        const totalOrderedQty = Number(po.totalOrderedQty).toFixed(2);

        return `
            <a class="order-row ${isSelected ? 'selected' : ''}"
               href="/Purchasing/PurchaseOrders?poid=${po.poid}"
               data-search="${escapeHtml(`${po.poNumber} ${po.supplier} ${po.remarks}`)}"
               data-supplier="${escapeHtml(po.supplier)}"
               data-status="${escapeHtml(po.status)}">
                <div class="row-main">
                    <div>
                        <div class="row-title">${escapeHtml(po.poNumber)}</div>
                        <div class="row-subtitle">${escapeHtml(po.supplier)}</div>
                    </div>
                    <span class="status-pill status-${statusClass}">${escapeHtml(po.status)}</span>
                </div>
                <div class="row-meta">
                    <span>${escapeHtml(po.orderDate)}</span>
                    <span>${escapeHtml(po.totalAmount)}</span>
                </div>
                <div class="progress-line" aria-label="Received ${progressPercent} percent">
                    <span style="width:${progressPercent}%"></span>
                </div>
                <div class="row-foot">
                    <span>${po.itemCount} items</span>
                    <span>${totalReceivedQty} / ${totalOrderedQty} received</span>
                </div>
            </a>
        `;
    };

    const loadMoreOrders = async () => {
        if (isLoading) return;
        isLoading = true;
        if (poListLoader) poListLoader.style.display = 'block';

        const search = searchInput?.value || '';
        const supplier = supplierFilter?.value || 'All Suppliers';
        const status = statusFilter?.value || 'All Statuses';

        try {
            const url = `/Purchasing/PurchaseOrders?handler=Orders&offset=${offset}&limit=${limit}&search=${encodeURIComponent(search)}&supplier=${encodeURIComponent(supplier)}&status=${encodeURIComponent(status)}`;
            const response = await fetch(url);
            if (!response.ok) throw new Error('Network response was not ok');
            const data = await response.json();

            if (data.success) {
                totalCount = data.totalCount;
                if (poCountLabel) {
                    poCountLabel.dataset.totalCount = totalCount;
                }
                const orders = data.orders || [];

                if (offset === 0 && poListItems) {
                    poListItems.innerHTML = '';
                }

                if (orders.length > 0 && poListItems) {
                    const html = orders.map(renderPoRowHtml).join('');
                    poListItems.insertAdjacentHTML('beforeend', html);
                    offset += orders.length;
                    if (poEmptyState) poEmptyState.style.display = 'none';
                } else if (offset === 0 && poEmptyState) {
                    poEmptyState.style.display = 'block';
                }

                updateCountLabel();
            }
        } catch (error) {
            console.error('Error loading purchase orders:', error);
        } finally {
            isLoading = false;
            if (poListLoader) poListLoader.style.display = 'none';
        }
    };

    const reloadOrders = () => {
        offset = 0;
        totalCount = 0;
        loadMoreOrders();
    };

    let debounceTimer;
    searchInput?.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            reloadOrders();
        }, 300);
    });

    [supplierFilter, statusFilter].forEach((control) => {
        control?.addEventListener('change', () => {
            reloadOrders();
        });
    });

    poList?.addEventListener('scroll', () => {
        const scrollThreshold = 80; // px from bottom
        if (poList.scrollTop + poList.clientHeight >= poList.scrollHeight - scrollThreshold) {
            if (offset < totalCount && !isLoading) {
                loadMoreOrders();
            }
        }
    });

    const productComboboxes = new Map();

    const buildProductOptions = (select) => {
        const options = [];
        select.querySelectorAll('option[value]').forEach((option) => {
            if (!option.value) return;

            options.push({
                value: option.value,
                label: option.textContent.trim(),
                group: option.parentElement?.tagName === 'OPTGROUP' ? option.parentElement.label : 'Products',
                search: normalize(`${option.dataset.search || ''} ${option.textContent || ''}`),
                type: option.dataset.type || '',
                category: option.dataset.category || '',
                supplierScope: option.dataset.supplierScope || 'all'
            });
        });
        return options;
    };

    const fieldValue = (source, ...names) => {
        for (const name of names) {
            if (source && source[name] !== undefined && source[name] !== null) {
                return source[name];
            }
        }
        return '';
    };

    const mappingFor = (supplierId, productId) => supplierProducts.find((mapping) => {
        const mappingSupplierId = fieldValue(mapping, 'supplierId', 'supplierID', 'SupplierID');
        const mappingProductId = fieldValue(mapping, 'productId', 'productID', 'ProductID');
        const status = normalize(fieldValue(mapping, 'status', 'Status'));
        return String(mappingSupplierId) === String(supplierId) && 
               String(mappingProductId) === String(productId) &&
               status !== 'blocked' && status !== 'inactive';
    });

    const approvedForSupplier = (state, option) => {
        if (!state.supplierSelect?.value) return false;
        return Boolean(mappingFor(state.supplierSelect.value, option.value));
    };

    const renderProductResults = (state) => {
        const query = normalize(state.input.value);
        const tokens = query.split(/\s+/).filter(Boolean);
        const supplierSelected = Boolean(state.supplierSelect?.value);
        const matches = state.options
            .filter((option) => supplierSelected && approvedForSupplier(state, option))
            .filter((option) => tokens.every((token) => option.search.includes(token)))
            .slice(0, 60);

        if (!supplierSelected) {
            state.panel.innerHTML = '<div class="po-combobox-empty">Select a supplier first to see approved products.</div>';
            state.panel.hidden = false;
            return;
        }

        if (!matches.length) {
            state.panel.innerHTML = '<div class="po-combobox-empty">No approved products match this supplier/search.</div>';
            state.panel.hidden = false;
            return;
        }

        let currentGroup = '';
        state.panel.innerHTML = matches.map((option) => {
            const groupHeader = option.group !== currentGroup
                ? `<div class="po-combobox-group">${escapeHtml(option.group)}</div>`
                : '';
            currentGroup = option.group;
            const mapping = mappingFor(state.supplierSelect.value, option.value);
            const price = mapping && fieldValue(mapping, 'lastQuotedPrice', 'LastQuotedPrice') != null 
                ? `NPR ${Number(fieldValue(mapping, 'lastQuotedPrice', 'LastQuotedPrice')).toLocaleString()}` 
                : 'No quote';
            const leadTime = mapping && fieldValue(mapping, 'leadTimeDays', 'LeadTimeDays') 
                ? `${fieldValue(mapping, 'leadTimeDays', 'LeadTimeDays')}d lead` 
                : 'Lead TBD';

            return `${groupHeader}
                <button type="button" class="po-combobox-option" data-value="${escapeHtml(option.value)}">
                    <span>${escapeHtml(option.label)}</span>
                    <small>${escapeHtml([option.type, option.category, price, leadTime].filter(Boolean).join(' / '))}</small>
                </button>`;
        }).join('');
        state.panel.hidden = false;
    };

    const setProductSelection = (state, value) => {
        const option = state.options.find((item) => item.value === value);
        state.select.value = value || '';
        state.input.value = option?.label || '';
        state.panel.hidden = true;
        applySupplierProductDefaults(state, value);
        state.select.dispatchEvent(new Event('change', { bubbles: true }));
    };

    const applySupplierProductDefaults = (state, productId) => {
        const mapping = mappingFor(state.supplierSelect?.value, productId);
        const td = state.select.closest('td');
        const note = td?.querySelector('.product-select-note');

        if (note) {
            if (!state.supplierSelect?.value) {
                note.textContent = 'Select a supplier to show approved products.';
            } else if (mapping) {
                const bits = [
                    fieldValue(mapping, 'supplierSku', 'SupplierSKU') ? `Supplier SKU: ${fieldValue(mapping, 'supplierSku', 'SupplierSKU')}` : '',
                    fieldValue(mapping, 'lastQuotedPrice', 'LastQuotedPrice') != null ? `Quote: NPR ${Number(fieldValue(mapping, 'lastQuotedPrice', 'LastQuotedPrice')).toLocaleString()}` : '',
                    fieldValue(mapping, 'leadTimeDays', 'LeadTimeDays') ? `Lead time: ${fieldValue(mapping, 'leadTimeDays', 'LeadTimeDays')} day(s)` : '',
                    fieldValue(mapping, 'minOrderQty', 'MinOrderQty') ? `MOQ: ${Number(fieldValue(mapping, 'minOrderQty', 'MinOrderQty')).toLocaleString()}` : '',
                    fieldValue(mapping, 'packSize', 'PackSize') ? `Pack: ${Number(fieldValue(mapping, 'packSize', 'PackSize')).toLocaleString()}` : ''
                ].filter(Boolean);
                note.textContent = bits.join(' | ') || 'Approved supplier-product mapping found.';
            } else {
                note.textContent = 'This product is not approved for the selected supplier.';
            }
        }
    };

    const initComboboxForSelect = (select, supplierSelect) => {
        if (productComboboxes.has(select.id)) return productComboboxes.get(select.id);

        const state = {
            select,
            options: buildProductOptions(select),
            supplierSelect: supplierSelect,
            input: document.createElement('input'),
            panel: document.createElement('div')
        };

        state.input.type = 'text';
        state.input.className = 'po-combobox-input';
        state.input.autocomplete = 'off';
        state.input.setAttribute('role', 'combobox');
        state.input.setAttribute('aria-autocomplete', 'list');
        state.panel.className = 'po-combobox-panel';
        state.panel.hidden = true;

        const hasSupplier = Boolean(supplierSelect?.value);
        state.input.disabled = !hasSupplier;
        state.input.placeholder = hasSupplier
            ? 'Search by SKU, product, type, or category'
            : 'Select a supplier first to see approved products.';
        if (!hasSupplier) {
            state.input.classList.add('disabled');
        }

        const wrapper = document.createElement('div');
        wrapper.className = 'po-combobox';
        wrapper.appendChild(state.input);
        wrapper.appendChild(state.panel);
        select.insertAdjacentElement('beforebegin', wrapper);
        select.classList.add('visually-hidden-select');

        state.input.addEventListener('input', () => renderProductResults(state));
        state.input.addEventListener('focus', () => renderProductResults(state));
        state.input.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                state.panel.hidden = true;
                return;
            }
            if (event.key === 'Enter') {
                event.preventDefault();
                const firstOption = state.panel.querySelector('.po-combobox-option');
                if (firstOption) {
                    setProductSelection(state, firstOption.dataset.value);
                } else {
                    renderProductResults(state);
                }
            }
        });

        state.panel.addEventListener('click', (event) => {
            const optionButton = event.target.closest('.po-combobox-option');
            if (!optionButton) return;
            setProductSelection(state, optionButton.dataset.value);
        });

        productComboboxes.set(select.id, state);
        return state;
    };

    const updateRowTotal = (row) => {
        const qty = parseFloat(row.querySelector('.row-qty').value) || 0;
        const price = parseFloat(row.querySelector('.row-price').value) || 0;
        const totalSpan = row.querySelector('.row-total');
        const lineTotal = qty * price;
        totalSpan.textContent = `Rs. ${lineTotal.toFixed(2)}`;
        recalculatePOTotal(row.closest('form'));
    };

    const reindexRows = (tbody, prefix) => {
        const rows = tbody.querySelectorAll('.po-item-row');
        rows.forEach((row, index) => {
            const select = row.querySelector('.po-product-select');
            if (select) {
                select.name = `${prefix}.Items[${index}].ProductID`;
            }
            const qtyInput = row.querySelector('.row-qty');
            if (qtyInput) {
                qtyInput.name = `${prefix}.Items[${index}].OrderedQty`;
            }
            const priceInput = row.querySelector('.row-price');
            if (priceInput) {
                priceInput.name = `${prefix}.Items[${index}].UnitPrice`;
            }
        });
    };

    const recalculatePOTotal = (form) => {
        if (!form) return;
        const rows = form.querySelectorAll('.po-item-row');
        let totalQty = 0;
        let totalAmount = 0;
        
        rows.forEach((row) => {
            const qty = parseFloat(row.querySelector('.row-qty').value) || 0;
            const price = parseFloat(row.querySelector('.row-price').value) || 0;
            totalQty += qty;
            totalAmount += qty * price;
        });

        const totalQtySpan = form.querySelector('.po-total-qty');
        const totalAmountSpan = form.querySelector('.po-total-amount');
        if (totalQtySpan) totalQtySpan.textContent = totalQty.toFixed(2);
        if (totalAmountSpan) totalAmountSpan.textContent = `Rs. ${totalAmount.toFixed(2)}`;
    };

    const addRow = (tbody, prefix, supplierSelect, productId = '', qty = 1, price = 0.00) => {
        const template = document.getElementById('po-row-template');
        if (!template) return;

        const index = tbody.querySelectorAll('.po-item-row').length;
        const tempNode = template.content.cloneNode(true);
        const row = tempNode.querySelector('.po-item-row');

        const select = row.querySelector('.po-product-select-template');
        select.className = 'po-product-select';
        select.id = `${prefix}-select-${Date.now()}-${index}`;
        select.name = `${prefix}.Items[${index}].ProductID`;

        const qtyInput = row.querySelector('.row-qty');
        qtyInput.name = `${prefix}.Items[${index}].OrderedQty`;
        qtyInput.value = qty;

        const priceInput = row.querySelector('.row-price');
        priceInput.name = `${prefix}.Items[${index}].UnitPrice`;
        priceInput.value = price;

        tbody.appendChild(row);

        const state = initComboboxForSelect(select, supplierSelect);

        if (productId) {
            setProductSelection(state, productId.toString());
            if (price > 0) {
                priceInput.value = parseFloat(price).toFixed(2);
            }
        }

        qtyInput.addEventListener('input', () => updateRowTotal(row));
        priceInput.addEventListener('input', () => updateRowTotal(row));
        select.addEventListener('change', () => {
            const val = select.value;
            const mapping = mappingFor(supplierSelect?.value, val);
            if (mapping && fieldValue(mapping, 'lastQuotedPrice', 'LastQuotedPrice') != null) {
                priceInput.value = Number(fieldValue(mapping, 'lastQuotedPrice', 'LastQuotedPrice')).toFixed(2);
            }
            updateRowTotal(row);
        });

        row.querySelector('.remove-row-btn').addEventListener('click', () => {
            row.remove();
            reindexRows(tbody, prefix);
            recalculatePOTotal(tbody.closest('form'));
        });

        updateRowTotal(row);
    };

    const editForm = document.querySelector('#editPoModal form');
    const editSupplierSelect = document.getElementById('edit-supplier');
    const editTbody = document.getElementById('edit-po-items-tbody');

    document.querySelectorAll('.edit-btn').forEach((button) => {
        button.addEventListener('click', () => {
            document.getElementById('edit-id').value = button.dataset.id || '';
            document.getElementById('edit-po').value = button.dataset.po || '';
            
            if (editSupplierSelect) {
                editSupplierSelect.value = button.dataset.supplier || '';
            }
            
            document.getElementById('edit-orderdate').value = button.dataset.orderdate || '';
            document.getElementById('edit-expecteddate').value = button.dataset.expecteddate || '';
            document.getElementById('edit-remarks').value = button.dataset.remarks || '';

            if (editTbody && editSupplierSelect) {
                editTbody.innerHTML = '';
                const items = JSON.parse(button.dataset.items || '[]');
                items.forEach((item) => {
                    addRow(editTbody, 'EditPurchaseOrder', editSupplierSelect, item.productID, item.orderedQty, item.unitPrice);
                });
                if (items.length === 0) {
                    addRow(editTbody, 'EditPurchaseOrder', editSupplierSelect);
                }
                recalculatePOTotal(editForm);
            }
        });
    });

    const addCreateRowBtn = document.getElementById('add-create-row-btn');
    const createSupplierSelect = document.getElementById('create-po-supplier');
    const createTbody = document.getElementById('create-po-items-tbody');
    if (addCreateRowBtn && createTbody && createSupplierSelect) {
        addCreateRowBtn.addEventListener('click', () => {
            addRow(createTbody, 'NewPurchaseOrder', createSupplierSelect);
        });
    }

    const addEditRowBtn = document.getElementById('add-edit-row-btn');
    if (addEditRowBtn && editTbody && editSupplierSelect) {
        addEditRowBtn.addEventListener('click', () => {
            addRow(editTbody, 'EditPurchaseOrder', editSupplierSelect);
        });
    }

    document.querySelectorAll('.confirm-form').forEach((form) => {
        form.addEventListener('submit', (event) => {
            const message = form.dataset.message || 'Continue?';
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    document.addEventListener('click', (event) => {
        productComboboxes.forEach((state) => {
            if (!state.panel.parentElement.contains(event.target)) {
                state.panel.hidden = true;
            }
        });
    });

    document.querySelectorAll('.po-supplier-select').forEach((supplierSelect) => {
        supplierSelect.addEventListener('change', () => {
            const form = supplierSelect.closest('form');
            if (!form) return;

            const prefix = form.querySelector('input[name="EditPurchaseOrder.POID"]') ? 'EditPurchaseOrder' : 'NewPurchaseOrder';
            const tbody = form.querySelector('tbody');

            if (tbody) {
                tbody.querySelectorAll('.po-product-select').forEach((select) => {
                    productComboboxes.delete(select.id);
                });

                tbody.innerHTML = '';
                addRow(tbody, prefix, supplierSelect);
                recalculatePOTotal(form);
            }
        });
    });

    document.querySelectorAll('.sparkline').forEach((sparkline) => {
        const values = (sparkline.dataset.points || '')
            .split(',')
            .map((value) => Number.parseFloat(value))
            .filter((value) => Number.isFinite(value));

        if (!values.length) {
            sparkline.innerHTML = '';
            return;
        }

        const width = 180;
        const height = 34;
        const max = Math.max(...values, 1);
        const step = values.length > 1 ? width / (values.length - 1) : width;
        const points = values
            .map((value, index) => {
                const x = index * step;
                const y = height - (value / max) * (height - 6) - 3;
                return `${x},${y}`;
            })
            .join(' ');

        sparkline.innerHTML = `
            <svg viewBox="0 0 ${width} ${height}" role="img" aria-label="Seven day purchase order trend" preserveAspectRatio="none">
                <polyline points="${points}" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"></polyline>
            </svg>
        `;
    });

    updateCountLabel();
});
