// Product Locator JavaScript - Interactive 2D Grid, Rack Elevation, and Live Autocomplete

document.addEventListener('DOMContentLoaded', function () {
    initLiveSearch();
    initWarehouseFilter();
    renderFloorPlan();
    initTabSwitcher();
    initDefaultSelection();
    initScannerButton();
});

let selectedShelfId = null;
let currentWarehouseFilter = 'all';

// Helper for safe property access regardless of PascalCase vs camelCase serialization
function getProp(obj, ...propNames) {
    if (!obj) return undefined;
    for (const name of propNames) {
        if (obj[name] !== undefined && obj[name] !== null) return obj[name];
    }
    return undefined;
}

// Helper to prevent NaN values under any circumstance
function formatNumber(val, fallback = '0') {
    if (val === null || val === undefined || val === '') return fallback;
    const n = Number(val);
    return isNaN(n) ? fallback : n.toLocaleString();
}

// 1. Live Autocomplete Search
function initLiveSearch() {
    const searchInput = document.getElementById('locatorSearchInput');
    const clearBtn = document.getElementById('clearSearchBtn');
    const suggestionsBox = document.getElementById('locatorSuggestions');

    if (!searchInput || !suggestionsBox) return;

    let debounceTimer = null;

    searchInput.addEventListener('input', function () {
        const query = searchInput.value.trim();
        clearTimeout(debounceTimer);

        if (query.length === 0) {
            suggestionsBox.style.display = 'none';
            suggestionsBox.innerHTML = '';
            return;
        }

        debounceTimer = setTimeout(() => {
            fetchProductSuggestions(query);
        }, 250);
    });

    searchInput.addEventListener('keydown', function (e) {
        const items = suggestionsBox.querySelectorAll('.suggestion-item');
        let currentIndex = -1;
        items.forEach((item, index) => {
            if (item.classList.contains('selected')) currentIndex = index;
        });

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (items.length === 0) return;
            if (currentIndex >= 0) items[currentIndex].classList.remove('selected');
            const nextIndex = (currentIndex + 1) % items.length;
            items[nextIndex].classList.add('selected');
            items[nextIndex].scrollIntoView({ block: 'nearest' });
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (items.length === 0) return;
            if (currentIndex >= 0) items[currentIndex].classList.remove('selected');
            const prevIndex = (currentIndex - 1 + items.length) % items.length;
            items[prevIndex].classList.add('selected');
            items[prevIndex].scrollIntoView({ block: 'nearest' });
        } else if (e.key === 'Enter') {
            if (currentIndex >= 0 && items[currentIndex]) {
                e.preventDefault();
                items[currentIndex].click();
            } else if (items.length > 0) {
                e.preventDefault();
                items[0].click();
            }
        } else if (e.key === 'Escape') {
            suggestionsBox.style.display = 'none';
        }
    });

    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            searchInput.value = '';
            searchInput.focus();
            suggestionsBox.style.display = 'none';
        });
    }

    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target) && !suggestionsBox.contains(e.target)) {
            suggestionsBox.style.display = 'none';
        }
    });
}

async function fetchProductSuggestions(term) {
    const suggestionsBox = document.getElementById('locatorSuggestions');
    try {
        const res = await fetch(`?handler=Search&term=${encodeURIComponent(term)}`);
        if (!res.ok) return;
        const data = await res.json();

        if (!data || data.length === 0) {
            suggestionsBox.innerHTML = '<div class="p-3 text-muted text-center" style="font-size: 0.85rem;">No matching products found</div>';
            suggestionsBox.style.display = 'block';
            return;
        }

        let html = '';
        data.forEach((p, idx) => {
            const sku = getProp(p, 'sku', 'SKU') || '';
            const cat = getProp(p, 'categoryName', 'CategoryName') || 'General';
            const name = getProp(p, 'productName', 'ProductName') || '';
            const qty = getProp(p, 'totalCurrentQty', 'TotalCurrentQty') || 0;
            const shelves = getProp(p, 'shelfCount', 'ShelfCount') || 0;

            html += `
                <a href="/Warehouses/ProductLocator?sku=${encodeURIComponent(sku)}" class="suggestion-item ${idx === 0 ? 'selected' : ''}">
                    <div class="sugg-left">
                        <div class="sugg-sku-line">
                            <span class="sugg-sku">${escapeHtml(sku)}</span>
                            <span class="sugg-cat">${escapeHtml(cat)}</span>
                        </div>
                        <div class="sugg-name">${escapeHtml(name)}</div>
                    </div>
                    <div class="sugg-right">
                        <span class="sugg-stock">${formatNumber(qty)} on-hand</span>
                        <span class="sugg-shelves">${shelves} shelf location(s)</span>
                    </div>
                </a>
            `;
        });

        suggestionsBox.innerHTML = html;
        suggestionsBox.style.display = 'block';
    } catch (err) {
        console.error('Error fetching suggestions:', err);
    }
}

