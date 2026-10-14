# GEMINI.md

## Project Overview

This project is an Inventory Management System for a factory / warehouse / garment-related business.

The system manages the complete inventory life cycle, including:

* Product / SKU master
* Supplier and customer master
* Warehouse layout
* Floor, room, rack, and shelf storage
* Inventory overview
* Stock movements
* Receiving
* Quality control
* Putaway
* Transfers
* Adjustments
* Cycle counts
* Purchase orders
* Sales orders
* Picking
* Packing
* Dispatch
* Bill of Materials
* Production material issue
* Finished goods receipt
* Reports and dashboards

## Technology Stack

* ASP.NET Core Razor Pages
* .NET 10 LTS
* SQL Server
* HTML
* CSS
* JavaScript
* Bootstrap-compatible layout
* Visual Studio
* Multi-page application, not SPA

## Very Important Rules

* Use ASP.NET Core Razor Pages only.
* Do not use React.
* Do not use Next.js.
* Do not use Angular.
* Do not use Vue.
* Do not use Tailwind unless explicitly requested.
* Do not install npm packages unless explicitly required.
* Do not convert the project into a SPA.
* Do not modify Program.cs unless absolutely required.
* Do not modify _Layout.cshtml unless absolutely required.
* Do not change existing working routes without asking.
* Do not rename existing folders unless instructed.
* Keep the project buildable after every change.
* Always try to make `dotnet build` succeed.

## Project Architecture

Use layered architecture.

Recommended structure:

```text
FactoryIMS
│
├── Pages
│   ├── Dashboard
│   ├── Products
│   ├── Suppliers
│   ├── Customers
│   ├── Warehouses
│   ├── Inventory
│   ├── Purchasing
│   ├── WarehouseOperations
│   ├── Sales
│   ├── Production
│   ├── Reports
│   ├── Users
│   └── Shared
│
├── Models
├── DTOs
├── Services
├── Repositories
├── Data
└── wwwroot
    ├── css
    ├── js
    ├── images
    └── data
```

## Layer Responsibilities

### Razor Pages

Razor Pages should only handle:

* UI display
* Form binding
* Basic page-level validation
* Calling services
* Showing result messages

Do not put heavy business logic directly inside `.cshtml.cs`.

### Services

Services should contain:

* Business rules
* Workflow validation
* Stock operation validation
* Approval checks
* Role-based process decisions

### Repositories

Repositories should handle:

* SQL Server access
* Stored procedure calls
* View queries
* Data mapping

### SQL Server

SQL Server should contain:

* Tables
* Views
* Functions
* Stored procedures
* Inventory transaction logic

Use stored procedures especially for stock-changing operations.

## Razor Pages Rules

Every page should usually have:

```text
PageName.cshtml
PageName.cshtml.cs
```

For repeated UI data, use mock data in the PageModel first.

Example:

```csharp
public List<SummaryCard> SummaryCards { get; set; } = new();

public void OnGet()
{
    SummaryCards = new List<SummaryCard>
    {
        new SummaryCard { Title = "Total Stock Value", Value = "Rs. 450,000" },
        new SummaryCard { Title = "Low Stock Items", Value = "12" },
        new SummaryCard { Title = "Pending Purchase Orders", Value = "8" },
        new SummaryCard { Title = "Daily Shipments", Value = "24" }
    };
}
```

In `.cshtml`, use `foreach` instead of repeating static HTML.

Example:

```cshtml
@foreach (var card in Model.SummaryCards)
{
    <div class="summary-card">
        <h3>@card.Title</h3>
        <p>@card.Value</p>
    </div>
}
```

## CSS and JavaScript Rules

Keep CSS inside:

```text
wwwroot/css
```

Keep JavaScript inside:

```text
wwwroot/js
```

Use page-specific CSS files where possible.

Example:

```text
wwwroot/css/admin-dashboard.css
wwwroot/css/inventory-overview.css
wwwroot/css/purchase-order.css
wwwroot/css/warehouse-visualization.css
```

Link CSS in Razor Pages like this:

```cshtml
@section Styles {
    <link rel="stylesheet" href="~/css/inventory-overview.css" asp-append-version="true" />
}
```

Link JavaScript like this:

```cshtml
@section Scripts {
    <script src="~/js/inventory-overview.js" asp-append-version="true"></script>
}
```

Do not put large CSS blocks inside `.cshtml`.

Do not put large JavaScript blocks inside `.cshtml`.

## UI Design Style

