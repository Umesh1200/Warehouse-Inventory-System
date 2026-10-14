# Inventory Management System (IMS) - Enterprise Logistics

A robust, enterprise-grade Inventory Management System (IMS) built with ASP.NET Core Razor Pages and SQL Server. This system manages the complete inventory lifecycle, from procurement and warehouse operations to production and sales.

## Technology Stack

- **Framework**: ASP.NET Core 10 LTS (Razor Pages)
- **Database**: Microsoft SQL Server
- **Data Access**: ADO.NET with `Microsoft.Data.SqlClient` (Pure SQL, no EF)
- **Architecture**: Layered Architecture (Pages -> Services -> Repositories -> SQL Server)
- **UI**: HTML5, CSS3 (Vanilla), JavaScript, Bootstrap-compatible layout

## Key Features & Modules

- **Security & RBAC**: Role-based access control for Admin, Manager, and Warehouse Staff.
- **Master Data**: Product (SKU), Supplier, Customer, and Warehouse Management.
- **Warehouse Layout**: Multi-level hierarchy (Warehouse → Floor → Room → Rack → Shelf).
- **Inventory Control**: Real-time overview, Stock Movements, Adjustments, and Transfers.
- **Purchasing Workflow**: PO → Receiving → QC → Putaway.
- **Sales Workflow**: Sales Order → Allocation → Picking → Packing → Dispatch.
- **Manufacturing Support**: Bill of Materials (BOM), Production Planning, Material Issue/Return, Finished Goods Receipt.
- **Reporting**: Advanced Audit Trails with filtering and CSV export.

## Business Rules Highlights

1.  **Stock Persistence**: Stock only increases after the **Putaway** stage (not at receiving or QC).
2.  **Order Integrity**: Stock only decreases during **Sales Dispatch** or **Production Issue**.
3.  **Reservations**: Sales orders increase `ReservedQty` upon confirmation, preventing over-selling.
4.  **No Negative Stock**: Business rules strictly forbid negative inventory levels.
5.  **Audit Integrity**: Every stock change generates a `StockMovements` record for full accountability.

## How to Run

1.  **Database Setup**: 
    - Execute the SQL scripts in `Database/` in the specified order (00 to 05).
    - Or follow the instructions in `Database/README.md`.
2.  **Configuration**: 
    - Update the `DefaultConnection` string in `appsettings.json` to point to your SQL Server instance.
3.  **Build & Run**:
    ```bash
    dotnet build
    dotnet run
    ```

## Demo Credentials

| Role | Username | Password |
| :--- | :--- | :--- |
| **Admin** | `admin` | `IMS@123` |
| **Warehouse Staff** | `deepak` | `IMS@123` |
| **Manager** | `arjun` | `IMS@123` |

*Note: Default password is for demo/demonstration purposes only.*

## Documentation

- [Screenshot Checklist](./Documentation/Screenshot_Checklist.md)
- [Project Blueprint](./Inventory_Management_System_Blueprint_Umesh_Chaudhary.md)