// 2. Warehouse & Floor Filtering for 2D Map
function initWarehouseFilter() {
    const select = document.getElementById('mapWarehouseSelect');
    if (!select || !window.warehouseLayoutData) return;

    const warehouses = [];
    const seenWh = new Set();

    window.warehouseLayoutData.forEach(s => {
        const whId = getProp(s, 'warehouseId', 'WarehouseID');
        const whName = getProp(s, 'warehouseName', 'WarehouseName');
        if (whId && !seenWh.has(whId)) {
            seenWh.add(whId);
            warehouses.push({ id: whId, name: whName });
        }
    });

    warehouses.forEach(wh => {
        const opt = document.createElement('option');
        opt.value = wh.id;
        opt.textContent = wh.name;
        select.appendChild(opt);
    });

    if (window.productLocations && window.productLocations.length > 0) {
        const firstLocWh = getProp(window.productLocations[0], 'warehouseId', 'WarehouseID');
        if (firstLocWh) {
            select.value = firstLocWh;
            currentWarehouseFilter = firstLocWh.toString();
        }
    }

    select.addEventListener('change', function () {
        currentWarehouseFilter = this.value;
        renderFloorPlan();
    });
}

// 3. Render 2D Floor Plan Grid
function renderFloorPlan() {
    const container = document.getElementById('warehouseGridContainer');
    if (!container || !window.warehouseLayoutData) return;

    let shelves = window.warehouseLayoutData;
    if (currentWarehouseFilter !== 'all') {
        shelves = shelves.filter(s => {
            const whId = getProp(s, 'warehouseId', 'WarehouseID');
            return whId && whId.toString() === currentWarehouseFilter;
        });
    }

    if (shelves.length === 0) {
        container.innerHTML = '<div class="p-4 text-center text-muted">No warehouse layout shelves found for this filter.</div>';
        return;
    }

    const roomsMap = new Map();

    shelves.forEach(s => {
        const fCode = getProp(s, 'floorCode', 'FloorCode') || '';
        const rCode = getProp(s, 'roomCode', 'RoomCode') || '';
        const rId = getProp(s, 'roomId', 'RoomID') || '';
        const roomKey = `${fCode}-${rCode || rId}`;

        if (!roomsMap.has(roomKey)) {
            roomsMap.set(roomKey, {
                roomName: getProp(s, 'roomName', 'RoomName') || `Room ${rCode}`,
                floorName: getProp(s, 'floorName', 'FloorName') || fCode,
                racks: new Map()
            });
        }
        const roomObj = roomsMap.get(roomKey);
        const rackCode = getProp(s, 'rackCode', 'RackCode') || '';
        const rackId = getProp(s, 'rackId', 'RackID');
        const rackKey = rackCode || rackId;

        if (!roomObj.racks.has(rackKey)) {
            roomObj.racks.set(rackKey, {
                rackId: rackId,
                rackName: getProp(s, 'rackName', 'RackName') || `Rack ${rackCode}`,
                shelves: []
            });
        }
        roomObj.racks.get(rackKey).shelves.push(s);
    });

    const occupiedIds = new Set(window.occupiedShelfIds || []);

    let html = '';
    roomsMap.forEach((room) => {
        html += `
            <div class="room-block">
                <div class="room-title-bar">
                    <strong><span class="material-symbols-outlined" style="font-size:1.1rem;color:#64748b;">meeting_room</span> ${escapeHtml(room.roomName)}</strong>
                    <span class="room-sub-info">${escapeHtml(room.floorName)}</span>
                </div>
                <div class="racks-row">
        `;

        room.racks.forEach((rack) => {
            html += `
                <div class="rack-card" data-rack-id="${rack.rackId}">
                    <div class="rack-title">
                        <span>${escapeHtml(rack.rackName)}</span>
                        <span>${rack.shelves.length} Shelves</span>
                    </div>
                    <div class="shelves-grid">
            `;

            rack.shelves.forEach((sh) => {
                const sId = getProp(sh, 'shelfId', 'shelfID', 'ShelfID');
                const sCode = getProp(sh, 'shelfCode', 'ShelfCode') || '';
                const sType = getProp(sh, 'shelfType', 'ShelfType') || 'General';
                const sUtil = getProp(sh, 'utilization', 'UtilizationPercentage') || 0;

                const isOccupied = occupiedIds.has(sId);
                const isSelected = selectedShelfId === sId;
                const isQcHold = sType === 'QC Hold';

                let cellClass = 'shelf-cell';
                if (isOccupied) cellClass += ' occupied-target';
                if (isSelected) cellClass += ' selected-target';
                if (isQcHold) cellClass += ' qchold-type';

                let prodQtyDisplay = '';
                if (window.productLocations) {
                    const match = window.productLocations.find(l => getProp(l, 'shelfId', 'shelfID', 'ShelfID') === sId);
                    if (match) {
                        const q = getProp(match, 'productCurrentQty', 'ProductCurrentQty') || 0;
                        prodQtyDisplay = `${formatNumber(q)} pcs`;
                    }
                }

                html += `
                    <div class="${cellClass}" 
                         id="grid-shelf-${sId}"
                         data-shelf-id="${sId}"
                         title="${escapeHtml(sCode)} (${sType}) - ${sUtil}% full"
                         onclick="selectShelfLocation(${sId})">
                        <span class="shelf-cell-code">${escapeHtml(sCode)}</span>
                        <span class="shelf-cell-badge">${isOccupied ? prodQtyDisplay : `${sUtil}%`}</span>
                    </div>
                `;
            });

            html += `
                    </div>
                </div>
            `;
        });

        html += `
                </div>
            </div>
        `;
    });

    container.innerHTML = html;
}

