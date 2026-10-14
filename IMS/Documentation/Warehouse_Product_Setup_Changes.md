# Warehouse/Product Setup Changes

Date: 2026-06-18

## Purpose

This change improves warehouse usefulness for putaway and transfer decisions. The system now has explicit product storage rules, realistic product weights, shelf weight-capacity calculation, and server-side validation so stock cannot be placed or transferred into unsuitable shelves.

## Database Changes

Added `Database/06_WarehouseProductStorageSetup.sql`.

The script is idempotent and can be rerun. It performs these changes:

- Adds `ProductStorageRules`.
- Adds active storage rules for seeded products:
  - `RM-COP-005` -> `Raw Material`
  - `PRD-SRV-AX200` -> `Finished Goods`
  - `PKG-BOX-MED` -> `General`
- Updates seeded product weights:
  - `RM-COP-005` = `0.02 kg`
  - `PRD-SRV-AX200` = `7.50 kg`
  - `PKG-BOX-MED` = `0.15 kg`
- Updates seeded product dimensions/descriptions.
- Updates finished-goods shelf `KTM-F1-R02-B01-S1` weight capacity to `500 kg` so current seed stock is valid.
- Adds/updates `dbo.fn_GetShelfAvailableWeightCapacity(@ShelfID)`.

Applied to the local `FactoryIMS` database.

Verification result:

- `ProductStorageRules` active rows: `3`
- Weight capacity function exists.
- Seed shelves now have positive remaining weight capacity.

## Backend Changes

### Putaway

Changed files:

- `Models/DTOs/PutawayDto.cs`
- `Repositories/IPutawayRepository.cs`
- `Repositories/PutawayRepository.cs`
- `Services/IPutawayService.cs`
- `Services/PutawayService.cs`
- `Pages/WarehouseOperations/Putaway.cshtml.cs`

Improvements:

- Added `ShelfOptionDto` for recommended shelf choices.
- Added product/shelf compatibility checks.
- Added quantity-capacity validation before creating or completing putaway.
- Added weight-capacity validation before creating or completing putaway.
- Prevents normal accepted stock from being put away to `QC Hold` shelves.
- Added `/WarehouseOperations/Putaway?handler=SuggestedShelves` JSON endpoint for recommended shelves.

### Stock Transfer

Changed files:

- `Repositories/IStockTransferRepository.cs`
- `Repositories/StockTransferRepository.cs`
- `Services/IStockTransferService.cs`
- `Services/StockTransferService.cs`
- `Pages/Inventory/Transfers.cshtml.cs`

Improvements:

- Validates source available stock.
- Validates destination shelf compatibility.
- Validates destination quantity capacity.
- Validates destination weight capacity.
- Derives transfer type from shelf hierarchy instead of hardcoding `Inter-Warehouse`.
- Fixes shelf product lookup so source shelf selection returns products actually available on that shelf.
- Adds destination shelf recommendation endpoint.

## Frontend Changes

Changed files:

- `Pages/WarehouseOperations/Putaway.cshtml`
- `Pages/Inventory/Transfers.cshtml`
- `wwwroot/js/putaway.js`
- `wwwroot/js/stock-transfers.js`
- `wwwroot/css/ims-common.css`
- `wwwroot/css/putaway.css`
- `wwwroot/css/stock-transfers.css`

Improvements:

- Removed non-functional `Auto-Assign Tasks` button from Putaway.
- Putaway modals now load recommended shelves with quantity and weight capacity.
- Moved Putaway modal behavior from inline Razor script to `wwwroot/js/putaway.js`.
- Transfer modal no longer asks the user to enter transfer number or transfer type; the backend now generates/derives them.
- Transfer modal shows destination shelf capacity advice after source product and quantity are selected.
- Added shared modal, field hint, and capacity advice styles in `ims-common.css`.
- Normalized Putaway and Transfer page card/modal radius toward the same 8px style.

## Verification

Commands run:

```powershell
sqlcmd -S .\SQLEXPRESS -E -i Database\06_WarehouseProductStorageSetup.sql
dotnet build
```

Final build result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Recommended Next Work

- Add UI management page for `ProductStorageRules` so admins can edit rules without SQL.
- Expand warehouse seed/layout beyond 4 shelves for realistic putaway and transfer testing.
- Add shelf capacity report showing quantity utilization and weight utilization together.
- Add automated workflow tests for purchase order -> receiving -> QC -> putaway -> inventory -> transfer.

## Warehouse Setup Page Update

Date: 2026-06-18

Changed files:

- `Pages/Warehouses/Setup.cshtml`
- `Pages/Warehouses/Setup.cshtml.cs`
- `Repositories/WarehouseRepository.cs`
- `wwwroot/css/warehouse-setup.css`
- `wwwroot/js/warehouse-setup.js`

Changes:

- Reworked `/Warehouses/Setup` into a clearer hierarchy-driven layout.
- Removed the non-functional auto-generate shelves button.
- Added the missing Add Warehouse modal.
- Replaced raw Floor ID, Room ID, and Rack ID inputs with real dropdowns.
- Removed fake shelf action icons that did not perform server actions.
- Added combined Warehouse, Shelf Type, Status, and Search filters.
- Implemented the missing live shelf hierarchy query in `WarehouseRepository`.
- Fixed warehouse create/update SQL to use the actual `Location` column instead of `Address`.
- Updated page styling for consistent cards, controls, filters, badges, and tables.
- Replaced noisy console logging with clean client-side filtering.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Warehouse Visualization Page Update

Date: 2026-06-18

Changed files:

- `Pages/Warehouses/Visualization.cshtml`
- `Pages/Warehouses/Visualization.cshtml.cs`
- `Repositories/WarehouseRepository.cs`
- `Models/DTOs/WarehouseSetupDto.cs`
- `wwwroot/css/warehouse-visualization.css`
- `wwwroot/js/warehouse-visualization.js`

Changes:

- Rebuilt `/Warehouses/Visualization` as a live operational view.
- Removed mojibake/static text and fake filter buttons.
- Removed external font links from the page.
- Added summary cards for capacity, rooms, racks, and active shelves.
- Added room utilization and rack utilization sections based on live shelf data.
- Added a shelf map with color-coded utilization states.
- Added working filters for room, shelf type, and search.
- Added clickable shelf tiles that update the details panel.
- Replaced mock current weight calculation with real stock-weight data from SQL.
- Added product/SKU/available quantity display for the selected shelf.
- Added low-stock shelf list using live inventory/reorder values.
- Extended the warehouse repository query to include current stock, current weight, main product, available quantity, and reorder level.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Goods Receiving Page Update

Date: 2026-06-18

Changed files:

- `Pages/WarehouseOperations/Receiving.cshtml`
- `Pages/WarehouseOperations/Receiving.cshtml.cs`
- `Repositories/ReceivingRepository.cs`
- `Models/DTOs/ReceivingDto.cs`
- `wwwroot/css/receiving.css`
- `wwwroot/js/receiving.js`

Changes:

- Reworked `/WarehouseOperations/Receiving` into a clearer purchase-order receiving queue.
- Removed fake/non-working view and send-to-QC icons.
- Added a real QC navigation action for receipts in `QC Required` status.
- Moved receive modal behavior out of inline Razor and into `wwwroot/js/receiving.js`.
- Added working supplier, status, and search filters.
- Added progress display for ordered vs received quantity.
- Added expected date, receipt date, receiver, damaged quantity, and rejected quantity in a cleaner table.
- Improved the receive modal with PO/supplier/product context and remaining quantity guardrails.
- Fixed partial receiving support so partially received PO items remain available for receiving remaining quantity.
- Updated PO status after receipt to `Partial` or `Received` based on all PO line quantities.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)
```

## QC Check Page Update

Date: 2026-06-18

Changed files:

- `Pages/WarehouseOperations/QCCheck.cshtml`
- `Pages/WarehouseOperations/QCCheck.cshtml.cs`
- `wwwroot/css/qc-check.css`
- `wwwroot/js/qc-check.js`

Changes:

- Reworked `/WarehouseOperations/QCCheck` into a clearer QC worklist and history view.
- Removed page-level external font links.
- Removed editable summary inputs that did not save anything.
- Removed the non-functional `Release to Putaway` button.
- Added a real Putaway navigation action for accepted QC rows.
- Replaced cramped inline table inputs with a QC submission modal.
- Added working status and search filters.
- Added modal context for PO, supplier, SKU, product, batch, and received quantity.
- Added client-side validation that accepted + damaged + rejected must equal received quantity.
- Moved QC page behavior into `wwwroot/js/qc-check.js`.
- Rebuilt page styling to match the newer Receiving/Warehouse pages.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Putaway Page Update

Date: 2026-06-18

Changed files:

- `Pages/WarehouseOperations/Putaway.cshtml`
- `Pages/WarehouseOperations/Putaway.cshtml.cs`
- `Repositories/PutawayRepository.cs`
- `wwwroot/css/putaway.css`
- `wwwroot/js/putaway.js`

Changes:

- Reworked `/WarehouseOperations/Putaway` into a cleaner putaway queue with consistent warehouse operation styling.
- Removed the fake/non-working view action from the table.
- Added working product type, status, and search filters.
- Added clearer create-task and complete-putaway icon actions.
- Improved create and complete modals with product, PO, batch, received quantity, and shelf recommendation context.
- Kept shelf recommendations connected to storage rules, product type, available quantity, and weight capacity checks.
- Populated product type and batch number from the putaway SQL query.
- Excluded `QC Hold` shelves from the normal active shelf fallback list.
- Added `General` to the product type filter options.
- Moved Putaway page behavior into a consistent page script with filter handling and shelf hint updates.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Picking & Packing Page Update

Date: 2026-06-18

Changed files:

- `Pages/WarehouseOperations/PickingPacking.cshtml`
- `Pages/WarehouseOperations/PickingPacking.cshtml.cs`
- `Repositories/PickingPackingRepository.cs`
- `wwwroot/css/picking-packing.css`
- `wwwroot/js/picking-packing.js`

Changes:

- Reworked `/WarehouseOperations/PickingPacking` into a cleaner two-panel picking workflow with order queue and selected order detail.
- Removed page-level external font links and replaced inline-heavy styling with a dedicated stylesheet.
- Added working order search and pick-status filters.
- Improved the selected order header with clear allocate, start, mark picked, and pack actions.
- Added workflow status steps for confirmed, allocated, picking, and packing.
- Rebuilt the allocation modal so it can reserve every sales-order line item instead of only the first product.
- Added per-product shelf selection using shelves with available stock for that product.
- Blocked allocation when one or more products do not have available stock on active shelves.
- Excluded `QC Hold` shelves from picking shelf selections.
- Fixed the picking status transition so marking an order as picked sets the sales order to `Picked`, while packing sets it to `Packed`.
- Added client-side queue filtering and allocation shelf validation in `wwwroot/js/picking-packing.js`.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/WarehouseOperations/PickingPacking
StatusCode=200
```

## Dispatch Tracking Page Update

Date: 2026-06-18

Changed files:

- `Pages/WarehouseOperations/DispatchTracking.cshtml`
- `Pages/WarehouseOperations/DispatchTracking.cshtml.cs`
- `Services/DispatchService.cs`
- `wwwroot/css/dispatch-tracking.css`
- `wwwroot/js/dispatch-tracking.js`

Changes:

- Reworked `/WarehouseOperations/DispatchTracking` into a clearer dispatch operations view with ready queue, selected order action panel, and dispatch activity list.
- Removed page-level external font links and inline-heavy styling.
- Added working ready-queue search and priority filtering.
- Added working dispatch activity search and status filtering.
- Fixed order selection so `soid` route values select the intended ready dispatch order.
- Split selected-order behavior correctly: packed orders without a dispatch record show the create-dispatch form, while orders with an existing ready dispatch record show the confirm-dispatch action.
- Added ready dispatch records to the activity list so they can be confirmed from the log as well.
- Added clearer workflow steps for packed, record, confirm, and delivered.
- Added backend validation for vehicle number and tracking number.
- Kept confirm dispatch protected with a confirmation prompt because it reduces stock, consumes reservation, and creates movement records.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/WarehouseOperations/DispatchTracking
StatusCode=200
Protected route returns login markup when unauthenticated.
```

## Production BOM Page Update

Date: 2026-06-18

Changed files:

- `Pages/Production/BOM.cshtml`
- `Pages/Production/BOM.cshtml.cs`
- `wwwroot/css/bom.css`
- `wwwroot/js/bom.js`

Changes:

- Reworked `/Production/BOM` into a production workbench with BOM summary cards, a selectable BOM library, selected product context, component breakdown, and version history.
- Added `bomId` route query support so users can switch between BOM versions/products from the page.
- Replaced hard-coded readiness values with metrics derived from the selected BOM components, including availability score, low/short component count, lead time, and component type count.
- Added working component search across SKU, name, type, and availability with visible row counts.
- Removed fake edit/add/view-all controls and replaced them with real navigation to production orders and products.
- Rebuilt BOM styling for consistent responsive panels, status pills, availability badges, readiness tiles, and component tables.
- Moved page behavior into `wwwroot/js/bom.js`.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/Production/BOM
Protected route returns login markup when unauthenticated.
```

## Purchasing Purchase Orders Page Update

Date: 2026-06-18

Changed files:

- `Pages/Purchasing/PurchaseOrders.cshtml`
- `Pages/Purchasing/PurchaseOrders.cshtml.cs`
- `Services/IPurchaseOrderService.cs`
- `Services/PurchaseOrderService.cs`
- `wwwroot/css/purchase-orders.css`
- `wwwroot/js/purchase-orders.js`

Changes:

- Reworked `/Purchasing/PurchaseOrders` from a flat table into a purchasing workbench with KPI cards, filters, order list, and selected order detail.
- Added seven-day sparkline trends for total POs, draft/confirmed, in-transit, and processing/QC counts.
- Added service access to existing purchase order line items so the selected PO now shows product SKU, ordered quantity, received quantity, and receipt progress.
- Added working client-side search, supplier, and status filters with visible result counts.
- Replaced loose action icons with clear edit, confirm, cancel, and receiving actions based on PO status.
- Moved edit modal population, confirmation prompts, filtering, and sparkline rendering into `wwwroot/js/purchase-orders.js`.
- Rebuilt the page CSS for consistent cards, detail panels, badges, modals, and responsive layout.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/Purchasing/PurchaseOrders
Protected route returns login markup when unauthenticated.
```

## Sales Orders Page Update

Date: 2026-06-18

Changed files:

- `Pages/Sales/Orders.cshtml`
- `Pages/Sales/Orders.cshtml.cs`
- `Services/ISalesOrderService.cs`
- `Services/SalesOrderService.cs`
- `wwwroot/css/sales-orders.css`
- `wwwroot/js/sales-orders.js`

Changes:

- Reworked `/Sales/Orders` into a cleaner sales order workspace with summary KPIs, searchable order list, and selected order detail.
- Added seven-day sparkline trend graphs to the wide KPI cards so the metric containers show historical movement instead of only a single number.
- Removed hard-coded order counts and pagination placeholder text.
- Added working sales order search and status filtering.
- Added `soid` route selection so clicking an order loads that order into the detail panel.
- Loaded selected sales order line items from SQL instead of showing an empty allocation panel.
- Added per-line available stock checks using the existing inventory availability function.
- Moved edit modal population, table filtering, and confirmation prompts into `wwwroot/js/sales-orders.js`.
- Removed page-level external font links and rebuilt the CSS for consistency with the updated operations pages.
- Exposed sales order item and available-stock reads through the sales order service interface.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/Sales/Orders
Protected route returns login markup when unauthenticated.
```

