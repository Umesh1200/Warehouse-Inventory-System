document.addEventListener('DOMContentLoaded', function () {
    const warehouseFilter = document.getElementById('warehouseFilter');
    const typeFilter = document.getElementById('typeFilter');
    const statusFilter = document.getElementById('statusFilter');
    const shelfSearch = document.getElementById('shelfSearch');
    const rows = Array.from(document.querySelectorAll('.tree-row'));

    // Apply visibility of rows based on expand/collapse state and active filters
    function applyTreeVisibility() {
        const warehouse = warehouseFilter?.value || 'All Warehouses';
        const type = typeFilter?.value || 'All Types';
        const status = statusFilter?.value || 'All Statuses';
        const search = (shelfSearch?.value || '').trim().toLowerCase();

        // Build a mapping of row ID to parent ID to avoid nested querySelector DOM queries
        const parentMap = {};
        rows.forEach(row => {
            parentMap[row.dataset.id] = row.dataset.parent;
        });

        const hasFilters = warehouse !== 'All Warehouses' || type !== 'All Types' || status !== 'All Statuses' || search !== '';

        if (hasFilters) {
            const matches = new Set();
            
            rows.forEach(row => {
                const level = parseInt(row.dataset.level || '0');
                
                if (level === 4) { // Shelf
                    const matchesWarehouse = warehouse === 'All Warehouses' || row.dataset.warehouse === warehouse;
                    const matchesType = type === 'All Types' || row.dataset.type === type;
                    const matchesStatus = status === 'All Statuses' || row.dataset.status === status;
                    const matchesSearch = !search || (row.dataset.search || '').includes(search);
                    if (matchesWarehouse && matchesType && matchesStatus && matchesSearch) {
                        matches.add(row.dataset.id);
                    }
                } else {
                    // Parent rows can match search text (e.g. searching for a warehouse, floor, room, or rack)
                    if (search) {
                        const text = row.textContent.toLowerCase();
                        if (text.includes(search)) {
                            matches.add(row.dataset.id);
                        }
                    }
                }
            });

            const visibleIds = new Set();
            const expandedIds = new Set();

            rows.forEach(row => {
                const id = row.dataset.id;
                let currentId = id;
                let isMatchOrDescendant = false;

                while (currentId) {
                    if (matches.has(currentId)) {
                        isMatchOrDescendant = true;
                        break;
                    }
                    currentId = parentMap[currentId];
                }

                if (isMatchOrDescendant) {
                    let tempId = id;
                    while (tempId) {
                        visibleIds.add(tempId);
                        const parentId = parentMap[tempId];
                        if (parentId) {
                            expandedIds.add(parentId);
                        }
                        tempId = parentId;
                    }
                }
            });

            // Update DOM visibility and toggle icons based on filter match
            rows.forEach(row => {
                const id = row.dataset.id;
                const toggle = row.querySelector('.tree-toggle');
                
                if (visibleIds.has(id)) {
                    row.classList.remove('tree-row-hidden');
                    if (expandedIds.has(id) && toggle) {
                        toggle.classList.remove('is-collapsed');
                    }
                } else {
                    row.classList.add('tree-row-hidden');
                }
            });

        } else {
            // No filters active - apply standard expand/collapse logic
            const isExpanded = {};
            rows.forEach(row => {
                const id = row.dataset.id;
                const toggle = row.querySelector('.tree-toggle');
                if (toggle) {
                    isExpanded[id] = !toggle.classList.contains('is-collapsed');
                } else {
                    isExpanded[id] = true;
                }
            });

            rows.forEach(row => {
                const level = parseInt(row.dataset.level || '0');
                if (level === 0) {
                    row.classList.remove('tree-row-hidden');
                } else {
                    let parentId = row.dataset.parent;
                    let showRow = true;
                    while (parentId) {
                        if (isExpanded[parentId] === false) {
                            showRow = false;
                            break;
                        }
                        parentId = parentMap[parentId];
                    }
                    if (showRow) {
                        row.classList.remove('tree-row-hidden');
                    } else {
                        row.classList.add('tree-row-hidden');
                    }
                }
            });
        }
    }

    // Collapse all rows initially on load (except level 0 - Warehouses)
    function initializeTreeGrid() {
        document.querySelectorAll('.tree-toggle').forEach(toggle => {
            toggle.classList.add('is-collapsed');
        });
        applyTreeVisibility();
    }

    // Add filter change event listeners
    [warehouseFilter, typeFilter, statusFilter, shelfSearch].forEach(control => {
        control?.addEventListener(control.tagName === 'INPUT' ? 'input' : 'change', applyTreeVisibility);
    });

    // Expand / Collapse All handlers
    document.getElementById('btnExpandAll')?.addEventListener('click', function() {
        document.querySelectorAll('.tree-toggle').forEach(toggle => {
            toggle.classList.remove('is-collapsed');
        });
        applyTreeVisibility();
    });

    document.getElementById('btnCollapseAll')?.addEventListener('click', function() {
        document.querySelectorAll('.tree-toggle').forEach(toggle => {
            toggle.classList.add('is-collapsed');
        });
        applyTreeVisibility();
    });

    // Event delegation for tree toggles
    document.addEventListener('click', function (e) {
        const toggle = e.target.closest('.tree-toggle');
        if (toggle) {
            toggle.classList.toggle('is-collapsed');
            applyTreeVisibility();
        }
    });

    // Generic AJAX fetch helper
    async function fetchAndPopulate(handlerName, id, modalId, populateCallback) {
        try {
            const response = await fetch(`?handler=${handlerName}&id=${id}`);
            if (!response.ok) throw new Error('Network response was not ok');
            const data = await response.json();
            if (data) {
                populateCallback(data);
                const modal = new bootstrap.Modal(document.getElementById(modalId));
                modal.show();
            } else {
                alert('Failed to retrieve details.');
            }
        } catch (err) {
            console.error(err);
            alert('An error occurred while loading details.');
        }
    }

    // Event delegation for inline action buttons
    document.addEventListener('click', function (e) {
        // --- Add Child Handlers ---
        const addFloorBtn = e.target.closest('.add-floor-btn');
        if (addFloorBtn) {
            const whId = addFloorBtn.dataset.id;
            const select = document.getElementById('add-floor-wh-select');
            if (select) select.value = whId;
            const modal = new bootstrap.Modal(document.getElementById('addFloorModal'));
            modal.show();
            return;
        }

        const addRoomBtn = e.target.closest('.add-room-btn');
        if (addRoomBtn) {
            const floorId = addRoomBtn.dataset.id;
            const select = document.getElementById('add-room-floor-select');
            if (select) select.value = floorId;
            const modal = new bootstrap.Modal(document.getElementById('addRoomModal'));
            modal.show();
            return;
        }

        const addRackBtn = e.target.closest('.add-rack-btn');
        if (addRackBtn) {
            const roomId = addRackBtn.dataset.id;
            const select = document.getElementById('add-rack-room-select');
            if (select) select.value = roomId;
            const modal = new bootstrap.Modal(document.getElementById('addRackModal'));
            modal.show();
            return;
        }

        const addShelfBtn = e.target.closest('.add-shelf-btn');
        if (addShelfBtn) {
            const rackId = addShelfBtn.dataset.id;
            const select = document.getElementById('add-shelf-rack-select');
            if (select) select.value = rackId;

            const tierSelect = document.getElementById('add-shelf-tier-select');
            const codeInput = document.getElementById('add-shelf-code-input');
            if (select && codeInput && select.selectedOptions.length > 0) {
                const optText = select.selectedOptions[0].textContent;
                const parts = optText.split('/');
                if (parts.length >= 2) {
                    const roomCode = parts[0].trim();
                    const rackCode = parts[1].split('-')[0].trim();
                    const tier = tierSelect ? tierSelect.value : '1';
                    codeInput.placeholder = `e.g. KTM-F1-${roomCode}-${rackCode}-S${tier}`;
                }
            }

            const modal = new bootstrap.Modal(document.getElementById('addShelfModal'));
            modal.show();
            return;
        }

        // --- Edit Handlers ---
        const editWhBtn = e.target.closest('.edit-wh-btn');
        if (editWhBtn) {
            const id = editWhBtn.dataset.id;
            fetchAndPopulate('Warehouse', id, 'editWarehouseModal', function (data) {
                document.getElementById('edit-wh-id').value = data.id || data.ID;
                document.getElementById('edit-wh-code').value = data.code || data.Code;
                document.getElementById('edit-wh-name').value = data.name || data.Name;
                document.getElementById('edit-wh-location').value = data.location || data.Location || '';
            });
            return;
        }

        const editFloorBtn = e.target.closest('.edit-floor-btn');
        if (editFloorBtn) {
            const id = editFloorBtn.dataset.id;
            fetchAndPopulate('Floor', id, 'editFloorModal', function (data) {
                document.getElementById('edit-floor-id').value = data.id || data.ID;
                document.getElementById('edit-floor-wh').value = data.warehouseID || data.warehouseId || data.WarehouseID;
                document.getElementById('edit-floor-code').value = data.code || data.Code;
                document.getElementById('edit-floor-name').value = data.name || data.Name;
            });
            return;
        }

        const editRoomBtn = e.target.closest('.edit-room-btn');
        if (editRoomBtn) {
            const id = editRoomBtn.dataset.id;
            fetchAndPopulate('Room', id, 'editRoomModal', function (data) {
                document.getElementById('edit-room-id').value = data.id || data.ID;
                document.getElementById('edit-room-floor').value = data.floorID || data.floorId || data.FloorID;
                document.getElementById('edit-room-code').value = data.code || data.Code;
                document.getElementById('edit-room-name').value = data.name || data.Name;
            });
            return;
        }

        const editRackBtn = e.target.closest('.edit-rack-btn');
        if (editRackBtn) {
            const id = editRackBtn.dataset.id;
            fetchAndPopulate('Rack', id, 'editRackModal', function (data) {
                document.getElementById('edit-rack-id').value = data.id || data.ID;
                document.getElementById('edit-rack-room').value = data.roomID || data.roomId || data.RoomID;
                document.getElementById('edit-rack-code').value = data.code || data.Code;
                document.getElementById('edit-rack-name').value = data.name || data.Name;
            });
            return;
        }

        const editShelfBtn = e.target.closest('.edit-shelf-btn');
        if (editShelfBtn) {
            const id = editShelfBtn.dataset.id;
            fetchAndPopulate('Shelf', id, 'editShelfModal', function (data) {
                document.getElementById('edit-shelf-id').value = data.id || data.ID;
                document.getElementById('edit-shelf-rack').value = data.rackID || data.rackId || data.RackID;
                document.getElementById('edit-shelf-code').value = data.code || data.Code;
                document.getElementById('edit-shelf-name').value = data.name || data.Name || '';
                document.getElementById('edit-shelf-type').value = data.type || data.Type;
                document.getElementById('edit-shelf-maxqty').value = data.maxQty || data.MaxQty;
                document.getElementById('edit-shelf-maxweight').value = data.maxWeight || data.MaxWeight;

                // Extract and pre-select Tier / Level
                const code = data.code || data.Code || '';
                const name = data.name || data.Name || '';
                let tier = 1;
                const parts = code.split('-');
                if (parts.length > 0) {
                    const last = parts[parts.length - 1].trim();
                    if (last.toUpperCase().startsWith('S') && !isNaN(parseInt(last.substring(1)))) {
                        tier = parseInt(last.substring(1));
                    } else {
                        const digits = last.replace(/\D/g, '');
                        if (digits) tier = parseInt(digits);
                    }
                }
                if (name) {
                    if (name.includes('Level 4') || name.includes('Tier 4')) tier = 4;
                    else if (name.includes('Level 3') || name.includes('Tier 3')) tier = 3;
                    else if (name.includes('Level 2') || name.includes('Tier 2')) tier = 2;
                    else if (name.includes('Level 1') || name.includes('Tier 1')) tier = 1;
                }
                if (tier < 1) tier = 1;
                if (tier > 4) tier = 4;
                const tierSelect = document.getElementById('edit-shelf-tier');
                if (tierSelect) tierSelect.value = tier;
            });
            return;
        }
    });

    // Run initialization
    initializeTreeGrid();
});