// 4. Render Rack Tier Elevation Graphic
function renderRackElevation(targetShelfId) {
    const container = document.getElementById('rackElevationContainer');
    if (!container || !window.warehouseLayoutData) return;

    const targetShelf = window.warehouseLayoutData.find(s => getProp(s, 'shelfId', 'shelfID', 'ShelfID') === targetShelfId);
    if (!targetShelf) {
        container.innerHTML = '<div class="p-4 text-center text-muted">Select a shelf to inspect rack elevation.</div>';
        return;
    }

    const tRackId = getProp(targetShelf, 'rackId', 'RackID');
    const tRackName = getProp(targetShelf, 'rackName', 'RackName') || getProp(targetShelf, 'rackCode', 'RackCode');
    const tRoomName = getProp(targetShelf, 'roomName', 'RoomName') || '';

    const rackShelves = window.warehouseLayoutData
        .filter(s => getProp(s, 'rackId', 'RackID') === tRackId)
        .sort((a, b) => (getProp(a, 'shelfCode', 'ShelfCode') || '').localeCompare(getProp(b, 'shelfCode', 'ShelfCode') || ''));

    const occupiedIds = new Set(window.occupiedShelfIds || []);

    let html = `
        <div class="rack-elevation-frame">
            <div class="rack-upright-left"></div>
            <div class="rack-upright-right"></div>
            <div class="rack-header-label">
                RACK: ${escapeHtml(tRackName)} (${escapeHtml(tRoomName)})
            </div>
            <div class="rack-tiers-stack">
    `;

    rackShelves.forEach((sh, index) => {
        const sId = getProp(sh, 'shelfId', 'shelfID', 'ShelfID');
        const sCode = getProp(sh, 'shelfCode', 'ShelfCode') || '';
        const sType = getProp(sh, 'shelfType', 'ShelfType') || 'General';
        const sMax = getProp(sh, 'maxQty', 'MaxQty') || 0;
        const sUtil = getProp(sh, 'utilization', 'UtilizationPercentage') || 0;

        const isTarget = sId === targetShelfId;
        const isOccupied = occupiedIds.has(sId);
        const tierNum = index + 1;

        let prodQtyText = '';
        if (window.productLocations) {
            const match = window.productLocations.find(l => getProp(l, 'shelfId', 'shelfID', 'ShelfID') === sId);
            if (match) {
                const q = getProp(match, 'productCurrentQty', 'ProductCurrentQty') || 0;
                prodQtyText = `${formatNumber(q)} pcs of ${window.currentProductSku}`;
            }
        }

        html += `
            <div class="rack-tier-row ${isTarget ? 'active-tier' : ''}" 
                 onclick="selectShelfLocation(${sId})">
                <div class="tier-lead">
                    <span class="tier-number-badge">Tier ${tierNum}</span>
                    <div>
                        <span class="tier-shelf-code">${escapeHtml(sCode)}</span>
                        <div style="font-size:0.75rem;color:#64748b;">${escapeHtml(sType)} • Max: ${formatNumber(sMax)}</div>
                    </div>
                </div>
                <div class="tier-load-info">
                    <span class="tier-prod-qty">${isOccupied ? prodQtyText : (isTarget ? 'Selected' : 'Other Stock')}</span>
                    <div class="tier-capacity-bar" title="Utilization: ${sUtil}%">
                        <div class="tier-capacity-fill" style="width: ${Math.min(100, sUtil)}%"></div>
                    </div>
                </div>
            </div>
        `;
    });

    html += `
            </div>
        </div>
    `;

    container.innerHTML = html;
}