Use a clean, modern, white-background dashboard style.

Use this color system:

```css
:root {
    --primary: #2563eb;
    --bg: #f5f7fb;
    --surface: #ffffff;
    --text: #111827;
    --muted: #6b7280;
    --border: #e5e7eb;
    --success: #16a34a;
    --warning: #f59e0b;
    --danger: #dc2626;
    --info: #0284c7;
}
```

General UI rules:

* Use white cards.
* Use soft borders.
* Use simple tables.
* Use readable spacing.
* Use badges for statuses.
* Use filters above tables.
* Use action buttons clearly.
* Keep UI easy for ordinary warehouse staff.
* Avoid overly complex designs.
* Avoid dark backgrounds for main pages.
* Keep dashboards clean and professional.

## IMS Core Modules

The system includes these modules:

```text
Dashboard
Product/SKU Master
Supplier Master
Customer Master
Warehouse Layout
Inventory Overview
Stock Movement
Receiving
QC Check
Putaway
Transfers
Adjustments
Cycle Count
Purchase Orders
Sales Orders
Picking
Packing
Dispatch
Production Support
Bill of Materials
Material Issue
Finished Goods Receipt
Reports
Users and Roles
```

## User Roles

The system has three main users:

### Admin

Admin can manage:

* Users
* Roles
* Products
* Suppliers
* Customers
* Warehouses
* Orders
* Approvals
* Reports
* Settings

### Manager

Manager can:

* Monitor stock
* Approve purchase requests
* Approve adjustments
* Assign tasks
* Review reports
* Monitor warehouse operations

### Warehouse Staff

Warehouse Staff can:

* Receive goods
* Perform QC
* Putaway accepted stock
* Pick items
* Pack orders
* Dispatch orders
* Transfer stock
* Perform cycle count
* Request adjustments

## Warehouse Terminology

Use this warehouse structure:

```text
Warehouse
  → Floor
    → Room
      → Rack
        → Shelf
```

Do not use “bin” as the main storage term.

Use “Shelf” instead of “Bin”.

Warehouse visualization should show:

* Floor
* Room
* Rack
* Shelf
* Shelf capacity
* Current quantity
* Current weight
* Utilization percentage
* Raw material or finished goods
* QC hold shelf
* Suggested shelf

## Inventory Calculation Rule

Available stock should be calculated as:

```text
AvailableStock = CurrentStock - ReservedStock - QCHoldStock - DamagedStock
```

Do not calculate availability from current stock only.

## Important Business Rules

Follow these rules in all modules:

1. Stock should never become negative.
2. Reserved stock should not exceed current stock.
3. Staff should not approve their own adjustment request.
4. Usable stock should increase only after putaway if QC is enabled.
5. Receiving alone should not automatically increase usable stock when QC is required.
6. Sales dispatch should reduce stock and consume reservation.
7. Every stock change must create a StockMovements record.
8. Low stock should be calculated from available stock.
9. Shelf capacity must be checked before putaway or transfer.
10. Damaged and QC hold stock should not be available for sale or production.
11. Important operations should be written to AuditLogs.
12. Role-based access must protect Razor Pages.
13. Repeated stock calculations should use SQL functions or service methods.

## Stock Movement Types

Use these movement types:

```text
StockIn
StockOut
Reserve
Unreserve
TransferOut
TransferIn
AdjustmentIn
AdjustmentOut
ReturnIn
DamageOut
ProductionIssue
ProductionReturn
```

Stock should never be changed silently.

Every stock change must insert a StockMovements record.

## Purchasing Workflow

Use this workflow:

```text
Purchase Request
→ Purchase Order
→ Receiving
→ QC Check
→ Putaway
→ Stock Increased
```

Purchase Order statuses may include:

```text
Draft
Confirmed
InTransit
Received
QCCheck
Putaway
Closed
Cancelled
```

## Sales Workflow

Use this workflow:

```text
Sales Order
→ Stock Check
→ Reservation
→ Picking
→ Packing
→ Dispatch
→ Stock Decreased
```

Sales Order statuses may include:

```text
Draft
Confirmed
Allocated
ReadyToPick
Picking
Packed
Dispatched
Fulfilled
Cancelled
```

## Production Workflow

Use this workflow:

```text
Production Plan
→ BOM Calculation
→ Material Availability Check
→ Material Issue
→ Finished Goods Receipt
→ QC
→ Putaway
```

Production modules should include:

* Bill of Materials
* Production Plan
* Material Issue
* Material Return
* Finished Goods Receipt

