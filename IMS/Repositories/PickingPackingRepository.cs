using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class PickingPackingRepository : IPickingPackingRepository
    {
        private readonly string _connectionString;

        public PickingPackingRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<PickingPackingDto>> GetPickPackTasksAsync()
        {
            var results = new List<PickingPackingDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT pt.PickID, pt.SOID, pt.Status AS PickStatus, pt.CreatedAt AS PickCreatedAt, pt.CompletedDate AS PickCompletedDate,
                           so.SONumber, c.CustomerName,
                           u_pick.FullName AS AssignedToName,
                           pk.PackID, pk.PackedBy, u_pack.FullName AS PackedByName, pk.ContainerType, pk.Status AS PackStatus, pk.CreatedAt AS PackCreatedAt,
                           items.ItemCount, items.TotalOrderedQty, items.TotalDispatchedQty
                    FROM PickTasks pt
                    INNER JOIN SalesOrders so ON pt.SOID = so.SOID
                    INNER JOIN Customers c ON so.CustomerID = c.CustomerID
                    LEFT JOIN Users u_pick ON pt.AssignedTo = u_pick.UserID
                    LEFT JOIN Packing pk ON so.SOID = pk.SOID
                    LEFT JOIN Users u_pack ON pk.PackedBy = u_pack.UserID
                    LEFT JOIN (
                        SELECT SOID, 
                               COUNT(*) AS ItemCount, 
                               SUM(OrderedQty) AS TotalOrderedQty, 
                               SUM(DispatchedQty) AS TotalDispatchedQty
                        FROM SalesOrderItems
                        GROUP BY SOID
                    ) items ON so.SOID = items.SOID
                    ORDER BY pt.CreatedAt DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new PickingPackingDto
                    {
                        PickID = reader.GetInt32("PickID"),
                        SOID = reader.GetInt32("SOID"),
                        PickStatus = reader.GetString("PickStatus"),
                        PickCreatedAt = reader.GetDateTime("PickCreatedAt"),
                        PickCompletedDate = reader.IsDBNull("PickCompletedDate") ? (DateTime?)null : reader.GetDateTime("PickCompletedDate"),
                        SONumber = reader.GetString("SONumber"),
                        CustomerName = reader.GetString("CustomerName"),
                        AssignedToName = reader.IsDBNull("AssignedToName") ? "" : reader.GetString("AssignedToName"),
                        PackID = reader.IsDBNull("PackID") ? (int?)null : reader.GetInt32("PackID"),
                        PackedByName = reader.IsDBNull("PackedByName") ? "" : reader.GetString("PackedByName"),
                        ContainerType = reader.IsDBNull("ContainerType") ? "" : reader.GetString("ContainerType"),
                        PackStatus = reader.IsDBNull("PackStatus") ? "" : reader.GetString("PackStatus"),
                        PackCreatedAt = reader.IsDBNull("PackCreatedAt") ? (DateTime?)null : reader.GetDateTime("PackCreatedAt"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalOrderedQty = reader.IsDBNull("TotalOrderedQty") ? 0 : reader.GetDecimal("TotalOrderedQty"),
                        TotalDispatchedQty = reader.IsDBNull("TotalDispatchedQty") ? 0 : reader.GetDecimal("TotalDispatchedQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching pick/pack tasks.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<PickingPackingItemDto>> GetTaskItemsAsync(int soid)
        {
            var results = new List<PickingPackingItemDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT soi.SOItemID, soi.SOID, soi.ProductID, p.SKU, p.ProductName, 
                           soi.OrderedQty, soi.DispatchedQty,
                           sh.ShelfID, sh.ShelfCode, w.WarehouseName, f.FloorName, rm.RoomName, rk.RackName,
                           ISNULL(sr.ReservedQty, 0) AS ReservedQty,
                           dbo.fn_GetAvailableStock(soi.ProductID, sh.ShelfID) AS AvailableQty
                    FROM SalesOrderItems soi
                    INNER JOIN Products p ON soi.ProductID = p.ProductID
                    -- Get location from reservations (since picking is from reserved stock)
                    LEFT JOIN StockReservations sr ON soi.ProductID = sr.ProductID 
                        AND sr.ReferenceType = 'SalesOrder' 
                        AND sr.ReferenceNo = (SELECT SONumber FROM SalesOrders WHERE SOID = soi.SOID)
                        AND sr.Status = 'Active'
                    LEFT JOIN Shelves sh ON sr.ShelfID = sh.ShelfID
                    LEFT JOIN Racks rk ON sh.RackID = rk.RackID
                    LEFT JOIN Rooms rm ON rk.RoomID = rm.RoomID
                    LEFT JOIN Floors f ON rm.FloorID = f.FloorID
                    LEFT JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                    WHERE soi.SOID = @SOID", connection);

                command.Parameters.AddWithValue("@SOID", soid);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new PickingPackingItemDto
                    {
                        SOItemID = reader.GetInt32("SOItemID"),
                        SOID = reader.GetInt32("SOID"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        OrderedQty = reader.GetDecimal("OrderedQty"),
                        DispatchedQty = reader.GetDecimal("DispatchedQty"),
                        ReservedQty = reader.GetDecimal("ReservedQty"),
                        AvailableQty = reader.GetDecimal("AvailableQty"),
                        ShelfID = reader.IsDBNull("ShelfID") ? (int?)null : reader.GetInt32("ShelfID"),
                        ShelfCode = reader.IsDBNull("ShelfCode") ? "" : reader.GetString("ShelfCode"),
                        WarehouseName = reader.IsDBNull("WarehouseName") ? "" : reader.GetString("WarehouseName"),
                        FloorName = reader.IsDBNull("FloorName") ? "" : reader.GetString("FloorName"),
                        RoomName = reader.IsDBNull("RoomName") ? "" : reader.GetString("RoomName"),
                        RackName = reader.IsDBNull("RackName") ? "" : reader.GetString("RackName")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching task items.", ex);
            }

            return results;
        }

        public async Task<PickingPackingActionResultDto> CreatePickTaskAsync(CreatePickTaskDto task)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Reserve stock for each item using stored procedure
                foreach (var item in task.Items)
                {
                    using var cmdReserve = new SqlCommand("dbo.sp_ReserveStock", connection, transaction);
                    cmdReserve.CommandType = CommandType.StoredProcedure;
                    cmdReserve.Parameters.AddWithValue("@SOID", task.SOID);
                    cmdReserve.Parameters.AddWithValue("@ProductID", item.ProductID);
                    cmdReserve.Parameters.AddWithValue("@ShelfID", item.ShelfID);
                    cmdReserve.Parameters.AddWithValue("@QtyToReserve", item.QtyToReserve);
                    cmdReserve.Parameters.AddWithValue("@PerformedBy", task.AssignedTo);
                    await cmdReserve.ExecuteNonQueryAsync();
                }

                // 2. Create Pick Task
                using var cmdPick = new SqlCommand(@"
                    INSERT INTO PickTasks (SOID, AssignedTo, Status, CreatedAt)
                    VALUES (@SOID, @AssignedTo, 'Pending', GETDATE());
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                cmdPick.Parameters.AddWithValue("@SOID", task.SOID);
                cmdPick.Parameters.AddWithValue("@AssignedTo", task.AssignedTo);
                var pickId = Convert.ToInt32(await cmdPick.ExecuteScalarAsync());

                // 3. Update Sales Order Status
                using var cmdSO = new SqlCommand(@"
                    UPDATE SalesOrders SET Status = 'Allocated'
                    WHERE SOID = @SOID", connection, transaction);
                cmdSO.Parameters.AddWithValue("@SOID", task.SOID);
                await cmdSO.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return new PickingPackingActionResultDto { Success = true, Message = "Pick task created and stock reserved successfully.", NewID = pickId };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PickingPackingActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PickingPackingActionResultDto> StartPickingAsync(int pickId, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmdPick = new SqlCommand(@"
                    UPDATE PickTasks SET Status = 'Picking' WHERE PickID = @PickID;
                    SELECT SOID FROM PickTasks WHERE PickID = @PickID;", connection, transaction);
                cmdPick.Parameters.AddWithValue("@PickID", pickId);
                var soid = (int?)await cmdPick.ExecuteScalarAsync();

                if (soid.HasValue)
                {
                    using var cmdSO = new SqlCommand("UPDATE SalesOrders SET Status = 'Picking' WHERE SOID = @SOID", connection, transaction);
                    cmdSO.Parameters.AddWithValue("@SOID", soid.Value);
                    await cmdSO.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return new PickingPackingActionResultDto { Success = true, Message = "Picking started." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PickingPackingActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PickingPackingActionResultDto> MarkPickedAsync(int pickId, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmdPick = new SqlCommand(@"
                    UPDATE PickTasks SET Status = 'Picked', CompletedDate = GETDATE() WHERE PickID = @PickID;
                    SELECT SOID FROM PickTasks WHERE PickID = @PickID;", connection, transaction);
                cmdPick.Parameters.AddWithValue("@PickID", pickId);
                var soid = (int?)await cmdPick.ExecuteScalarAsync();

                if (soid.HasValue)
                {
                    using var cmdSO = new SqlCommand("UPDATE SalesOrders SET Status = 'Picked' WHERE SOID = @SOID", connection, transaction);
                    cmdSO.Parameters.AddWithValue("@SOID", soid.Value);
                    await cmdSO.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return new PickingPackingActionResultDto { Success = true, Message = "Items marked as Picked." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PickingPackingActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<PickingPackingActionResultDto> CompletePackingAsync(PackingCreateDto packing)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmdPack = new SqlCommand(@"
                    IF EXISTS (SELECT 1 FROM Packing WHERE SOID = @SOID)
                        UPDATE Packing SET PackedBy = @PackedBy, ContainerType = @ContainerType, Status = 'Packed' WHERE SOID = @SOID
                    ELSE
                        INSERT INTO Packing (SOID, PackedBy, ContainerType, Status, CreatedAt)
                        VALUES (@SOID, @PackedBy, @ContainerType, 'Packed', GETDATE());", connection, transaction);

                cmdPack.Parameters.AddWithValue("@SOID", packing.SOID);
                cmdPack.Parameters.AddWithValue("@PackedBy", packing.PackedBy);
                cmdPack.Parameters.AddWithValue("@ContainerType", packing.ContainerType);
                await cmdPack.ExecuteNonQueryAsync();

                using var cmdSO = new SqlCommand("UPDATE SalesOrders SET Status = 'Packed' WHERE SOID = @SOID", connection, transaction);
                cmdSO.Parameters.AddWithValue("@SOID", packing.SOID);
                await cmdSO.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return new PickingPackingActionResultDto { Success = true, Message = "Order packed successfully." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PickingPackingActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<SalesOrderForPickingDto?> GetSalesOrderForPickingAsync(int soId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmdHeader = new SqlCommand(@"
                SELECT so.SOID, so.SONumber, c.CustomerName, so.Status
                FROM SalesOrders so
                INNER JOIN Customers c ON so.CustomerID = c.CustomerID
                WHERE so.SOID = @SOID", connection);
            cmdHeader.Parameters.AddWithValue("@SOID", soId);

            using var reader = await cmdHeader.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var result = new SalesOrderForPickingDto
                {
                    SOID = reader.GetInt32("SOID"),
                    SONumber = reader.GetString("SONumber"),
                    CustomerName = reader.GetString("CustomerName"),
                    Status = reader.GetString("Status")
                };
                reader.Close();

                result.Items = (await GetTaskItemsAsync(soId)).ToList();
                return result;
            }
            return null;
        }

        public async Task<decimal> GetAvailableStockAsync(int productId, int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT dbo.fn_GetAvailableStock(@ProductID, @ShelfID)", connection);
            command.Parameters.AddWithValue("@ProductID", productId);
            command.Parameters.AddWithValue("@ShelfID", shelfId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }

        public async Task<IEnumerable<dynamic>> GetConfirmedSalesOrdersAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT SOID, SONumber, TotalAmount FROM SalesOrders WHERE Status = 'Confirmed' ORDER BY OrderDate", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Number = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetActiveWarehouseStaffAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT UserID, FullName FROM Users WHERE IsActive = 1 ORDER BY FullName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetActiveShelvesAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT s.ShelfID, w.WarehouseName + ' -> ' + f.FloorName + ' -> ' + rm.RoomName + ' -> ' + rk.RackName + ' -> ' + s.ShelfCode AS FullPath
                FROM Shelves s
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                WHERE s.IsActive = 1
                    AND s.ShelfType <> 'QC Hold'
                ORDER BY w.WarehouseName, s.ShelfCode", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetShelvesWithProductAsync(int productId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT s.ShelfID,
                       w.WarehouseName + ' -> ' + f.FloorName + ' -> ' + rm.RoomName + ' -> ' + rk.RackName + ' -> ' + s.ShelfCode
                           + ' (Available: ' + CAST(dbo.fn_GetAvailableStock(@ProductID, s.ShelfID) AS NVARCHAR(50)) + ')' AS FullPath
                FROM InventoryStocks ist
                INNER JOIN Shelves s ON ist.ShelfID = s.ShelfID
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                WHERE s.IsActive = 1
                    AND s.ShelfType <> 'QC Hold'
                    AND ist.ProductID = @ProductID
                    AND dbo.fn_GetAvailableStock(@ProductID, s.ShelfID) > 0
                ORDER BY w.WarehouseName, s.ShelfCode", connection);
            command.Parameters.AddWithValue("@ProductID", productId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            return results;
        }
    }
}