// 5. Select a Shelf Location
window.selectShelfLocation = function (shelfId) {
    if (!shelfId) return;
    selectedShelfId = shelfId;

    document.querySelectorAll('.shelf-cell').forEach(el => el.classList.remove('selected-target'));
    const cell = document.getElementById(`grid-shelf-${shelfId}`);
    if (cell) {
        cell.classList.add('selected-target');
        cell.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'nearest' });
    }

    document.querySelectorAll('.shelf-address-card').forEach(el => el.classList.remove('active-selected'));
    const card = document.getElementById(`card-shelf-${shelfId}`);
    if (card) {
        card.classList.add('active-selected');
        card.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    updateShelfBanner(shelfId);
    renderRackElevation(shelfId);
};

window.focusShelfOnMap = function (shelfId) {
    const tabFloorBtn = document.getElementById('tabFloorPlanBtn');
    if (tabFloorBtn) tabFloorBtn.click();

    selectShelfLocation(shelfId);

    const cell = document.getElementById(`grid-shelf-${shelfId}`);
    if (cell) {
        cell.style.transition = 'transform 0.3s ease';
        cell.style.transform = 'scale(1.35)';
        setTimeout(() => {
            cell.style.transform = '';
        }, 500);
    }
};

function updateShelfBanner(shelfId) {
    const codeEl = document.getElementById('selectedShelfCode');
    const breadcrumbEl = document.getElementById('selectedShelfBreadcrumb');
    const qtyEl = document.getElementById('statProdQty');
    const utilEl = document.getElementById('statUtilization');
    const capEl = document.getElementById('statCapacity');

    if (!codeEl) return;

    let locMatch = null;
    if (window.productLocations) {
        locMatch = window.productLocations.find(l => getProp(l, 'shelfId', 'shelfID', 'ShelfID') === shelfId);
    }

    const layoutMatch = window.warehouseLayoutData ? window.warehouseLayoutData.find(s => getProp(s, 'shelfId', 'shelfID', 'ShelfID') === shelfId) : null;

    if (locMatch) {
        const sCode = getProp(locMatch, 'shelfCode', 'ShelfCode') || '';
        const path = getProp(locMatch, 'hierarchyPath', 'HierarchyPath') || `${getProp(locMatch, 'warehouseName', 'WarehouseName') || ''} > ${getProp(locMatch, 'floorName', 'FloorName') || ''} > ${getProp(locMatch, 'roomName', 'RoomName') || ''} > ${getProp(locMatch, 'rackName', 'RackName') || ''} > ${getProp(locMatch, 'shelfName', 'ShelfName') || ''}`;
        const pQty = getProp(locMatch, 'productCurrentQty', 'ProductCurrentQty') || 0;
        const util = getProp(locMatch, 'shelfUtilizationPercentage', 'ShelfUtilizationPercentage') || 0;
        const cap = getProp(locMatch, 'shelfAvailableCapacity', 'ShelfAvailableCapacity') || 0;

        codeEl.textContent = sCode;
        breadcrumbEl.textContent = path;
        qtyEl.textContent = `${formatNumber(pQty)} units`;
        utilEl.textContent = `${util}%`;
        capEl.textContent = `${formatNumber(cap)} units free`;
    } else if (layoutMatch) {
        const sCode = getProp(layoutMatch, 'shelfCode', 'ShelfCode') || '';
        const path = `${getProp(layoutMatch, 'warehouseName', 'WarehouseName') || ''} > ${getProp(layoutMatch, 'floorName', 'FloorName') || ''} > ${getProp(layoutMatch, 'roomName', 'RoomName') || ''} > ${getProp(layoutMatch, 'rackName', 'RackName') || ''} > ${getProp(layoutMatch, 'shelfName', 'ShelfName') || ''}`;
        const util = getProp(layoutMatch, 'utilization', 'UtilizationPercentage') || 0;
        const maxQ = getProp(layoutMatch, 'maxQty', 'MaxQty') || 0;
        const curQ = getProp(layoutMatch, 'currentQty', 'CurrentQty') || 0;
        const free = Math.max(0, maxQ - curQ);

        codeEl.textContent = sCode;
        breadcrumbEl.textContent = path;
        qtyEl.textContent = '0 units (other products)';
        utilEl.textContent = `${util}%`;
        capEl.textContent = `${formatNumber(free)} units free`;
    }
}