## Sales Customers Page Update

Date: 2026-06-18

Changed files:

- `Pages/Sales/Customers.cshtml`
- `Pages/Sales/Customers.cshtml.cs`
- `wwwroot/css/customers.css`
- `wwwroot/js/customers.js`

Changes:

- Reworked `/Sales/Customers` into a cleaner customer directory consistent with the updated Sales Orders page.
- Added seven-day sparkline trend graphs to the customer KPI cards using customer creation dates.
- Added working search, customer type, and status filters.
- Removed the fake view-profile icon and replaced it with a real navigation action to Sales Orders.
- Moved edit modal population, filtering, and deactivate confirmation into `wwwroot/js/customers.js`.
- Rebuilt table styling, badges, action buttons, summary cards, and modals for consistency.
- Kept customer stats backed by the existing SQL customer/order aggregate data.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/Sales/Customers
Protected route returns login markup when unauthenticated.
```

## Admin Dashboard Page Update

Date: 2026-06-18

Changed files:

- `Pages/Dashboard/Admin.cshtml`
- `Pages/Dashboard/Admin.cshtml.cs`
- `Models/DTOs/DashboardDto.cs`
- `Repositories/IDashboardRepository.cs`
- `Repositories/DashboardRepository.cs`
- `Services/IDashboardService.cs`
- `Services/DashboardService.cs`
- `wwwroot/css/admin-dashboard.css`
- `wwwroot/js/admin-dashboard.js`

Changes:

- Reworked `/Dashboard/Admin` into a visual Chart.js-based admin dashboard.
- Added a real warehouse dropdown backed by active warehouses from SQL.
- Made dashboard summary, low-stock alerts, shelf utilization, movement list, stock value breakdown, and movement trend data respect the selected warehouse.
- Replaced warehouse utilization text/progress list with a horizontal bar chart using green/orange/red threshold coloring.
- Added stock value breakdown doughnut chart with total stock value rendered in the chart center.
- Added seven-day inbound versus outbound stock movement line chart with smooth curves and gradient fill.
- Added daily fulfillment semi-doughnut gauge against a target of 20 shipments.
- Rebuilt dashboard CSS for responsive KPI cards, chart panels, operation tables, and alert cards.
- Removed page-level external font links and moved all chart behavior into `wwwroot/js/admin-dashboard.js`.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/Dashboard/Admin
Protected route returns login markup when unauthenticated.
```

## Purchasing Suppliers Page Update

Date: 2026-06-18

Changed files:

- `Pages/Purchasing/Suppliers.cshtml`
- `Pages/Purchasing/Suppliers.cshtml.cs`
- `Pages/WarehouseOperations/PickingPacking.cshtml.cs`
- `wwwroot/css/suppliers.css`
- `wwwroot/js/suppliers.js`

Changes:

- Reworked `/Purchasing/Suppliers` into a cleaner supplier directory consistent with the updated Sales Customers page.
- Added seven-day sparkline trend graphs to supplier KPI cards using supplier creation dates.
- Added working search, supplier type, and status filters.
- Removed the fake view action and replaced it with a real Purchase Orders navigation action.
- Moved edit modal population, filtering, and deactivate confirmation into `wwwroot/js/suppliers.js`.
- Rebuilt table styling, badges, action buttons, summary cards, and modals for consistency.
- Preserved live supplier stats from the existing SQL-backed supplier service.
- Fixed a nullable warning in the Picking/Packing create-task fallback list initialization.

Verification:

```text
dotnet build
Build succeeded.
0 Warning(s)
0 Error(s)

GET http://localhost:5237/Purchasing/Suppliers
Protected route returns login markup when unauthenticated.
```
