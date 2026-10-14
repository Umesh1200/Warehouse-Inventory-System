document.addEventListener('DOMContentLoaded', () => {
    // 1. Component search and filtering
    const searchInput = document.getElementById('componentSearch');
    const rows = Array.from(document.querySelectorAll('.component-table tbody tr'));
    const visibleCount = document.getElementById('visibleComponentCount');

    const normalize = (value) => (value || '').toString().trim().toLowerCase();

    const filterComponents = () => {
        const query = normalize(searchInput?.value);
        let visible = 0;

        rows.forEach((row) => {
            const haystack = normalize(row.dataset.component);
            const shouldShow = !query || haystack.includes(query);
            row.style.display = shouldShow ? '' : 'none';
            if (shouldShow) {
                visible += 1;
            }
        });

        if (visibleCount) {
            visibleCount.textContent = visible.toString();
        }
    };

    searchInput?.addEventListener('input', filterComponents);
    filterComponents();
});

// Modal selectors
const createBomModal = document.getElementById('createBomModal');
const orderShortagesModal = document.getElementById('orderShortagesModal');
let bomRowCount = 0;

// Create BOM Modal Toggle
window.openCreateBomModal = function() {
    createBomModal.classList.add('open');
    document.getElementById('bomItemsTableBody').innerHTML = '';
    bomRowCount = 0;
    // Add one empty component row by default for user convenience
    addComponentRow();
};

window.closeCreateBomModal = function() {
    createBomModal.classList.remove('open');
};

// Order Shortages Modal Toggle
window.openOrderShortagesModal = function() {
    orderShortagesModal.classList.add('open');
};

window.closeOrderShortagesModal = function() {
    orderShortagesModal.classList.remove('open');
};

// Dynamic BOM Component Row Operations
window.addComponentRow = function() {
    const tableBody = document.getElementById('bomItemsTableBody');
    const categoriesHtml = document.getElementById('categoriesOptionsTemplate').innerHTML;
    const unitsHtml = document.getElementById('unitsOptionsTemplate').innerHTML;
    const rawMaterialsHtml = document.getElementById('rawMaterialsOptionsTemplate').innerHTML;
    const index = bomRowCount;

    const rowHtml = `
        <tr class="bom-item-row" id="bomItemRow_${index}">
            <td>
                <div class="existing-material-group" id="existingMaterialGroup_${index}" style="display: flex; gap: 8px; align-items: center;">
                    <select class="form-control item-product-select" name="NewBom.Items[${index}].ProductID" required onchange="onItemProductChange(this, ${index})" style="flex: 1;">
                        <option value="">-- Select Material --</option>
                        ${rawMaterialsHtml}
                        <option value="new-material-flag" style="color: var(--bom-blue); font-weight: bold;">+ Create New Raw Material</option>
                    </select>
                    <button type="button" class="btn-secondary" style="min-height: 40px; padding: 0 10px; display: inline-flex; align-items: center; justify-content: center; flex-shrink: 0;" title="Create New Raw Material" onclick="toggleNewMaterialForm(${index})">
                        <span class="material-symbols-outlined">add</span>
                    </button>
                </div>
                <div class="new-material-fields" id="newMaterialFields_${index}" style="display: none; margin-top: 10px;">
                    <div class="new-material-form">
                        <div class="form-group">
                            <label>SKU *</label>
                            <input type="text" class="form-control" name="NewBom.Items[${index}].NewMaterialSKU" placeholder="e.g. RM-001" />
                        </div>
                        <div class="form-group">
                            <label>Name *</label>
                            <input type="text" class="form-control" name="NewBom.Items[${index}].NewMaterialName" placeholder="e.g. Copper Wire" />
                        </div>
                        <div class="form-group">
                            <label>Category</label>
                            <input type="text" class="form-control" value="Raw Material" readonly style="background-color: #f1f5f9; cursor: not-allowed;" />
                        </div>
                        <div class="form-group">
                            <label>Unit *</label>
                            <select class="form-control" name="NewBom.Items[${index}].NewMaterialUnitID">
                                <option value="">-- Unit --</option>
                                ${unitsHtml}
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Standard Cost (NPR) *</label>
                            <input type="number" step="0.01" class="form-control" value="0.00" name="NewBom.Items[${index}].NewMaterialStandardCost" />
                        </div>
                        <div class="form-group full-width">
                            <label>Description / Notes *</label>
                            <input type="text" class="form-control" name="NewBom.Items[${index}].NewMaterialNotes" placeholder="Notes for PO / stock master" />
                        </div>
                        <div class="form-group full-width text-right" style="margin-top: 4px; margin-bottom: 0;">
                            <a href="#" style="font-size: 11px; color: var(--bom-blue); font-weight: bold; text-decoration: none;" onclick="cancelNewMaterial(event, ${index})">← Select Existing Material</a>
                        </div>
                    </div>
                </div>
            </td>
            <td>
                <input type="number" step="0.01" class="form-control" name="NewBom.Items[${index}].RequiredQty" placeholder="0.00" required />
            </td>
            <td>
                <input type="number" step="0.1" class="form-control" name="NewBom.Items[${index}].WastagePercent" value="0.0" required />
            </td>
            <td class="text-right">
                <button type="button" class="btn-danger-link" onclick="removeComponentRow(${index})">
                    <span class="material-symbols-outlined">delete</span> Remove
                </button>
            </td>
        </tr>
    `;

    tableBody.insertAdjacentHTML('beforeend', rowHtml);
    bomRowCount++;
};