// 6. View Switcher (2D Floor Map vs Rack Tier Diagram)
function initTabSwitcher() {
    const tabs = document.querySelectorAll('.tab-btn');
    tabs.forEach(tab => {
        tab.addEventListener('click', function () {
            tabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');

            const targetId = this.dataset.target;
            document.querySelectorAll('.visual-view').forEach(view => {
                view.style.display = 'none';
            });
            const targetView = document.getElementById(targetId);
            if (targetView) targetView.style.display = 'block';

            if (targetId === 'rackElevationView' && selectedShelfId) {
                renderRackElevation(selectedShelfId);
            }
        });
    });
}

// 7. Initial Selection on Page Load
function initDefaultSelection() {
    if (window.productLocations && window.productLocations.length > 0) {
        const firstShelfId = getProp(window.productLocations[0], 'shelfId', 'shelfID', 'ShelfID');
        if (firstShelfId) selectShelfLocation(firstShelfId);
    } else if (window.warehouseLayoutData && window.warehouseLayoutData.length > 0) {
        const firstLayoutId = getProp(window.warehouseLayoutData[0], 'shelfId', 'shelfID', 'ShelfID');
        if (firstLayoutId) selectShelfLocation(firstLayoutId);
    }

    const printModalBtn = document.getElementById('btnPrintLabelModal');
    if (printModalBtn) {
        printModalBtn.addEventListener('click', function () {
            openPrintModal();
        });
    }
}

