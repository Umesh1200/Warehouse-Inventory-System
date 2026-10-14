var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddRazorPages(options =>
//{
//    options.Conventions.AuthorizeFolder("/");
//    options.Conventions.AllowAnonymousToPage("/Account/Login");

//    // Role-based folder and page access
//    options.Conventions.AuthorizeFolder("/Users", "AdminOnly");
//    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
//    options.Conventions.AuthorizePage("/Warehouses/Setup", "AdminOnly");
//    options.Conventions.AuthorizeFolder("/Reports", "ManagerOrAdmin");
//    options.Conventions.AuthorizeFolder("/Purchasing", "ManagerOrAdmin");
//    options.Conventions.AuthorizeFolder("/Sales", "ManagerOrAdmin");
//    options.Conventions.AuthorizePage("/Production/Planning", "ManagerOrAdmin");
//    options.Conventions.AuthorizePage("/Production/BOM", "ManagerOrAdmin");
//    options.Conventions.AuthorizeFolder("/Dashboard", "ManagerOrAdmin");
//    options.Conventions.AuthorizeFolder("/SupplierPortal", "SupplierAccess");

//    // Internal Staff Pages/Folders (excluding Supplier)
//    options.Conventions.AuthorizeFolder("/WarehouseOperations", "InternalStaffOnly");
//    options.Conventions.AuthorizeFolder("/Production", "InternalStaffOnly");
//    options.Conventions.AuthorizeFolder("/Inventory", "InternalStaffOnly");
//    options.Conventions.AuthorizeFolder("/Warehouses", "InternalStaffOnly");
//});

builder.Services.AddRazorPages(options =>
{
    // Protect every page by default
    options.Conventions.AuthorizeFolder("/");

    // Public pages
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Error");

    // Role-based folder and page access
    options.Conventions.AuthorizeFolder("/Users", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizePage("/Warehouses/Setup", "AdminOnly");

    options.Conventions.AuthorizeFolder("/Reports", "ManagerOrAdmin");
    options.Conventions.AuthorizeFolder("/Purchasing", "ManagerOrAdmin");
    options.Conventions.AuthorizeFolder("/Accounts", "ManagerOrAdmin");
    options.Conventions.AuthorizeFolder("/Sales", "ManagerOrAdmin");
    options.Conventions.AuthorizePage(
        "/Production/Planning",
        "ManagerOrAdmin");
    options.Conventions.AuthorizePage(
        "/Production/BOM",
        "ManagerOrAdmin");
    options.Conventions.AuthorizeFolder(
        "/Dashboard",
        "InternalStaffOnly");
    options.Conventions.AuthorizePage(
        "/Dashboard/Admin",
        "ManagerOrAdmin");
    options.Conventions.AuthorizeFolder(
        "/SupplierPortal",
        "SupplierAccess");

    // Internal staff pages
    options.Conventions.AuthorizeFolder(
        "/WarehouseOperations",
        "InternalStaffOnly");
    options.Conventions.AuthorizeFolder(
        "/Production",
        "InternalStaffOnly");
    options.Conventions.AuthorizeFolder(
        "/Inventory",
        "InternalStaffOnly");
    options.Conventions.AuthorizeFolder(
        "/Warehouses",
        "InternalStaffOnly");
});

builder.Services.AddAuthentication("IMS_Auth")
    .AddCookie("IMS_Auth", options =>
    {
        options.Cookie.Name = "IMS_Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Warehouse Staff", "Admin"));
    options.AddPolicy("ProcurementAccess", policy => policy.RequireRole("Admin", "Manager", "Supplier"));
    options.AddPolicy("InternalStaffOnly", policy => policy.RequireRole("Admin", "Manager", "Warehouse Staff"));
    options.AddPolicy("SupplierAccess", policy => policy.RequireRole("Supplier", "Admin", "Manager"));
});