window.removeComponentRow = function(index) {
    const row = document.getElementById(`bomItemRow_${index}`);
    if (row) {
        row.remove();
    }
};

window.onItemProductChange = function(select, index) {
    if (select.value === 'new-material-flag') {
        const fieldsContainer = document.getElementById(`newMaterialFields_${index}`);
        fieldsContainer.style.display = 'block';

        const existingGroup = document.getElementById(`existingMaterialGroup_${index}`);
        existingGroup.style.display = 'none';

        // Set inputs to required
        toggleInputsRequired(fieldsContainer, true);

        // Create a hidden input for ProductID with value "0" to indicate it's a new product
        const hiddenInput = document.createElement('input');
        hiddenInput.type = 'hidden';
        hiddenInput.name = `NewBom.Items[${index}].ProductID`;
        hiddenInput.value = '0';
        hiddenInput.id = `hiddenProductID_${index}`;
        existingGroup.appendChild(hiddenInput);

        // Disable select so model binder doesn't see "new-material-flag" as ProductID
        select.disabled = true;
    }
};

window.cancelNewMaterial = function(event, index) {
    event.preventDefault();

    const fieldsContainer = document.getElementById(`newMaterialFields_${index}`);
    fieldsContainer.style.display = 'none';
    toggleInputsRequired(fieldsContainer, false);

    const existingGroup = document.getElementById(`existingMaterialGroup_${index}`);
    existingGroup.style.display = 'block';

    const hiddenInput = document.getElementById(`hiddenProductID_${index}`);
    if (hiddenInput) {
        hiddenInput.remove();
    }

    const select = existingGroup.querySelector('.item-product-select');
    select.disabled = false;
    select.value = '';
};

function toggleInputsRequired(container, isRequired) {
    const inputs = container.querySelectorAll('input, select');
    inputs.forEach(input => {
        // Description/Notes is technically optional but useful, we make core fields required
        if (!input.name.endsWith('Notes')) {
            if (isRequired) {
                input.setAttribute('required', 'required');
            } else {
                input.removeAttribute('required');
            }
        }
    });
}

window.toggleNewMaterialForm = function(index) {
    const select = document.querySelector(`#existingMaterialGroup_${index} select`);
    select.value = 'new-material-flag';
    window.onItemProductChange(select, index);
};

window.toggleNewFinishedForm = function(isNew, event) {
    if (event) event.preventDefault();
    const isNewHidden = document.getElementById('IsNewFinishedProductHidden');
    const existingGroup = document.getElementById('existingFinishedGroup');
    const newFields = document.getElementById('newFinishedFields');
    const select = document.getElementById('newBomProduct');

    if (isNew) {
        isNewHidden.value = 'true';
        existingGroup.style.display = 'none';
        newFields.style.display = 'block';
        select.disabled = true;
        select.removeAttribute('required');
        
        // Add required attribute to core finished goods fields
        toggleInputsRequired(newFields, true);
    } else {
        isNewHidden.value = 'false';
        existingGroup.style.display = 'flex';
        newFields.style.display = 'none';
        select.disabled = false;
        select.setAttribute('required', 'required');
        select.value = '';

        // Remove required attribute from core finished goods fields
        toggleInputsRequired(newFields, false);
    }
};