## SQL Server Rules

Use SQL Server for persistent data.

Use tables for:

* Products
* Categories
* Units
* Suppliers
* Customers
* Warehouses
* Floors
* Rooms
* Racks
* Shelves
* InventoryStocks
* StockMovements
* StockReservations
* PurchaseOrders
* PurchaseOrderItems
* ReceivingItems
* QCChecks
* PutawayTasks
* SalesOrders
* SalesOrderItems
* PickTasks
* Packing
* Dispatches
* Transfers
* Adjustments
* CycleCounts
* BOM
* BOMItems
* ProductionPlans
* ProductionMaterialIssues
* FinishedGoodsReceipts
* AuditLogs

Use SQL functions for reusable calculations, such as:

* Available stock
* Shelf available capacity
* Stock status

Use stored procedures for stock-changing operations, such as:

* Record stock movement
* Confirm putaway
* Reserve stock
* Transfer stock
* Dispatch stock
* Adjust stock
* Issue production material
* Receive finished goods

Use SQL views for reports and dashboards.

## Stored Procedure Rule

When implementing stock operations, prefer stored procedures.

Do not directly update stock quantities from Razor Pages.

Correct flow:

```text
Razor Page
→ Service
→ Repository
→ Stored Procedure
→ StockMovements + InventoryStocks update
```

Incorrect flow:

```text
Razor Page
→ Directly update InventoryStocks only
```

## Stitch Export Rules

Stitch exports are stored inside:

```text
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative
```

There may be many Stitch export folders, for example:

```text
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/admin_dashboard_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/inventory_overview_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/purchase_order_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/sales_order_management_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/warehouse_visualization_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/picking_packing_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/dispatch_tracking_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/quality_control_check_localized
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/finished_goods_receipt
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/bill_of_materials_bom_management
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/material_issue_return
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/production_planning
```

Important:

* Convert only one Stitch folder at a time.
* Do not read and convert all Stitch folders together unless specifically requested.
* Do not mix CSS from different Stitch folders.
* Do not merge unrelated pages into one Razor Page.
* Do not create React components from Stitch export.
* Do not create Next.js pages from Stitch export.
* Do not install Tailwind just because Stitch export uses Tailwind-like classes.
* Convert Tailwind-style utility classes into normal CSS if Tailwind is not already configured.
* Keep each converted page independent and clean.

## Stitch-to-Razor Conversion Rules

When converting a Stitch export:

1. Read only the selected Stitch folder.
2. Identify its `index.html`, `style.css`, and `script.js` if present.
3. Convert `index.html` into a Razor Page `.cshtml`.
4. Convert repeated static HTML into `foreach` loops.
5. Move repeated data into the `.cshtml.cs` PageModel as mock data.
6. Move CSS into a page-specific file in `wwwroot/css`.
7. Move JavaScript into a page-specific file in `wwwroot/js` only if needed.
8. Keep the visual design close to the Stitch export.
9. Use existing project layout where possible.
10. Do not modify global layout unless required.
11. Make sure the page route works.
12. Make sure `dotnet build` succeeds.

## Stitch Conversion Prompt Pattern

When the user asks to convert a Stitch folder, follow this pattern:

```text
Read GEMINI.md first.

Convert only this Stitch export folder:
C:\IMS\IMS\IMS\UIDesign\stitch_untitled_development_initiative/[folder-name]

Target files:
- Pages/[Module]/[Page].cshtml
- Pages/[Module]/[Page].cshtml.cs
- wwwroot/css/[page-css].css
- wwwroot/js/[page-js].js

Rules:
- Use ASP.NET Core Razor Pages.
- Use .NET 10 LTS.
- Do not use React.
- Do not use Next.js.
- Do not use Tailwind.
- Do not touch other Stitch folders.
- Do not modify Program.cs.
- Do not modify _Layout.cshtml unless absolutely required.
- Use mock data in the PageModel.
- Use foreach loops for repeated content.
- Keep the design visually close to Stitch.
- Make sure dotnet build succeeds.
```

## Recommended Stitch Folder Mapping

Use this mapping when converting pages:

```text
admin_dashboard_localized
→ Pages/Dashboard/Admin.cshtml
→ Pages/Dashboard/Admin.cshtml.cs
→ wwwroot/css/admin-dashboard.css
→ wwwroot/js/admin-dashboard.js

inventory_overview_localized
→ Pages/Inventory/Overview.cshtml
→ Pages/Inventory/Overview.cshtml.cs
→ wwwroot/css/inventory-overview.css
→ wwwroot/js/inventory-overview.js

purchase_order_localized
→ Pages/Purchasing/PurchaseOrders.cshtml
→ Pages/Purchasing/PurchaseOrders.cshtml.cs
→ wwwroot/css/purchase-orders.css
→ wwwroot/js/purchase-orders.js

sales_order_management_localized
→ Pages/Sales/Orders.cshtml
→ Pages/Sales/Orders.cshtml.cs
→ wwwroot/css/sales-orders.css
→ wwwroot/js/sales-orders.js

warehouse_visualization_localized
→ Pages/Warehouses/Visualization.cshtml
→ Pages/Warehouses/Visualization.cshtml.cs
→ wwwroot/css/warehouse-visualization.css
→ wwwroot/js/warehouse-visualization.js

picking_packing_localized
→ Pages/WarehouseOperations/PickingPacking.cshtml
→ Pages/WarehouseOperations/PickingPacking.cshtml.cs
→ wwwroot/css/picking-packing.css
→ wwwroot/js/picking-packing.js

dispatch_tracking_localized
→ Pages/WarehouseOperations/DispatchTracking.cshtml
→ Pages/WarehouseOperations/DispatchTracking.cshtml.cs
→ wwwroot/css/dispatch-tracking.css
→ wwwroot/js/dispatch-tracking.js

quality_control_check_localized
→ Pages/WarehouseOperations/QCCheck.cshtml
→ Pages/WarehouseOperations/QCCheck.cshtml.cs
→ wwwroot/css/qc-check.css
→ wwwroot/js/qc-check.js

finished_goods_receipt
→ Pages/Production/FinishedGoodsReceipt.cshtml
→ Pages/Production/FinishedGoodsReceipt.cshtml.cs
→ wwwroot/css/finished-goods-receipt.css
→ wwwroot/js/finished-goods-receipt.js

bill_of_materials_bom_management
→ Pages/Production/BOM.cshtml
→ Pages/Production/BOM.cshtml.cs
→ wwwroot/css/bom.css
→ wwwroot/js/bom.js

material_issue_return
→ Pages/Production/MaterialIssueReturn.cshtml
→ Pages/Production/MaterialIssueReturn.cshtml.cs
→ wwwroot/css/material-issue-return.css
→ wwwroot/js/material-issue-return.js

production_planning
→ Pages/Production/Planning.cshtml
→ Pages/Production/Planning.cshtml.cs
→ wwwroot/css/production-planning.css
→ wwwroot/js/production-planning.js
```

## Mock Data Rule

Use mock data first unless the user provides database connection details.

Mock data should be placed in the PageModel.

Do not hard-code repeated rows directly in `.cshtml`.

Use small model classes inside `.cshtml.cs` when needed.

Example:

```csharp
public class InventoryRow
{
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal CurrentQty { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal QCHoldQty { get; set; }
    public decimal DamagedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public string Status { get; set; } = string.Empty;
}
```

## Build and Error Fixing Rules

After generating or modifying files:

1. Check Razor syntax.
2. Check namespace.
3. Check file paths.
4. Check CSS link paths.
5. Check JavaScript link paths.
6. Check that model names match.
7. Run or recommend:

```bash
dotnet build
```

If there is a build error, fix the cause directly.

Common issues to avoid:

* Wrong namespace
* Missing PageModel class
* Wrong folder route
* Invalid Razor syntax
* Using `@` inside JavaScript incorrectly
* Missing closing tags
* Broken Tag Helpers
* CSS file not linked
* JS file path wrong
* Duplicate class names
* Using React syntax in Razor Pages

## Navigation Rules

Do not randomly change navigation.

When adding a new page, use a logical route based on the module.

Examples:

```text
/Dashboard/Admin
/Inventory/Overview
/Purchasing/PurchaseOrders
/Sales/Orders
/Warehouses/Visualization
/WarehouseOperations/PickingPacking
/WarehouseOperations/QCCheck
/Production/Planning
/Production/BOM
```

Only update navigation if the user asks.

## Report and Dashboard Rules

Dashboards should use SQL views or mock data first.

Good dashboard cards:

* Total stock value
* Low stock items
* Pending purchase orders
* Daily shipments
* QC pending items
* Available stock
* Reserved stock
* Warehouse utilization

Reports should include:

