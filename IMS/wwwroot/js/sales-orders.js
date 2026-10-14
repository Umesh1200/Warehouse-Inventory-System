document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('salesOrderSearch');
    const statusFilter = document.getElementById('salesStatusFilter');
    const rows = Array.from(document.querySelectorAll('.order-row'));

    const applyFilters = () => {
        const query = (searchInput?.value || '').trim().toLowerCase();
        const status = statusFilter?.value || '';

        rows.forEach((row) => {
            const matchesQuery = !query || (row.dataset.search || '').toLowerCase().includes(query);
            const matchesStatus = !status || row.dataset.status === status;
            row.hidden = !(matchesQuery && matchesStatus);
        });
    };

    searchInput?.addEventListener('input', applyFilters);
    statusFilter?.addEventListener('change', applyFilters);

    // Dynamic Multi-Item Ordering Support
    const activeProducts = window.activeProducts || [];

    const getProductOptionsHtml = () => {
        let html = '<option value="">Select product</option>';
        activeProducts.forEach(p => {
            const id = p.ID !== undefined ? p.ID : p.id;
            const sku = p.SKU !== undefined ? p.SKU : p.sku;
            const name = p.Name !== undefined ? p.Name : p.name;
            html += `<option value="${id}">[${sku}] ${name}</option>`;
        });
        return html;
    };

    const reindexRows = (tbody, listPrefix) => {
        const itemRows = tbody.querySelectorAll('.item-row');
        itemRows.forEach((row, index) => {
            const selectEl = row.querySelector('.select-product');
            const qtyEl = row.querySelector('.input-qty');
            const priceEl = row.querySelector('.input-price');
            
            if (selectEl) selectEl.name = `${listPrefix}[${index}].ProductID`;
            if (qtyEl) qtyEl.name = `${listPrefix}[${index}].OrderedQty`;
            if (priceEl) priceEl.name = `${listPrefix}[${index}].UnitPrice`;
            
            const deleteBtn = row.querySelector('.btn-delete-row');
            if (deleteBtn) {
                deleteBtn.disabled = itemRows.length === 1;
            }
        });
    };

    // Create Modal Items Table Handler
    const createTbody = document.getElementById('createItemsTbody');
    const btnAddCreateRow = document.getElementById('btnAddCreateRow');

    if (btnAddCreateRow && createTbody) {
        btnAddCreateRow.addEventListener('click', () => {
            const rowCount = createTbody.querySelectorAll('.item-row').length;
            const newRow = document.createElement('tr');
            newRow.className = 'item-row';
            newRow.innerHTML = `
                <td>
                    <select name="NewSalesOrder.Items[${rowCount}].ProductID" class="form-select select-product" required>
                        ${getProductOptionsHtml()}
                    </select>
                </td>
                <td>
                    <input type="number" step="0.01" name="NewSalesOrder.Items[${rowCount}].OrderedQty" class="form-control input-qty" required />
                </td>
                <td>
                    <input type="number" step="0.01" name="NewSalesOrder.Items[${rowCount}].UnitPrice" class="form-control input-price" required />
                </td>
                <td class="text-center">
                    <button type="button" class="btn btn-sm btn-link text-danger btn-delete-row">
                        <span class="material-symbols-outlined">delete</span>
                    </button>
                </td>
            `;

            newRow.querySelector('.btn-delete-row').addEventListener('click', () => {
                newRow.remove();
                reindexRows(createTbody, 'NewSalesOrder.Items');
            });

            createTbody.appendChild(newRow);
            reindexRows(createTbody, 'NewSalesOrder.Items');
        });

        createTbody.querySelectorAll('.btn-delete-row').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const row = e.target.closest('.item-row');
                if (row) {
                    row.remove();
                    reindexRows(createTbody, 'NewSalesOrder.Items');
                }
            });
        });
    }

    // Edit Modal Items Table Handler
    const editTbody = document.getElementById('editItemsTbody');
    const btnAddEditRow = document.getElementById('btnAddEditRow');

    const addEditRow = (productId = '', qty = '', price = '') => {
        if (!editTbody) return;
        const rowCount = editTbody.querySelectorAll('.item-row').length;
        const newRow = document.createElement('tr');
        newRow.className = 'item-row';
        newRow.innerHTML = `
            <td>
                <select name="EditSalesOrder.Items[${rowCount}].ProductID" class="form-select select-product" required>
                    ${getProductOptionsHtml()}
                </select>
            </td>
            <td>
                <input type="number" step="0.01" name="EditSalesOrder.Items[${rowCount}].OrderedQty" value="${qty}" class="form-control input-qty" required />
            </td>
            <td>
                <input type="number" step="0.01" name="EditSalesOrder.Items[${rowCount}].UnitPrice" value="${price}" class="form-control input-price" required />
            </td>
            <td class="text-center">
                <button type="button" class="btn btn-sm btn-link text-danger btn-delete-row">
                    <span class="material-symbols-outlined">delete</span>
                </button>
            </td>
        `;

        const select = newRow.querySelector('.select-product');
        if (select) select.value = productId;

        newRow.querySelector('.btn-delete-row').addEventListener('click', () => {
            newRow.remove();
            reindexRows(editTbody, 'EditSalesOrder.Items');
        });

        editTbody.appendChild(newRow);
        reindexRows(editTbody, 'EditSalesOrder.Items');
    };

    window.addEditRow = addEditRow;

    if (btnAddEditRow) {
        btnAddEditRow.addEventListener('click', () => {
            addEditRow();
        });
    }

    document.querySelectorAll('.edit-btn').forEach((button) => {
        button.addEventListener('click', () => {
            document.getElementById('edit-id').value = button.dataset.id || '';
            document.getElementById('edit-so').value = button.dataset.so || '';
            document.getElementById('edit-customer').value = button.dataset.customer || '';
            document.getElementById('edit-orderdate').value = button.dataset.orderdate || '';
            document.getElementById('edit-requireddate').value = button.dataset.requireddate || '';
            document.getElementById('edit-remarks').value = button.dataset.remarks || '';

            if (editTbody) {
                editTbody.innerHTML = '';
            }

            let items = [];
            try {
                items = JSON.parse(button.dataset.items || '[]');
            } catch (e) {
                console.error('Error parsing items DTO:', e);
            }

            if (items.length > 0) {
                items.forEach(item => {
                    const id = item.ProductID !== undefined ? item.ProductID : (item.productId || '');
                    const qty = item.QtyOrdered !== undefined ? item.QtyOrdered : (item.qtyOrdered || '');
                    const price = item.UnitPrice !== undefined ? item.UnitPrice : (item.unitPrice || '');
                    addEditRow(id, qty, price);
                });
            } else {
                addEditRow();
            }
        });
    });

    document.querySelectorAll('form[data-confirm-message]').forEach((form) => {
        form.addEventListener('submit', (event) => {
            if (!window.confirm(form.dataset.confirmMessage || 'Confirm this action?')) {
                event.preventDefault();
            }
        });
    });

    // Stock Location Details Collapsible Toggle Handler
    document.addEventListener('click', (e) => {
        const toggleBtn = e.target.closest('.toggle-locations-btn');
        if (toggleBtn) {
            const detailsDiv = toggleBtn.nextElementSibling;
            if (detailsDiv) {
                detailsDiv.classList.toggle('d-none');
                const isHidden = detailsDiv.classList.contains('d-none');
                const iconSpan = toggleBtn.querySelector('.material-symbols-outlined');
                if (iconSpan) {
                    iconSpan.textContent = isHidden ? 'location_on' : 'arrow_drop_down';
                }
            }
        }
    });
});
