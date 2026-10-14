// Warehouse Visualization JavaScript - FactoryIMS
document.addEventListener('DOMContentLoaded', function () {
    const dataEl = document.getElementById('vizDataJson');
    if (!dataEl) return;

    let vizData = {};
    try {
        vizData = JSON.parse(dataEl.textContent || '{}');
    } catch (e) {
        console.error('Failed to parse warehouse visualization data:', e);
    }

    const shelves = vizData.shelves || [];
    const rooms = vizData.rooms || [];
    const racks = vizData.racks || [];

    // Safe property getter
    function getProp(obj, ...keys) {
        if (!obj) return undefined;
        for (const k of keys) {
            if (obj[k] !== undefined && obj[k] !== null) return obj[k];
            const lower = k.toLowerCase();
            const foundKey = Object.keys(obj).find(x => x.toLowerCase() === lower);
            if (foundKey && obj[foundKey] !== undefined && obj[foundKey] !== null) return obj[foundKey];
        }
        return undefined;
    }

    // Safe Number Formatter (Prevents NaN)
    function formatNumber(val, digits = 0) {
        const num = Number(val);
        if (isNaN(num)) return '0';
        return num.toLocaleString(undefined, {
            minimumFractionDigits: digits,
            maximumFractionDigits: digits
        });
    }

    // Escape HTML
    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    // Detail Panel DOM Elements
    const dom = {
        code: document.getElementById('detailCode'),
        path: document.getElementById('detailPath'),
        status: document.getElementById('detailStatus'),
        product: document.getElementById('detailProduct'),
        sku: document.getElementById('detailSku'),
        type: document.getElementById('detailType'),
        util: document.getElementById('detailUtil'),
        utilBar: document.getElementById('detailUtilBar'),
        currentQty: document.getElementById('detailCurrentQty'),
        availableQty: document.getElementById('detailAvailableQty'),
        maxQty: document.getElementById('detailMaxQty'),
        availCap: document.getElementById('detailAvailCap'),
        currentWeight: document.getElementById('detailCurrentWeight'),
        maxWeight: document.getElementById('detailMaxWeight'),
        floor: document.getElementById('detailFloor'),
        room: document.getElementById('detailRoom'),
        rack: document.getElementById('detailRack'),
        locateBtn: document.getElementById('btnLocateProduct'),
        rackHeaderRoom: document.getElementById('rackHeaderRoom'),
        rackHeaderCode: document.getElementById('rackHeaderCode'),
        rackSelector: document.getElementById('rackSelector'),
        rackTiersContainer: document.getElementById('rackTiersContainer')
    };

    // 1. Select Shelf by ID and update Details Inspector
    window.selectShelfById = function (shelfId) {
        if (!shelfId) return;
        const idNum = Number(shelfId);
        const shelf = shelves.find(s => Number(getProp(s, 'shelfId', 'shelfID', 'ShelfID')) === idNum);
        if (!shelf) return;

        const code = getProp(shelf, 'code', 'shelfCode', 'Code') || '';
        const name = getProp(shelf, 'productName', 'ProductName') || 'Empty Shelf';
        const sku = getProp(shelf, 'sku', 'SKU') || '';
        const type = getProp(shelf, 'shelfType', 'ShelfType') || 'General';
        const status = getProp(shelf, 'status', 'Status') || 'Available';
        const statusKey = getProp(shelf, 'statusKey', 'StatusKey') || 'empty';
        const curQty = getProp(shelf, 'currentQty', 'CurrentQty') || 0;
        const maxQty = getProp(shelf, 'maxQty', 'MaxQty') || 0;
        const availQty = getProp(shelf, 'availableQty', 'AvailableQty') || 0;
        const availCap = getProp(shelf, 'availableCapacity', 'AvailableCapacity') || Math.max(0, maxQty - curQty);
        const curWeight = getProp(shelf, 'currentWeight', 'CurrentWeight') || 0;
        const maxWeight = getProp(shelf, 'maxWeight', 'MaxWeight') || 0;
        const util = getProp(shelf, 'utilizationPercentage', 'UtilizationPercentage') || 0;
        const path = getProp(shelf, 'path', 'Path') || '';
        const floor = getProp(shelf, 'floor', 'Floor') || '';
        const floorCode = getProp(shelf, 'floorCode', 'FloorCode') || '';
        const room = getProp(shelf, 'room', 'Room') || '';
        const roomCode = getProp(shelf, 'roomCode', 'RoomCode') || '';
        const rack = getProp(shelf, 'rack', 'Rack') || '';
        const rackCode = getProp(shelf, 'rackCode', 'RackCode') || '';

        // Update DOM elements
        if (dom.code) dom.code.textContent = code;
        if (dom.path) dom.path.textContent = path || `${floorCode} > ${roomCode} > ${rackCode} > ${code}`;
        if (dom.status) {
            dom.status.textContent = status;
            dom.status.className = `status-pill status-pill-${statusKey}`;
        }
        if (dom.product) dom.product.textContent = name;
        if (dom.sku) dom.sku.textContent = sku ? `SKU: ${sku}` : 'No SKU assigned';
        if (dom.type) dom.type.textContent = type;
        if (dom.util) dom.util.textContent = `${formatNumber(util)}%`;
        if (dom.utilBar) dom.utilBar.style.width = `${Math.min(Number(util), 100)}%`;
        if (dom.currentQty) dom.currentQty.textContent = `${formatNumber(curQty)} units`;
        if (dom.availableQty) dom.availableQty.textContent = `${formatNumber(availQty)} units`;
        if (dom.maxQty) dom.maxQty.textContent = `${formatNumber(maxQty)} units`;
        if (dom.availCap) dom.availCap.textContent = `${formatNumber(availCap)} units`;
        if (dom.currentWeight) dom.currentWeight.textContent = `${formatNumber(curWeight, 2)} kg`;
        if (dom.maxWeight) dom.maxWeight.textContent = `${formatNumber(maxWeight, 2)} kg`;
        if (dom.floor) dom.floor.textContent = floor ? `${floor} (${floorCode})` : floorCode;
        if (dom.room) dom.room.textContent = room ? `${room} (${roomCode})` : roomCode;
        if (dom.rack) dom.rack.textContent = rack ? `${rack} (${rackCode})` : rackCode;

        if (dom.locateBtn) {
            if (sku) {
                dom.locateBtn.href = `/Warehouses/ProductLocator?sku=${encodeURIComponent(sku)}`;
                dom.locateBtn.style.display = 'flex';
            } else {
                dom.locateBtn.href = `/Warehouses/ProductLocator`;
                dom.locateBtn.style.display = 'flex';
            }
        }

        // Highlight active shelf node
        document.querySelectorAll('.shelf-node').forEach(node => {
            const nodeShelfId = Number(node.dataset.shelfId);
            node.classList.toggle('active', nodeShelfId === idNum);
        });
    };

    // 2. Select Room and update Racks
    window.selectRoom = function (roomId) {
        const idNum = Number(roomId);
        document.querySelectorAll('.room-card').forEach(card => {
            const cardId = Number(card.dataset.roomId);
            card.classList.toggle('active', cardId === idNum);
        });

        const selectedRoom = rooms.find(r => Number(getProp(r, 'roomId', 'RoomID')) === idNum);
        const roomRacks = racks.filter(rk => Number(getProp(rk, 'roomId', 'RoomID')) === idNum);

        if (dom.rackSelector) {
            dom.rackSelector.innerHTML = '';
            if (roomRacks.length === 0) {
                const opt = document.createElement('option');
                opt.value = '';
                opt.textContent = 'No racks in this room';
                dom.rackSelector.appendChild(opt);
            } else {
                roomRacks.forEach(rk => {
                    const rkId = getProp(rk, 'rackId', 'RackID');
                    const rkCode = getProp(rk, 'rackCode', 'RackCode');
                    const rkUtil = getProp(rk, 'utilizationPercentage', 'UtilizationPercentage') || 0;
                    const opt = document.createElement('option');
                    opt.value = rkId;
                    opt.textContent = `Rack ${rkCode} (${rkUtil}% util)`;
                    dom.rackSelector.appendChild(opt);
                });
            }
        }

        if (roomRacks.length > 0) {
            const firstRackId = getProp(roomRacks[0], 'rackId', 'RackID');
            renderRackElevation(firstRackId, selectedRoom);
        } else {
            if (dom.rackTiersContainer) {
                dom.rackTiersContainer.innerHTML = '<div class="empty-rack-msg">No racks or shelves configured in this room.</div>';
            }
            if (dom.rackHeaderRoom && selectedRoom) {
                dom.rackHeaderRoom.textContent = getProp(selectedRoom, 'roomName', 'RoomName') || '';
            }
            if (dom.rackHeaderCode) {
                dom.rackHeaderCode.textContent = 'None';
            }
        }
    };

    // 3. Render Rack Elevation for specified Rack
    function renderRackElevation(rackId, roomObj = null) {
        const idNum = Number(rackId);
        const rackShelves = shelves.filter(s => Number(getProp(s, 'rackId', 'RackID', 'rackID')) === idNum);

        const rackObj = racks.find(rk => Number(getProp(rk, 'rackId', 'RackID')) === idNum);
        const rkCode = rackObj ? getProp(rackObj, 'rackCode', 'RackCode') : '';
        const rkRoomId = rackObj ? Number(getProp(rackObj, 'roomId', 'RoomID')) : null;
        const rm = roomObj || rooms.find(r => Number(getProp(r, 'roomId', 'RoomID')) === rkRoomId);

        if (dom.rackHeaderRoom && rm) dom.rackHeaderRoom.textContent = getProp(rm, 'roomName', 'RoomName') || '';
        if (dom.rackHeaderCode) dom.rackHeaderCode.textContent = rkCode || `Rack #${idNum}`;
        if (dom.rackSelector) dom.rackSelector.value = idNum;

        if (!dom.rackTiersContainer) return;

        if (rackShelves.length === 0) {
            dom.rackTiersContainer.innerHTML = '<div class="empty-rack-msg">No shelves configured in this rack.</div>';
            return;
        }

        // Determine tiers for shelves (Level 4 down to Level 1)
        const tierMap = { 4: [], 3: [], 2: [], 1: [] };

        rackShelves.forEach((s, idx) => {
            const code = getProp(s, 'code', 'shelfCode', 'Code') || '';
            let tier = 1;
            const parts = code.split('-');
            if (parts.length > 0) {
                const last = parts[parts.length - 1].trim();
                if (last.toUpperCase().startsWith('S') && !isNaN(parseInt(last.substring(1)))) {
                    tier = parseInt(last.substring(1));
                } else if (!isNaN(parseInt(last))) {
                    tier = parseInt(last);
                }
            }
            if (isNaN(tier) || tier < 1) tier = ((idx) % 4) + 1;
            if (tier > 4) tier = 4;

            tierMap[tier] = tierMap[tier] || [];
            tierMap[tier].push(s);
        });

        let html = '';
        for (let lvl = 4; lvl >= 1; lvl--) {
            const slots = tierMap[lvl] || [];
            html += `
                <div class="rack-tier-level" data-level="${lvl}">
                    <div class="tier-indicator-label">Level ${lvl}</div>
                    <div class="tier-slots-row">
            `;

            if (slots.length === 0) {
                html += '<div class="empty-tier-slot">Available Tier Space</div>';
            } else {
                slots.forEach(slot => {
                    const sId = getProp(slot, 'shelfId', 'shelfID', 'ShelfID');
                    const sCode = getProp(slot, 'code', 'shelfCode', 'Code') || '';
                    const sType = getProp(slot, 'shelfType', 'ShelfType') || 'General';
                    const sUtil = getProp(slot, 'utilizationPercentage', 'UtilizationPercentage') || 0;
                    let sKey = getProp(slot, 'statusKey', 'StatusKey') || 'status-empty';
                    if (!sKey.startsWith('status-')) sKey = 'status-' + sKey;
                    const sProd = getProp(slot, 'productName', 'ProductName') || 'Empty';

                    html += `
                        <button type="button"
                                class="shelf-node ${escapeHtml(sKey)}"
                                data-shelf-id="${sId}"
                                data-shelf-code="${escapeHtml(sCode)}"
                                title="${escapeHtml(sCode)} - ${escapeHtml(sProd)} (${formatNumber(sUtil)}% utilized)"
                                onclick="selectShelfById(${sId})">
                            <span class="shelf-node-code">${escapeHtml(sCode)}</span>
                            <span class="shelf-node-type">${escapeHtml(sType)}</span>
                            <span class="shelf-node-util">${formatNumber(sUtil)}%</span>
                        </button>
                    `;
                });
            }

            html += `
                    </div>
                </div>
            `;
        }

        dom.rackTiersContainer.innerHTML = html;

        // Auto select first shelf in the rack
        if (rackShelves.length > 0) {
            const firstActive = rackShelves.find(s => (getProp(s, 'currentQty', 'CurrentQty') || 0) > 0) || rackShelves[0];
            const firstId = getProp(firstActive, 'shelfId', 'shelfID', 'ShelfID');
            selectShelfById(firstId);
        }
    }

    // 4. Rack select dropdown change event
    window.onRackSelectChange = function (rackId) {
        if (!rackId) return;
        renderRackElevation(rackId);
    };

    // 5. Table click event from lower table
    window.onTableRackClick = function (rackId) {
        if (!rackId) return;
        const idNum = Number(rackId);
        const targetRack = racks.find(rk => Number(getProp(rk, 'rackId', 'RackID')) === idNum);
        if (targetRack) {
            const rmId = Number(getProp(targetRack, 'roomId', 'RoomID'));
            if (rmId) {
                // Update room card highlight
                document.querySelectorAll('.room-card').forEach(card => {
                    const cardId = Number(card.dataset.roomId);
                    card.classList.toggle('active', cardId === rmId);
                });
            }
        }
        renderRackElevation(idNum);

        // Smooth scroll up to rack elevation
        const panel = document.querySelector('.rack-view-panel');
        if (panel) {
            panel.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    };

    // Initial setup: If active shelf is already selected, highlight it
    const initShelfId = vizData.shelfId || (shelves.length > 0 ? getProp(shelves[0], 'shelfId', 'shelfID') : null);
    if (initShelfId) {
        selectShelfById(initShelfId);
    }
});