* Current stock report
* Low stock report
* Out of stock report
* Stock movement report
* Purchase report
* Sales report
* Supplier performance report
* Warehouse utilization report
* Shelf capacity report
* QC report
* Adjustment report
* Cycle count variance report
* Inventory valuation report

## Development Roadmap

Build in this order:

1. Login and role-based layout
2. Product master
3. Warehouse and shelf setup
4. Inventory overview
5. Stock movement system
6. Purchase order
7. Receiving
8. QC check
9. Putaway
10. Sales order
11. Stock reservation
12. Picking
13. Packing
14. Dispatch
15. Adjustment
16. Cycle count
17. Reports
18. Production material issue
19. Finished goods receipt
20. Dashboard polish

## Final Instruction

Always prioritize:

1. Clean Razor Pages structure
2. Working build
3. Simple maintainable UI
4. Correct IMS business rules
5. Proper separation of UI, service, repository, and SQL logic
6. Safe stock movement handling
7. Clear page-specific CSS and JavaScript
8. One Stitch export folder conversion at a time

## Testing Skills Gemini Must Use

When asked to test or fix the application, act with these skills:

### 1. Full-stack QA Tester

* Test the application from the browser/user point of view.
* Do not rely only on code reading.
* Click navbar links, header links, sidebar links, action buttons, modals, filters, search boxes, submit buttons, cancel buttons, and export buttons.
* Verify success/error messages.
* Verify pages do not show offline/mock data when database connection is available.

### 2. ASP.NET Core Razor Pages Specialist

* Check `asp-page`, `asp-page-handler`, form `method`, hidden fields, `[BindProperty]`, handler method names, and modal forms.
* Ensure every button has the correct behavior:

  * submit buttons submit forms
  * modal buttons only open modals
  * action buttons pass correct IDs
* Verify handlers reload dropdowns and page data after validation failure.
* Verify TempData messages and PRG pattern work correctly.

### 3. SQL Server Schema Auditor

* Use the actual `FactoryIMS` database schema as the source of truth.
* Check `INFORMATION_SCHEMA.COLUMNS`, stored procedure parameters, views, and table names.
* Fix invalid column errors such as `Invalid column name 'TaskNo'`.
* Do not guess column names.
* Prefer fixing C# SQL queries with correct column names or aliases.
* Do not rename database columns unless absolutely necessary.

### 4. Business Process Lifecycle Tester

Test complete process lifecycles, not isolated pages only:

Purchase/Inward:
Purchase Order → Confirm PO → Receive Goods → QC Check → Putaway → Inventory Overview → Stock Movements

Sales/Outbound:
Sales Order → Confirm → Allocate/Pick Task → Start Picking → Mark Picked → Complete Packing → Create Dispatch → Confirm Dispatch → Delivered → Inventory Overview → Stock Movements

Manufacturing:
Production Plan → Approve → Start → Material Issue Request → Confirm Issue → Material Return if needed → Finished Goods Receipt → QC/Putaway if supported → Inventory Overview → Stock Movements

Inventory Control:
Adjustment Request → Approve → Post → Inventory Overview → Stock Movements

Transfer:
Transfer Request → Approve → Post → Source Shelf/Destination Shelf stock check → Stock Movements

### 5. Mock Data Detector

* Search for hardcoded/mock/sample/offline/static data in:

  * `.cshtml`
  * `.cshtml.cs`
  * repositories
  * services
  * wwwroot/js
* Look for terms:

  * mock
  * sample
  * demo
  * offline
  * fallback
  * static list
  * hardcoded
  * example
* Replace mock data with database-driven data where the module is supposed to be live.
* If offline mode exists, show it only when database connection fails, not during normal operation.
* Specifically inspect pages like `/Production/FinishedGoodsReceipt` for mock rows or hardcoded examples.

### 6. Filter/Search Tester

* Test every filter and search box.
* Verify query string filters work.
* Verify filters are passed to repository SQL.
* Verify reset buttons clear filters.
* Verify filtered results match database data.
* Fix filters that only change UI but do not affect database results.

### 7. RBAC and Security Tester

* Test as Admin, Manager, and Staff.
* Verify navbar visibility by role.
* Verify direct URL access is blocked.
* Verify unauthorized POST actions are blocked server-side.
* Verify logged-in user ID is used instead of hardcoded user ID.

### 8. Minimal Fix Discipline

* Fix bugs with the smallest safe change.
* Do not redesign UI.
* Do not add new features during testing.
* Do not change business rules.
* Do not bypass stored procedures for stock-changing operations.
* Keep `dotnet build` successful.
