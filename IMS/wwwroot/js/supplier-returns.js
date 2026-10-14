document.addEventListener("DOMContentLoaded", function () {
    // ----------------------------------------------------
    // 1. Search and Filtering Logs
    // ----------------------------------------------------
    const searchInput = document.getElementById("returnSearch");
    const supplierFilter = document.getElementById("supplierFilter");
    const statusFilter = document.getElementById("statusFilter");
    const creditFilter = document.getElementById("creditFilter");
    const returnRows = document.querySelectorAll(".return-row");
    const visibleCountEl = document.getElementById("visibleReturnCount");
    const emptyStateEl = document.querySelector(".returns-panel .empty-state");

    function applyFilters() {
        const query = searchInput ? searchInput.value.toLowerCase().trim() : "";
        const supplier = supplierFilter ? supplierFilter.value : "All Suppliers";
        const status = statusFilter ? statusFilter.value : "All Statuses";
        const credit = creditFilter ? creditFilter.value : "All Credits";

        let visibleCount = 0;

        returnRows.forEach(row => {
            const searchData = row.getAttribute("data-search").toLowerCase();
            const rowSupplier = row.getAttribute("data-supplier");
            const rowStatus = row.getAttribute("data-status");
            const rowCredit = row.getAttribute("data-credit");

            const matchesQuery = query === "" || searchData.includes(query);
            const matchesSupplier = supplier === "All Suppliers" || rowSupplier === supplier;
            const matchesStatus = status === "All Statuses" || rowStatus === status;
            const matchesCredit = credit === "All Credits" || rowCredit === credit;

            if (matchesQuery && matchesSupplier && matchesStatus && matchesCredit) {
                row.style.display = "";
                visibleCount++;
            } else {
                row.style.display = "none";
            }
        });

        if (visibleCountEl) {
            visibleCountEl.textContent = visibleCount;
        }

        if (emptyStateEl) {
            emptyStateEl.style.display = visibleCount === 0 ? "flex" : "none";
        }
    }

    if (searchInput) searchInput.addEventListener("input", applyFilters);
    if (supplierFilter) supplierFilter.addEventListener("change", applyFilters);
    if (statusFilter) statusFilter.addEventListener("change", applyFilters);
    if (creditFilter) creditFilter.addEventListener("change", applyFilters);

    // ----------------------------------------------------
    // 2. Update Credit Modal Population
    // ----------------------------------------------------
    const updateCreditModal = document.getElementById("updateCreditModal");
    if (updateCreditModal) {
        updateCreditModal.addEventListener("show.bs.modal", function (event) {
            const button = event.relatedTarget;
            const returnId = button.getAttribute("data-return-id");
            const currentStatus = button.getAttribute("data-credit-status");

            document.getElementById("modalCreditReturnID").value = returnId;

            const radioPending = document.getElementById("optCreditPending");
            const radioIssued = document.getElementById("optCreditIssued");
            const radioDenied = document.getElementById("optCreditDenied");

            if (currentStatus === "Pending" && radioPending) radioPending.checked = true;
            if (currentStatus === "Issued" && radioIssued) radioIssued.checked = true;
            if (currentStatus === "Denied" && radioDenied) radioDenied.checked = true;
        });
    }

    // ----------------------------------------------------
    // 3. Create Return Modal Logic (Autofill & Dynamic List)
    // ----------------------------------------------------
    const createReturnModal = document.getElementById("createReturnModal");
    const supplierSelect = document.getElementById("newReturnSupplier");
    const carrierInput = document.getElementById("newReturnCarrier");
    const itemSelect = document.getElementById("quarantineItemSelect");
    const itemQty = document.getElementById("itemQty");
    const itemUnitPrice = document.getElementById("itemUnitPrice");
    const itemReason = document.getElementById("itemReason");
    const lblAvailQty = document.getElementById("lblAvailQty");
    const btnAddItem = document.getElementById("btnAddReturnItem");
    const addedItemsList = document.getElementById("addedItemsList");
    const emptyRow = document.getElementById("emptyItemsRow");
    const tableFoot = document.getElementById("itemsTableFoot");
    const lblGrandTotal = document.getElementById("lblGrandTotal");
    const btnSubmit = document.getElementById("btnSubmitReturn");
    const itemsJsonInput = document.getElementById("NewReturnItemsJson");

    let addedItems = [];

    // Reset modal state on show
    if (createReturnModal) {
        createReturnModal.addEventListener("show.bs.modal", function () {
            // Reset form fields
            document.getElementById("createReturnForm").reset();
            addedItems = [];
            updateItemsTable();

            // Populate quarantine dropdown
            populateQuarantineDropdown();

            // Hide policy panel
            const policyPanel = document.getElementById("policyPanel");
            if (policyPanel) policyPanel.classList.add("d-none");
        });
    }

    // Populate the quarantine stock select dropdown
    function populateQuarantineDropdown() {
        if (!itemSelect) return;
        itemSelect.innerHTML = '<option value="">-- Choose Item from Quarantine --</option>';

        if (window.quarantineStock && Array.isArray(window.quarantineStock)) {
            window.quarantineStock.forEach(stock => {
                const option = document.createElement("option");
                option.value = `${stock.productID}_${stock.shelfID}`;
                option.textContent = `${stock.sku} - ${stock.productName} (Shelf: ${stock.shelfCode}, Available: ${stock.currentQty.toFixed(2)})`;
                // Store data attributes on option for easy retrieval
                option.setAttribute("data-product-id", stock.productID);
                option.setAttribute("data-sku", stock.sku);
                option.setAttribute("data-name", stock.productName);
                option.setAttribute("data-shelf-id", stock.shelfID);
                option.setAttribute("data-shelf-code", stock.shelfCode);
                option.setAttribute("data-qty", stock.currentQty);
                option.setAttribute("data-price", stock.unitPrice);

                itemSelect.appendChild(option);
            });
        }
    }

    // Handle supplier choice -> autofill policy and carrier
    if (supplierSelect) {
        supplierSelect.addEventListener("change", function () {
            const supplierId = parseInt(supplierSelect.value);
            const policyPanel = document.getElementById("policyPanel");
            const policyEmail = document.getElementById("policyEmail");
            const policyCarrier = document.getElementById("policyCarrier");
            const policyText = document.getElementById("policyText");

            if (!supplierId || isNaN(supplierId)) {
                if (policyPanel) policyPanel.classList.add("d-none");
                return;
            }

            // Find details in window.activeSuppliers
            const sup = (window.activeSuppliers || []).find(s => s.supplierID === supplierId);
            if (sup) {
                if (policyPanel) policyPanel.classList.remove("d-none");
                if (policyEmail) policyEmail.textContent = sup.returnEmail || "Not specified";
                if (policyCarrier) policyCarrier.textContent = sup.preferredCarrier || "Not specified";
                if (policyText) policyText.textContent = sup.returnPolicy || "No return policy details on file.";

                // Autofill default carrier if not already typed
                if (carrierInput && sup.preferredCarrier && carrierInput.value === "") {
                    carrierInput.value = sup.preferredCarrier;
                }
            } else {
                if (policyPanel) policyPanel.classList.add("d-none");
            }
        });
    }

    // Handle quarantine item dropdown select -> autofill unit cost and show available limit
    if (itemSelect) {
        itemSelect.addEventListener("change", function () {
            const selectedOpt = itemSelect.options[itemSelect.selectedIndex];
            if (!selectedOpt || selectedOpt.value === "") {
                if (lblAvailQty) lblAvailQty.textContent = "0.00";
                if (itemQty) {
                    itemQty.value = "1";
                    itemQty.removeAttribute("max");
                }
                if (itemUnitPrice) itemUnitPrice.value = "0.00";
                return;
            }

            const availQty = parseFloat(selectedOpt.getAttribute("data-qty"));
            const standardCost = parseFloat(selectedOpt.getAttribute("data-price"));

            if (lblAvailQty) lblAvailQty.textContent = availQty.toFixed(2);
            if (itemQty) {
                itemQty.value = "1";
                itemQty.max = availQty;
            }
            if (itemUnitPrice) itemUnitPrice.value = standardCost.toFixed(2);
        });
    }

    // Handle Add Item to Return List
    if (btnAddItem) {
        btnAddItem.addEventListener("click", function () {
            const selectedOpt = itemSelect.options[itemSelect.selectedIndex];
            if (!selectedOpt || selectedOpt.value === "") {
                alert("Please select an item from Quarantine.");
                return;
            }

            const pId = parseInt(selectedOpt.getAttribute("data-product-id"));
            const sId = parseInt(selectedOpt.getAttribute("data-shelf-id"));
            const sku = selectedOpt.getAttribute("data-sku");
            const name = selectedOpt.getAttribute("data-name");
            const code = selectedOpt.getAttribute("data-shelf-code");
            const maxQty = parseFloat(selectedOpt.getAttribute("data-qty"));

            const qty = parseFloat(itemQty.value);
            const price = parseFloat(itemUnitPrice.value);
            const reason = itemReason.value.trim();

            if (isNaN(qty) || qty <= 0) {
                alert("Please enter a valid quantity greater than zero.");
                return;
            }

            if (qty > maxQty) {
                alert(`Insufficient stock. Maximum returnable quantity is ${maxQty.toFixed(2)}.`);
                return;
            }

            if (isNaN(price) || price < 0) {
                alert("Please enter a valid unit cost.");
                return;
            }

            // Check if item on this shelf is already added
            const existingIndex = addedItems.findIndex(i => i.productID === pId && i.sourceShelfID === sId);
            if (existingIndex > -1) {
                // Check if total exceeds max
                const combinedQty = addedItems[existingIndex].quantity + qty;
                if (combinedQty > maxQty) {
                    alert(`Combined return quantity (${combinedQty.toFixed(2)}) exceeds available quarantine stock (${maxQty.toFixed(2)}).`);
                    return;
                }
                addedItems[existingIndex].quantity = combinedQty;
                if (reason !== "") {
                    addedItems[existingIndex].reason = reason;
                }
            } else {
                addedItems.push({
                    productID: pId,
                    sku: sku,
                    productName: name,
                    sourceShelfID: sId,
                    sourceShelfCode: code,
                    quantity: qty,
                    unitPrice: price,
                    reason: reason || "Returned to Vendor"
                });
            }

            // Reset item add row
            itemSelect.selectedIndex = 0;
            itemQty.value = "1";
            itemUnitPrice.value = "0.00";
            itemReason.value = "";
            if (lblAvailQty) lblAvailQty.textContent = "0.00";

            updateItemsTable();
        });
    }

    // Refresh table markup and serialize to input
    function updateItemsTable() {
        if (!addedItemsList) return;

        // Clear existing list
        addedItemsList.querySelectorAll("tr:not(#emptyItemsRow)").forEach(tr => tr.remove());

        if (addedItems.length === 0) {
            if (emptyRow) emptyRow.style.display = "";
            if (tableFoot) tableFoot.classList.add("d-none");
            if (btnSubmit) btnSubmit.disabled = true;
            if (itemsJsonInput) itemsJsonInput.value = "[]";
            return;
        }

        if (emptyRow) emptyRow.style.display = "none";
        if (tableFoot) tableFoot.classList.remove("d-none");
        if (btnSubmit) btnSubmit.disabled = false;

        let grandTotal = 0;

        addedItems.forEach((item, index) => {
            const total = item.quantity * item.unitPrice;
            grandTotal += total;

            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td>
                    <div style="font-weight:600; color:var(--returns-ink);">${escapeHtml(item.productName)}</div>
                    <div style="font-size:11px; color:var(--returns-muted);">${escapeHtml(item.sku)}</div>
                </td>
                <td><span class="shelf-tag">${escapeHtml(item.sourceShelfCode)}</span></td>
                <td class="text-end font-mono">${item.quantity.toFixed(2)}</td>
                <td class="text-end font-mono">Rs. ${item.unitPrice.toFixed(2)}</td>
                <td class="text-end font-mono">Rs. ${total.toFixed(2)}</td>
                <td class="text-muted" style="font-size:12px;">${escapeHtml(item.reason)}</td>
                <td class="text-center">
                    <button type="button" class="btn btn-link btn-sm text-danger p-0 delete-item-btn" data-index="${index}">
                        <span class="material-symbols-outlined" style="font-size:18px; vertical-align:middle;">delete</span>
                    </button>
                </td>
            `;

            addedItemsList.appendChild(tr);
        });

        if (lblGrandTotal) {
            lblGrandTotal.textContent = `Rs. ${grandTotal.toFixed(2)}`;
        }

        // Serialize to JSON input
        if (itemsJsonInput) {
            // Map to the format expected by the DTO: ProductID, Quantity, UnitPrice, Reason, SourceShelfID
            const cleanList = addedItems.map(i => ({
                productID: i.productID,
                quantity: i.quantity,
                unitPrice: i.unitPrice,
                reason: i.reason,
                sourceShelfID: i.sourceShelfID
            }));
            itemsJsonInput.value = JSON.stringify(cleanList);
        }
    }

    // Handle delete item from creation table
    if (addedItemsList) {
        addedItemsList.addEventListener("click", function (event) {
            const deleteBtn = event.target.closest(".delete-item-btn");
            if (deleteBtn) {
                const index = parseInt(deleteBtn.getAttribute("data-index"));
                addedItems.splice(index, 1);
                updateItemsTable();
            }
        });
    }

    // Handle Supplier Authorize Modal Population
    const supplierAuthorizeModal = document.getElementById("supplierAuthorizeModal");
    if (supplierAuthorizeModal) {
        supplierAuthorizeModal.addEventListener("show.bs.modal", function (event) {
            const button = event.relatedTarget;
            const returnId = button.getAttribute("data-return-id");
            const rma = button.getAttribute("data-rma") || "";
            const carrier = button.getAttribute("data-carrier") || "";
            const tracking = button.getAttribute("data-tracking") || "";

            document.getElementById("modalAuthorizeReturnID").value = returnId;
            document.getElementById("modalAuthorizeRMANumber").value = rma;
            document.getElementById("modalAuthorizeCarrier").value = carrier;
            document.getElementById("modalAuthorizeTracking").value = tracking;
        });
    }

    // Helper to sanitize HTML strings
    function escapeHtml(str) {
        if (!str) return "";
        return str
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
});