builder.Services.AddScoped<IMS.Repositories.IProductRepository, IMS.Repositories.ProductRepository>();
builder.Services.AddScoped<IMS.Services.IProductService, IMS.Services.ProductService>();
builder.Services.AddScoped<IMS.Repositories.IInventoryRepository, IMS.Repositories.InventoryRepository>();
builder.Services.AddScoped<IMS.Services.IInventoryService, IMS.Services.InventoryService>();
builder.Services.AddScoped<IMS.Repositories.IDashboardRepository, IMS.Repositories.DashboardRepository>();
builder.Services.AddScoped<IMS.Services.IDashboardService, IMS.Services.DashboardService>();
builder.Services.AddScoped<IMS.Repositories.ISupplierRepository, IMS.Repositories.SupplierRepository>();
builder.Services.AddScoped<IMS.Services.ISupplierService, IMS.Services.SupplierService>();
builder.Services.AddScoped<IMS.Repositories.ICustomerRepository, IMS.Repositories.CustomerRepository>();
builder.Services.AddScoped<IMS.Services.ICustomerService, IMS.Services.CustomerService>();
builder.Services.AddScoped<IMS.Repositories.IWarehouseRepository, IMS.Repositories.WarehouseRepository>();
builder.Services.AddScoped<IMS.Services.IWarehouseService, IMS.Services.WarehouseService>();
builder.Services.AddScoped<IMS.Repositories.IPurchaseOrderRepository, IMS.Repositories.PurchaseOrderRepository>();
builder.Services.AddScoped<IMS.Services.IPurchaseOrderService, IMS.Services.PurchaseOrderService>();
builder.Services.AddScoped<IMS.Repositories.IProcurementWorkflowRepository, IMS.Repositories.ProcurementWorkflowRepository>();
builder.Services.AddScoped<IMS.Services.IProcurementWorkflowService, IMS.Services.ProcurementWorkflowService>();
builder.Services.AddScoped<IMS.Repositories.ISalesOrderRepository, IMS.Repositories.SalesOrderRepository>();
builder.Services.AddScoped<IMS.Services.ISalesOrderService, IMS.Services.SalesOrderService>();
builder.Services.AddScoped<IMS.Repositories.IReceivingRepository, IMS.Repositories.ReceivingRepository>();
builder.Services.AddScoped<IMS.Services.IReceivingService, IMS.Services.ReceivingService>();
builder.Services.AddScoped<IMS.Repositories.IQCRepository, IMS.Repositories.QCRepository>();
builder.Services.AddScoped<IMS.Services.IQCService, IMS.Services.QCService>();
builder.Services.AddScoped<IMS.Repositories.IPutawayRepository, IMS.Repositories.PutawayRepository>();
builder.Services.AddScoped<IMS.Services.IPutawayService, IMS.Services.PutawayService>();
builder.Services.AddScoped<IMS.Repositories.IStockMovementRepository, IMS.Repositories.StockMovementRepository>();
builder.Services.AddScoped<IMS.Services.IStockMovementService, IMS.Services.StockMovementService>();
builder.Services.AddScoped<IMS.Repositories.IStockTransferRepository, IMS.Repositories.StockTransferRepository>();
builder.Services.AddScoped<IMS.Services.IStockTransferService, IMS.Services.StockTransferService>();
builder.Services.AddScoped<IMS.Repositories.IInventoryAdjustmentRepository, IMS.Repositories.InventoryAdjustmentRepository>();
builder.Services.AddScoped<IMS.Services.IInventoryAdjustmentService, IMS.Services.InventoryAdjustmentService>();
builder.Services.AddScoped<IMS.Repositories.ICycleCountRepository, IMS.Repositories.CycleCountRepository>();
builder.Services.AddScoped<IMS.Services.ICycleCountService, IMS.Services.CycleCountService>();
builder.Services.AddScoped<IMS.Repositories.IPickingPackingRepository, IMS.Repositories.PickingPackingRepository>();
builder.Services.AddScoped<IMS.Services.IPickingPackingService, IMS.Services.PickingPackingService>();
builder.Services.AddScoped<IMS.Repositories.IDispatchRepository, IMS.Repositories.DispatchRepository>();
builder.Services.AddScoped<IMS.Services.IDispatchService, IMS.Services.DispatchService>();
builder.Services.AddScoped<IMS.Repositories.IMaterialIssueReturnRepository, IMS.Repositories.MaterialIssueReturnRepository>();
builder.Services.AddScoped<IMS.Services.IMaterialIssueReturnService, IMS.Services.MaterialIssueReturnService>();
builder.Services.AddScoped<IMS.Repositories.IBomRepository, IMS.Repositories.BomRepository>();
builder.Services.AddScoped<IMS.Services.IBomService, IMS.Services.BomService>();
builder.Services.AddScoped<IMS.Repositories.IProductionPlanningRepository, IMS.Repositories.ProductionPlanningRepository>();
builder.Services.AddScoped<IMS.Services.IProductionPlanningService, IMS.Services.ProductionPlanningService>();
builder.Services.AddScoped<IMS.Repositories.IFinishedGoodsReceiptRepository, IMS.Repositories.FinishedGoodsReceiptRepository>();
builder.Services.AddScoped<IMS.Services.IFinishedGoodsReceiptService, IMS.Services.FinishedGoodsReceiptService>();
builder.Services.AddScoped<IMS.Repositories.IUserRoleRepository, IMS.Repositories.UserRoleRepository>();
builder.Services.AddScoped<IMS.Services.IUserRoleService, IMS.Services.UserRoleService>();
builder.Services.AddScoped<IMS.Repositories.IAuditLogRepository, IMS.Repositories.AuditLogRepository>();
builder.Services.AddScoped<IMS.Services.IAuditLogService, IMS.Services.AuditLogService>();
builder.Services.AddScoped<IMS.Repositories.ISystemSettingRepository, IMS.Repositories.SystemSettingRepository>();
builder.Services.AddScoped<IMS.Services.ISystemSettingService, IMS.Services.SystemSettingService>();
builder.Services.AddScoped<IMS.Repositories.IReportRepository, IMS.Repositories.ReportRepository>();
builder.Services.AddScoped<IMS.Services.IReportService, IMS.Services.ReportService>();
builder.Services.AddScoped<IMS.Repositories.ISupplierReturnRepository, IMS.Repositories.SupplierReturnRepository>();
builder.Services.AddScoped<IMS.Services.ISupplierReturnService, IMS.Services.SupplierReturnService>();
builder.Services.AddScoped<IMS.Repositories.IProductStorageRuleRepository, IMS.Repositories.ProductStorageRuleRepository>();
builder.Services.AddScoped<IMS.Services.IProductStorageRuleService, IMS.Services.ProductStorageRuleService>();


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMS.Services.IUserContext, IMS.Services.UserContext>();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine();
    Console.WriteLine("==================================================");
    Console.WriteLine("  IMS Application Started!");
    foreach (var url in app.Urls)
    {
        Console.WriteLine($"  Access URL: {url}");
    }
    Console.WriteLine("==================================================");
    Console.WriteLine();
});

app.Run();