// 8. Multi-Shelf Print Modal Management
window.openPrintModal = function (specificShelfId = null) {
    const locs = window.productLocations || [];
    const shelfSelect = document.getElementById('printShelfSelect');
    const formatSelect = document.getElementById('printFormatSelect');
    const multiTableBody = document.getElementById('multiSheetTableBody');

    // Always update Master SKU Card
    const masterSkuEl = document.getElementById('masterTagSKU');
    const masterNameEl = document.getElementById('masterTagProdName');
    if (masterSkuEl) masterSkuEl.textContent = window.currentProductSku || 'SKU';
    if (masterNameEl) masterNameEl.textContent = window.currentProductName || 'Product Name';

    // Populate Target Shelf Dropdown
    if (shelfSelect) {
        shelfSelect.innerHTML = '';
        locs.forEach(loc => {
            const sId = getProp(loc, 'shelfId', 'shelfID', 'ShelfID');
            const sCode = getProp(loc, 'shelfCode', 'ShelfCode') || '';
            const sQty = getProp(loc, 'productCurrentQty', 'ProductCurrentQty') || 0;
            const sTier = getProp(loc, 'tierLevel', 'TierLevel') || 1;
            const opt = document.createElement('option');
            opt.value = sId;
            opt.textContent = `${sCode} (${formatNumber(sQty)} pcs) - Tier ${sTier}`;
            shelfSelect.appendChild(opt);
        });
    }

    // Populate Multi-Shelf Wayfinding Table
    if (multiTableBody) {
        let rowsHtml = '';
        if (locs.length === 0) {
            rowsHtml = '<tr><td colspan="7" class="text-center text-muted p-3">No active shelf stock recorded for this product.</td></tr>';
        } else {
            locs.forEach((loc, idx) => {
                const sCode = getProp(loc, 'shelfCode', 'ShelfCode') || '';
                const path = getProp(loc, 'hierarchyPath', 'HierarchyPath') || `${getProp(loc, 'warehouseName', 'WarehouseName') || ''} > ${getProp(loc, 'floorName', 'FloorName') || ''} > ${getProp(loc, 'roomName', 'RoomName') || ''} > ${getProp(loc, 'rackName', 'RackName') || ''} > ${getProp(loc, 'shelfName', 'ShelfName') || ''}`;
                const sType = getProp(loc, 'shelfType', 'ShelfType') || 'General';
                const sQty = getProp(loc, 'productCurrentQty', 'ProductCurrentQty') || 0;
                const sAvail = getProp(loc, 'productAvailableQty', 'ProductAvailableQty') || 0;

                rowsHtml += `
                    <tr>
                        <td style="font-weight:700; color:#64748b;">#${idx + 1}</td>
                        <td class="doc-shelf-code">${escapeHtml(sCode)}</td>
                        <td class="doc-route-path">${escapeHtml(path)}</td>
                        <td><span style="font-size:0.75rem; background:#f1f5f9; padding:2px 6px; border-radius:3px;">${escapeHtml(sType)}</span></td>
                        <td style="text-align: right; font-weight:700;">${formatNumber(sQty)}</td>
                        <td style="text-align: right; font-weight:700; color:#16a34a;">${formatNumber(sAvail)}</td>
                        <td style="text-align: center;">
                            <div class="verification-box">[ ] Picked: _____</div>
                        </td>
                    </tr>
                `;
            });
        }
        multiTableBody.innerHTML = rowsHtml;
    }

    // Determine default mode
    if (specificShelfId) {
        formatSelect.value = 'single-shelf';
        togglePrintFormat('single-shelf');
        if (shelfSelect) shelfSelect.value = specificShelfId;
        onTargetShelfChanged(specificShelfId);
    } else if (locs.length === 0) {
        formatSelect.value = 'master-sku';
        togglePrintFormat('master-sku');
    } else if (locs.length > 1) {
        formatSelect.value = 'multi-sheet';
        togglePrintFormat('multi-sheet');
    } else {
        formatSelect.value = 'single-shelf';
        togglePrintFormat('single-shelf');
        if (locs.length === 1) {
            const firstId = getProp(locs[0], 'shelfId', 'shelfID', 'ShelfID');
            if (firstId) {
                if (shelfSelect) shelfSelect.value = firstId;
                onTargetShelfChanged(firstId);
            }
        }
    }

    const modalEl = document.getElementById('printLabelModal');
    if (modalEl) {
        const bsModal = new bootstrap.Modal(modalEl);
        bsModal.show();
    }
};

