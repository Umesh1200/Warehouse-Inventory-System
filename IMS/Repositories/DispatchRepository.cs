using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class DispatchRepository : IDispatchRepository
    {
        private readonly string _connectionString;

        public DispatchRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<DispatchTrackingDto>> GetDispatchTasksAsync()
        {
            var results = new List<DispatchTrackingDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                // Union query:
                // 1. Packed orders without dispatch records (Ready to Dispatch)
                // 2. Existing dispatch records
                using var command = new SqlCommand(@"
                    -- Part 1: Packed but not yet dispatched
                    SELECT 
                        NULL AS DispatchID,
                        so.SOID,
                        so.SONumber,
                        c.CustomerName,
                        pk.PackID,
                        '' AS CourierName,
                        '' AS VehicleNumber,
                        '' AS TrackingNumber,
                        NULL AS DispatchedBy,
                        '' AS DispatchedByName,
                        NULL AS DispatchDate,
                        'Ready To Dispatch' AS Status,
                        so.OrderDate,
                        so.RequiredDate,
                        so.TotalAmount,
                        items.ItemCount,
                        items.TotalOrderedQty,
                        items.TotalDispatchedQty
                    FROM SalesOrders so
                    INNER JOIN Customers c ON so.CustomerID = c.CustomerID
                    INNER JOIN Packing pk ON so.SOID = pk.SOID
                    LEFT JOIN (
                        SELECT SOID, 
                               COUNT(*) AS ItemCount, 
                               SUM(OrderedQty) AS TotalOrderedQty, 
                               SUM(DispatchedQty) AS TotalDispatchedQty
                        FROM SalesOrderItems
                        GROUP BY SOID
                    ) items ON so.SOID = items.SOID
                    WHERE pk.Status = 'Packed'
                    AND so.SOID NOT IN (SELECT SOID FROM Dispatches)

                    UNION ALL

                    -- Part 2: Existing dispatches
                    SELECT 
                        d.DispatchID,
                        so.SOID,
                        so.SONumber,
                        c.CustomerName,
                        d.PackID,
                        d.CourierName,
                        d.VehicleNumber,
                        d.TrackingNumber,
                        d.DispatchedBy,
                        u.FullName AS DispatchedByName,
                        d.DispatchDate,
                        d.Status,
                        so.OrderDate,
                        so.RequiredDate,
                        so.TotalAmount,
                        items.ItemCount,
                        items.TotalOrderedQty,
                        items.TotalDispatchedQty
                    FROM Dispatches d
                    INNER JOIN SalesOrders so ON d.SOID = so.SOID
                    INNER JOIN Customers c ON so.CustomerID = c.CustomerID
                    LEFT JOIN Users u ON d.DispatchedBy = u.UserID
                    LEFT JOIN (
                        SELECT SOID, 
                               COUNT(*) AS ItemCount, 
                               SUM(OrderedQty) AS TotalOrderedQty, 
                               SUM(DispatchedQty) AS TotalDispatchedQty
                        FROM SalesOrderItems
                        GROUP BY SOID
                    ) items ON so.SOID = items.SOID

                    ORDER BY DispatchDate DESC, SOID DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new DispatchTrackingDto
                    {
                        DispatchID = reader.IsDBNull("DispatchID") ? (int?)null : reader.GetInt32("DispatchID"),
                        SOID = reader.GetInt32("SOID"),
                        SONumber = reader.GetString("SONumber"),
                        CustomerName = reader.GetString("CustomerName"),
                        PackID = reader.IsDBNull("PackID") ? (int?)null : reader.GetInt32("PackID"),
                        CourierName = reader.IsDBNull("CourierName") ? "" : reader.GetString("CourierName"),
                        VehicleNumber = reader.IsDBNull("VehicleNumber") ? "" : reader.GetString("VehicleNumber"),
                        TrackingNumber = reader.IsDBNull("TrackingNumber") ? "" : reader.GetString("TrackingNumber"),
                        DispatchedBy = reader.IsDBNull("DispatchedBy") ? (int?)null : reader.GetInt32("DispatchedBy"),
                        DispatchedByName = reader.IsDBNull("DispatchedByName") ? "" : reader.GetString("DispatchedByName"),
                        DispatchDate = reader.IsDBNull("DispatchDate") ? (DateTime?)null : reader.GetDateTime("DispatchDate"),
                        Status = reader.GetString("Status"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        RequiredDate = reader.IsDBNull("RequiredDate") ? (DateTime?)null : reader.GetDateTime("RequiredDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalOrderedQty = reader.IsDBNull("TotalOrderedQty") ? 0 : reader.GetDecimal("TotalOrderedQty"),
                        TotalDispatchedQty = reader.IsDBNull("TotalDispatchedQty") ? 0 : reader.GetDecimal("TotalDispatchedQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching dispatch tasks.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<DispatchItemDto>> GetDispatchItemsAsync(int soid)
        {
            var results = new List<DispatchItemDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT soi.*, p.SKU, p.ProductName,
                           ISNULL(sr.ReservedQty, 0) AS ReservedQty,
                           (SELECT SUM(CurrentQty - ReservedQty - QCHoldQty - DamagedQty) 
                            FROM InventoryStocks 
                            WHERE ProductID = soi.ProductID) AS AvailableQty
                    FROM SalesOrderItems soi
                    INNER JOIN Products p ON soi.ProductID = p.ProductID
                    LEFT JOIN StockReservations sr ON soi.ProductID = sr.ProductID 
                        AND sr.ReferenceType = 'SalesOrder' 
                        AND sr.ReferenceNo = (SELECT SONumber FROM SalesOrders WHERE SOID = soi.SOID)
                        AND sr.Status = 'Active'
                    WHERE soi.SOID = @SOID", connection);

                command.Parameters.AddWithValue("@SOID", soid);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new DispatchItemDto
                    {
                        SOItemID = reader.GetInt32("SOItemID"),
                        SOID = reader.GetInt32("SOID"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        OrderedQty = reader.GetDecimal("OrderedQty"),
                        DispatchedQty = reader.GetDecimal("DispatchedQty"),
                        UnitPrice = reader.GetDecimal("UnitPrice"),
                        TotalPrice = reader.GetDecimal("TotalPrice"),
                        ReservedQty = reader.GetDecimal("ReservedQty"),
                        AvailableQty = reader.IsDBNull("AvailableQty") ? 0 : reader.GetDecimal("AvailableQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching dispatch items.", ex);
            }

            return results;
        }

        public async Task<DispatchActionResultDto> CreateDispatchAsync(DispatchCreateDto dispatch)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    INSERT INTO Dispatches (SOID, PackID, CourierName, VehicleNumber, TrackingNumber, DispatchedBy, DispatchDate, Status)
                    VALUES (@SOID, @PackID, @CourierName, @VehicleNumber, @TrackingNumber, @DispatchedBy, GETDATE(), 'Ready To Dispatch');
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@SOID", dispatch.SOID);
                command.Parameters.AddWithValue("@PackID", dispatch.PackID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CourierName", dispatch.CourierName);
                command.Parameters.AddWithValue("@VehicleNumber", dispatch.VehicleNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TrackingNumber", dispatch.TrackingNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DispatchedBy", dispatch.DispatchedBy);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return new DispatchActionResultDto { Success = true, Message = "Dispatch record created.", NewID = Convert.ToInt32(result) };
            }
            catch (Exception ex)
            {
                return new DispatchActionResultDto { Success = false, Message = "Database error: " + ex.Message };
            }
        }

        public async Task<DispatchActionResultDto> ConfirmDispatchAsync(int dispatchId, int performedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("dbo.sp_DispatchStock", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@DispatchID", dispatchId);
                command.Parameters.AddWithValue("@PerformedBy", performedBy);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                return new DispatchActionResultDto { Success = true, Message = "Dispatch confirmed. Stock reduced and reservation consumed." };
            }
            catch (SqlException ex)
            {
                return new DispatchActionResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new DispatchActionResultDto { Success = false, Message = "An unexpected error occurred: " + ex.Message };
            }
        }

        public async Task<DispatchActionResultDto> MarkDeliveredAsync(int dispatchId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var cmdDispatch = new SqlCommand(@"
                    UPDATE Dispatches SET Status = 'Delivered' WHERE DispatchID = @DispatchID;
                    SELECT SOID FROM Dispatches WHERE DispatchID = @DispatchID;", connection, transaction);
                cmdDispatch.Parameters.AddWithValue("@DispatchID", dispatchId);
                var soid = (int?)await cmdDispatch.ExecuteScalarAsync();

                if (soid.HasValue)
                {
                    using var cmdSO = new SqlCommand("UPDATE SalesOrders SET Status = 'Fulfilled' WHERE SOID = @SOID", connection, transaction);
                    cmdSO.Parameters.AddWithValue("@SOID", soid.Value);
                    await cmdSO.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return new DispatchActionResultDto { Success = true, Message = "Marked as Delivered and Order Fulfilled." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new DispatchActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<DispatchActionResultDto> MarkFailedAsync(int dispatchId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("UPDATE Dispatches SET Status = 'Failed' WHERE DispatchID = @DispatchID", connection);
                command.Parameters.AddWithValue("@DispatchID", dispatchId);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return new DispatchActionResultDto { Success = true, Message = "Dispatch marked as Failed." };
            }
            catch (Exception ex)
            {
                return new DispatchActionResultDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<DispatchTrackingDto?> GetDispatchByIdAsync(int dispatchId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT * FROM Dispatches WHERE DispatchID = @DispatchID", connection);
            command.Parameters.AddWithValue("@DispatchID", dispatchId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new DispatchTrackingDto
                {
                    DispatchID = reader.GetInt32("DispatchID"),
                    SOID = reader.GetInt32("SOID"),
                    Status = reader.GetString("Status")
                };
            }
            return null;
        }

        public async Task<SalesOrderForDispatchDto?> GetSalesOrderForDispatchAsync(int soId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT so.SOID, pk.PackID, so.SONumber, c.CustomerName
                FROM SalesOrders so
                INNER JOIN Customers c ON so.CustomerID = c.CustomerID
                LEFT JOIN Packing pk ON so.SOID = pk.SOID
                WHERE so.SOID = @SOID", connection);
            command.Parameters.AddWithValue("@SOID", soId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SalesOrderForDispatchDto
                {
                    SOID = reader.GetInt32("SOID"),
                    PackID = reader.IsDBNull("PackID") ? (int?)null : reader.GetInt32("PackID"),
                    SONumber = reader.GetString("SONumber"),
                    CustomerName = reader.GetString("CustomerName")
                };
            }
            return null;
        }

        public async Task<bool> HasDispatchForSOAsync(int soId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM Dispatches WHERE SOID = @SOID AND Status != 'Failed'", connection);
            command.Parameters.AddWithValue("@SOID", soId);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }
    }
}
