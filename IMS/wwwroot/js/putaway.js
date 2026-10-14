document.addEventListener('DOMContentLoaded', function () {
    const rows = Array.from(document.querySelectorAll('.putaway-row'));
    const searchInput = document.getElementById('putawaySearch');
    const typeFilter = document.getElementById('typeFilter');
    const statusFilter = document.getElementById('statusFilter');
    const timeFilter = document.getElementById('timeFilter');

    function applyFilters() {
        const search = (searchInput?.value || '').trim().toLowerCase();
        const type = typeFilter?.value || 'All Types';
        const status = statusFilter?.value || 'All Statuses';
        const time = timeFilter?.value || 'Today';

        const todayStr = new Date().toISOString().split('T')[0];

        rows.forEach(row => {
            const matchesSearch = !search || (row.dataset.search || '').includes(search);
            const matchesType = type === 'All Types' || row.dataset.type === type;
            const matchesStatus = status === 'All Statuses' || row.dataset.status === status;
            
            let matchesTime = true;
            if (time === 'Today' && row.dataset.status === 'Completed') {
                matchesTime = (row.dataset.date === todayStr);
            }

            row.hidden = !(matchesSearch && matchesType && matchesStatus && matchesTime);
        });
    }

    const shelfState = {};
    const shelfComboboxes = new Map();

    function syncInputText(state) {
        const shelf = state.shelves.find(s => String(s.id) === String(state.select.value));
        if (shelf) {
            const txt = `${shelf.shelfCode} [${shelf.shelfType}] - ${Number(shelf.projectedUtilizationPercentage || 0).toFixed(0)}% Full`;
            state.input.value = txt;
            state.input.lastSelectedText = txt;
        } else {
            state.input.value = '';
            state.input.lastSelectedText = '';
        }
    }

    function selectShelfOption(state, value) {
        state.select.value = value;
        state.panel.hidden = true;
        state.select.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function renderPanel(state, searchQuery) {
        const query = (searchQuery || '').trim().toLowerCase();
        const tokens = query.split(/\s+/).filter(Boolean);
        
        const filtered = state.shelves.filter(shelf => {
            if (tokens.length === 0) return true;
            const searchStr = `${shelf.shelfCode} ${shelf.shelfType} ${shelf.name}`.toLowerCase();
            return tokens.every(token => searchStr.includes(token));
        });

        if (filtered.length === 0) {
            state.panel.innerHTML = '<div class="candidate-empty" style="padding: 10px; text-align: center; color: var(--text-muted); font-size: 12px;">No matching shelves found.</div>';
            state.panel.hidden = false;
            return;
        }

        const groups = {};
        filtered.forEach(shelf => {
            const parts = shelf.name.split(' -> ');
            let groupName = 'Storage Shelves';
            if (parts.length >= 4) {
                groupName = `${parts[2]} - ${parts[3]}`; // RoomName - RackName
            }
            if (!groups[groupName]) {
                groups[groupName] = [];
            }
            groups[groupName].push(shelf);
        });

        let html = '';
        for (const [groupName, groupShelves] of Object.entries(groups)) {
            html += `<div class="shelf-combobox-group">${escapeHtml(groupName)}</div>`;
            groupShelves.forEach(shelf => {
                const isRecommended = shelf.isRecommended;
                const warningText = shelf.warning ? ` / Warning: ${shelf.warning}` : '';
                const recommendedClass = isRecommended ? 'recommended-option' : '';
                const disabledAttr = isRecommended ? '' : 'disabled';
                
                html += `
                    <button type="button" class="shelf-combobox-option ${recommendedClass}" data-value="${shelf.id}" ${disabledAttr}>
                        <span>${escapeHtml(shelf.shelfCode)} [${escapeHtml(shelf.shelfType)}]</span>
                        <small>${Number(shelf.projectedUtilizationPercentage || 0).toFixed(0)}% Full / Available: ${Number(shelf.quantityAvailable).toLocaleString()}${escapeHtml(warningText)}</small>
                    </button>
                `;
            });
        }
        state.panel.innerHTML = html;
        state.panel.hidden = false;
    }

    function initShelfCombobox(selectId, hintId) {
        if (shelfComboboxes.has(selectId)) return shelfComboboxes.get(selectId);

        const select = document.getElementById(selectId);
        if (!select) return null;

        const container = document.createElement('div');
        container.className = 'shelf-combobox';

        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'shelf-combobox-input';
        input.placeholder = selectId.includes('create') ? '-- Select Suggestion --' : '-- Select Final Location --';
        input.autocomplete = 'off';

        const panel = document.createElement('div');
        panel.className = 'shelf-combobox-panel';
        panel.hidden = true;

        select.parentNode.insertBefore(container, select);
        container.appendChild(input);
        container.appendChild(panel);
        select.style.display = 'none';

        const state = {
            selectId,
            hintId,
            select,
            input,
            panel,
            shelves: [],
            selectedValue: ''
        };

        select.addEventListener('change', () => {
            syncInputText(state);
        });

        input.addEventListener('focus', () => {
            input.select();
            renderPanel(state, '');
        });

        input.addEventListener('input', () => {
            renderPanel(state, input.value);
        });

        input.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                panel.hidden = true;
                syncInputText(state);
            } else if (e.key === 'Enter') {
                e.preventDefault();
                const firstOption = panel.querySelector('.shelf-combobox-option:not(:disabled)');
                if (firstOption) {
                    selectShelfOption(state, firstOption.dataset.value);
                } else {
                    panel.hidden = true;
                    syncInputText(state);
                }
            }
        });

        panel.addEventListener('click', (e) => {
            const optionBtn = e.target.closest('.shelf-combobox-option');
            if (optionBtn && !optionBtn.disabled) {
                selectShelfOption(state, optionBtn.dataset.value);
            }
        });

        shelfComboboxes.set(selectId, state);
        return state;
    }

    async function loadSuggestedShelves(productId, quantity, selectId, hintId, selectedShelfId) {
        const select = document.getElementById(selectId);
        const hint = document.getElementById(hintId);
        if (!select) return;

        const comboboxState = initShelfCombobox(selectId, hintId);

        if (comboboxState) {
            comboboxState.input.disabled = true;
            comboboxState.input.placeholder = 'Loading shelves...';
            comboboxState.input.value = '';
        }

        select.innerHTML = '<option value="">Loading shelves...</option>';
        select.disabled = true;
        if (hint) hint.textContent = '';
        updateShelfInsights(selectId, []);
        renderShelfCandidates(selectId, []);

        try {
            const response = await fetch(`?handler=SuggestedShelves&productId=${productId}&quantity=${quantity}`);
            const shelves = await response.json();
            shelfState[selectId] = shelves;

            if (comboboxState) {
                comboboxState.shelves = shelves;
            }

            select.innerHTML = '<option value="">-- Select Shelf --</option>';
            shelves.forEach(shelf => {
                const option = document.createElement('option');
                option.value = shelf.id;
                option.textContent = `${shelf.name} | Available: ${Number(shelf.quantityAvailable).toLocaleString()} | Projected: ${Number(shelf.projectedUtilizationPercentage || 0).toFixed(0)}%`;
                option.disabled = !shelf.isRecommended;
                option.dataset.warning = shelf.warning || '';
                option.dataset.recommended = shelf.isRecommended ? 'true' : 'false';
                option.dataset.shelfCode = shelf.shelfCode || '';
                option.dataset.projected = shelf.projectedUtilizationPercentage || '';
                select.appendChild(option);
            });

            if (selectedShelfId) {
                select.value = selectedShelfId;
            } else {
                const recommended = shelves.find(shelf => shelf.isRecommended);
                if (recommended) select.value = recommended.id;
            }

            if (comboboxState) {
                comboboxState.input.disabled = false;
                comboboxState.input.placeholder = selectId.includes('create') ? '-- Select Suggestion --' : '-- Select Final Location --';
                syncInputText(comboboxState);
            }

            updateShelfHint(select, hint);
            updateShelfInsights(selectId, shelves);
            renderShelfCandidates(selectId, shelves);
            select.disabled = false;
        } catch {
            select.innerHTML = '<option value="">Unable to load shelves</option>';
            if (hint) hint.textContent = 'Shelf suggestions could not be loaded.';
            if (comboboxState) {
                comboboxState.input.placeholder = 'Unable to load shelves';
                comboboxState.input.value = '';
            }
            updateShelfInsights(selectId, []);
            renderShelfCandidates(selectId, []);
        }
    }

    function updateShelfHint(select, hint) {
        if (!hint) return;
        const option = select.options[select.selectedIndex];
        const warning = option?.dataset.warning || '';
        const recommended = option?.dataset.recommended === 'true';
        const projected = option?.dataset.projected ? ` Projected fill: ${Number(option.dataset.projected).toFixed(0)}%.` : '';
        hint.textContent = warning || (recommended ? `Recommended shelf based on storage rule and capacity.${projected}` : '');
        hint.classList.toggle('is-warning', Boolean(warning));
    }

    function modalPrefix(selectId) {
        return selectId.startsWith('create') ? 'create' : 'complete';
    }

    function updateShelfInsights(selectId, shelves) {
        const prefix = modalPrefix(selectId);
        const recommended = shelves.filter(shelf => shelf.isRecommended);
        const selected = shelves.find(shelf => String(shelf.id) === String(document.getElementById(selectId)?.value));
        const best = selected || recommended[0];

        setText(`${prefix}-compatible-count`, recommended.length ? `${recommended.length}` : '0');
        setText(`${prefix}-best-fit`, best?.shelfCode || '-');
        setText(`${prefix}-projected-fill`, best ? `${Number(best.projectedUtilizationPercentage || 0).toFixed(0)}%` : '-');
    }

    function renderShelfCandidates(selectId, shelves) {
        const prefix = modalPrefix(selectId);
        const container = document.getElementById(`${prefix}-shelf-candidates`);
        if (!container) return;

        const candidates = shelves.slice(0, 5);
        if (!candidates.length) {
            container.innerHTML = '<div class="candidate-empty">No shelf suggestions available.</div>';
            return;
        }

        container.innerHTML = candidates.map(shelf => {
            const current = Number(shelf.utilizationPercentage || 0);
            const projected = Number(shelf.projectedUtilizationPercentage || 0);
            const status = shelf.isRecommended ? 'Recommended' : (shelf.warning || 'Not available');
            const stateClass = shelf.isRecommended ? 'recommended' : 'blocked';
            return `
                <button type="button" class="shelf-candidate ${stateClass}" data-select-id="${selectId}" data-shelf-id="${shelf.id}" ${shelf.isRecommended ? '' : 'disabled'}>
                    <span class="candidate-code">${escapeHtml(shelf.shelfCode || shelf.name)}</span>
                    <span class="candidate-status">${escapeHtml(status)}</span>
                    <span class="candidate-bar"><i style="width: ${Math.min(100, Math.max(0, projected))}%"></i></span>
                    <span class="candidate-meta">Now ${current.toFixed(0)}% -> ${projected.toFixed(0)}% | Avail ${Number(shelf.quantityAvailable || 0).toLocaleString()}</span>
                </button>`;
        }).join('');
    }

    function escapeHtml(value) {
        return String(value || '').replace(/[&<>"']/g, char => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[char]));
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) element.textContent = value || '-';
    }

    document.addEventListener('click', event => {
        const candidate = event.target.closest('.shelf-candidate');
        if (!candidate || candidate.disabled) return;

        const select = document.getElementById(candidate.dataset.selectId);
        if (!select) return;

        select.value = candidate.dataset.shelfId;
        select.dispatchEvent(new Event('change', { bubbles: true }));
    });

    document.addEventListener('click', (e) => {
        if (!e.target.closest('.shelf-combobox')) {
            document.querySelectorAll('.shelf-combobox-panel').forEach(panel => {
                panel.hidden = true;
            });
            shelfComboboxes.forEach(state => {
                syncInputText(state);
            });
        }
    });

    document.querySelectorAll('.create-task-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            document.getElementById('create-qcid').value = this.dataset.qcid;
            document.getElementById('create-productid').value = this.dataset.productid;
            document.getElementById('create-qty').value = this.dataset.qty;
            document.getElementById('create-disp-product').innerText = this.dataset.name;
            document.getElementById('create-disp-sku').innerText = 'SKU: ' + this.dataset.sku;
            document.getElementById('create-disp-qty').innerText = Number(this.dataset.qty || 0).toLocaleString();
            document.getElementById('create-disp-ref').innerText = this.dataset.ref || '';
            document.getElementById('create-disp-batch').innerText = this.dataset.batch ? `Batch: ${this.dataset.batch}` : '';
            loadSuggestedShelves(this.dataset.productid, this.dataset.qty, 'create-suggested-shelf', 'create-shelf-hint');
        });
    });

    document.querySelectorAll('.complete-putaway-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            document.getElementById('complete-taskid').value = this.dataset.taskid;
            document.getElementById('complete-disp-taskno').innerText = this.dataset.taskno;
            document.getElementById('complete-disp-product').innerText = this.dataset.name;
            document.getElementById('complete-disp-sku').innerText = 'SKU: ' + this.dataset.sku;
            document.getElementById('complete-disp-qty').innerText = Number(this.dataset.qty || 0).toLocaleString();
            document.getElementById('complete-disp-ref').innerText = this.dataset.ref || '';
            document.getElementById('complete-disp-batch').innerText = this.dataset.batch ? `Batch: ${this.dataset.batch}` : '';
            loadSuggestedShelves(this.dataset.productid, this.dataset.qty, 'complete-actual-shelf', 'complete-shelf-hint', this.dataset.suggested);
        });
    });

    document.querySelectorAll('.view-putaway-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const row = this.closest('.putaway-row');
            if (!row) return;

            setText('detail-subtitle', `${row.dataset.product} - ${row.dataset.sku}`);
            setText('detail-taskno', row.dataset.taskno);
            setText('detail-status', row.dataset.status);
            setText('detail-qty', Number(row.dataset.qty || 0).toLocaleString());
            setText('detail-capacity', row.dataset.capacity);
            setText('detail-reference', row.dataset.reference);
            setText('detail-batch', row.dataset.batch || '-');
            setText('detail-product', row.dataset.product);
            setText('detail-sku', row.dataset.sku);
            setText('detail-suggested', row.dataset.suggested);
            setText('detail-actual', row.dataset.actual || 'Not completed yet');
            setText('detail-warehouse', row.dataset.warehouse || '-');
            setText('detail-path', [row.dataset.floor, row.dataset.room, row.dataset.rack, row.dataset.displayShelf].filter(Boolean).join(' / '));
            setText('detail-available', row.dataset.available);
            setText('detail-assigned', row.dataset.assigned);
        });
    });

    ['create-suggested-shelf', 'complete-actual-shelf'].forEach(selectId => {
        const select = document.getElementById(selectId);
        const hintId = selectId === 'create-suggested-shelf' ? 'create-shelf-hint' : 'complete-shelf-hint';
        if (select) {
            select.addEventListener('change', () => {
                updateShelfHint(select, document.getElementById(hintId));
                updateShelfInsights(selectId, shelfState[selectId] || []);
            });
        }
    });

    [searchInput, typeFilter, statusFilter, timeFilter].forEach(control => {
        control?.addEventListener(control.tagName === 'INPUT' ? 'input' : 'change', applyFilters);
    });

    // Apply default filters immediately
    applyFilters();
});