window.togglePrintFormat = function (format) {
    const multiView = document.getElementById('multiSheetView');
    const singleView = document.getElementById('singleTagView');
    const masterView = document.getElementById('masterSkuView');
    const shelfGroup = document.getElementById('shelfSelectGroup');

    if (multiView) multiView.style.display = (format === 'multi-sheet') ? 'block' : 'none';
    if (singleView) singleView.style.display = (format === 'single-shelf') ? 'block' : 'none';
    if (masterView) masterView.style.display = (format === 'master-sku') ? 'block' : 'none';
    if (shelfGroup) shelfGroup.style.display = (format === 'single-shelf') ? 'flex' : 'none';

    if (format === 'single-shelf') {
        const shelfSelect = document.getElementById('printShelfSelect');
        if (shelfSelect && shelfSelect.value) {
            onTargetShelfChanged(shelfSelect.value);
        }
    }
};

window.onTargetShelfChanged = function (shelfId) {
    if (!shelfId) return;
    const locs = window.productLocations || [];
    const loc = locs.find(l => {
        const id = getProp(l, 'shelfId', 'shelfID', 'ShelfID');
        return id !== undefined && id.toString() === shelfId.toString();
    });

    if (loc) {
        const sCode = getProp(loc, 'shelfCode', 'ShelfCode') || 'SHELF-01';
        const sTier = getProp(loc, 'tierLevel', 'TierLevel') || 1;
        const sQty = getProp(loc, 'productCurrentQty', 'ProductCurrentQty') || 0;
        const path = getProp(loc, 'hierarchyPath', 'HierarchyPath') || `${getProp(loc, 'warehouseName', 'WarehouseName') || ''} > ${getProp(loc, 'floorName', 'FloorName') || ''} > ${getProp(loc, 'roomName', 'RoomName') || ''} > ${getProp(loc, 'rackName', 'RackName') || ''} > ${getProp(loc, 'shelfName', 'ShelfName') || ''}`;

        const codeEl = document.getElementById('printTagShelfCode');
        const tierEl = document.getElementById('printTagTier');
        const qtyEl = document.getElementById('printTagQty');
        const addrEl = document.getElementById('printTagAddress');
        const skuEl = document.getElementById('printTagSKU');
        const nameEl = document.getElementById('printTagProdName');

        if (codeEl) codeEl.textContent = sCode;
        if (tierEl) tierEl.textContent = `Tier ${sTier}`;
        if (qtyEl) qtyEl.textContent = `${formatNumber(sQty)} Units`;
        if (addrEl) addrEl.textContent = path;
        if (skuEl) skuEl.textContent = window.currentProductSku || 'SKU';
        if (nameEl) nameEl.textContent = window.currentProductName || 'Product Name';
    }
};

window.printShelfTag = function (shelfCode, shelfName, hierarchyPath, sku, productName, qty) {
    const locs = window.productLocations || [];
    const match = locs.find(l => getProp(l, 'shelfCode', 'ShelfCode') === shelfCode);
    const shelfId = match ? getProp(match, 'shelfId', 'shelfID', 'ShelfID') : null;
    openPrintModal(shelfId);
};

// 9. Barcode Scanner Simulation Button
function initScannerButton() {
    const scanBtn = document.getElementById('scanBarcodeBtn');
    const searchInput = document.getElementById('locatorSearchInput');
    if (!scanBtn || !searchInput) return;

    scanBtn.addEventListener('click', function () {
        searchInput.focus();
        searchInput.select();
        searchInput.placeholder = 'Scanner ready... scan barcode or press Enter';
        searchInput.style.borderColor = '#16a34a';
        setTimeout(() => {
            searchInput.style.borderColor = '';
            searchInput.placeholder = 'Search by SKU, Barcode, or Product Name...';
        }, 3000);
    });
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
