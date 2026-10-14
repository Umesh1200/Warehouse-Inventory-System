using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class SalesOrderRepository : ISalesOrderRepository
    {
        private readonly string _connectionString;

        public SalesOrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<SalesOrderDto>> GetAllSalesOrdersAsync()
        {
            var results = new List<SalesOrderDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT so.*, 
                           c.CustomerName, 
                           u.FullName AS CreatedByName,
                           items.ItemCount,
                           items.TotalOrderedQty,
                           items.TotalDispatchedQty
                    FROM SalesOrders so
                    INNER JOIN Customers c ON so.CustomerID = c.CustomerID
                    LEFT JOIN Users u ON so.CreatedBy = u.UserID
                    LEFT JOIN (
                        SELECT SOID, 
                               COUNT(*) AS ItemCount, 
                               SUM(OrderedQty) AS TotalOrderedQty, 
                               SUM(DispatchedQty) AS TotalDispatchedQty
                        FROM SalesOrderItems
                        GROUP BY SOID
                    ) items ON so.SOID = items.SOID
                    ORDER BY so.OrderDate DESC", connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new SalesOrderDto
                    {
                        SOID = reader.GetInt32("SOID"),
                        SONumber = reader.GetString("SONumber"),
                        CustomerID = reader.GetInt32("CustomerID"),
                        CustomerName = reader.GetString("CustomerName"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        RequiredDate = reader.IsDBNull("RequiredDate") ? (DateTime?)null : reader.GetDateTime("RequiredDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        CreatedByName = reader.IsDBNull("CreatedByName") ? "" : reader.GetString("CreatedByName"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks"),
                        ItemCount = reader.IsDBNull("ItemCount") ? 0 : reader.GetInt32("ItemCount"),
                        TotalOrderedQty = reader.IsDBNull("TotalOrderedQty") ? 0 : reader.GetDecimal("TotalOrderedQty"),
                        TotalDispatchedQty = reader.IsDBNull("TotalDispatchedQty") ? 0 : reader.GetDecimal("TotalDispatchedQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching sales orders.", ex);
            }

            return results;
        }

        public async Task<IEnumerable<SalesOrderItemDto>> GetSalesOrderItemsAsync(int soid)
        {
            var results = new List<SalesOrderItemDto>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT soi.*, p.SKU, p.ProductName
                    FROM SalesOrderItems soi
                    INNER JOIN Products p ON soi.ProductID = p.ProductID
                    WHERE soi.SOID = @SOID", connection);

                command.Parameters.AddWithValue("@SOID", soid);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new SalesOrderItemDto
                    {
                        SOItemID = reader.GetInt32("SOItemID"),
                        SOID = reader.GetInt32("SOID"),
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        OrderedQty = reader.GetDecimal("OrderedQty"),
                        DispatchedQty = reader.GetDecimal("DispatchedQty"),
                        UnitPrice = reader.GetDecimal("UnitPrice"),
                        TotalPrice = reader.GetDecimal("TotalPrice")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error occurred while fetching sales order items.", ex);
            }

            return results;
        }

        public async Task<int> CreateSalesOrderWithItemsAsync(SalesOrderCreateDto so)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                decimal totalAmount = 0;
                foreach (var item in so.Items)
                {
                    totalAmount += (item.OrderedQty * item.UnitPrice);
                }

                if (string.IsNullOrWhiteSpace(so.SONumber))
                {
                    so.SONumber = $"SO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                }

                using var cmdHeader = new SqlCommand(@"
                    INSERT INTO SalesOrders (SONumber, CustomerID, OrderDate, RequiredDate, TotalAmount, Status, Remarks, CreatedBy)
                    VALUES (@SONumber, @CustomerID, @OrderDate, @RequiredDate, @TotalAmount, 'Draft', @Remarks, @CreatedBy);
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                cmdHeader.Parameters.AddWithValue("@SONumber", so.SONumber);
                cmdHeader.Parameters.AddWithValue("@CustomerID", so.CustomerID);
                cmdHeader.Parameters.AddWithValue("@OrderDate", so.OrderDate);
                cmdHeader.Parameters.AddWithValue("@RequiredDate", so.RequiredDate ?? (object)DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@TotalAmount", totalAmount);
                cmdHeader.Parameters.AddWithValue("@Remarks", so.Remarks ?? (object)DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@CreatedBy", so.CreatedBy);

                var soId = Convert.ToInt32(await cmdHeader.ExecuteScalarAsync());

                foreach (var item in so.Items)
                {
                    using var cmdItem = new SqlCommand(@"
                        INSERT INTO SalesOrderItems (SOID, ProductID, OrderedQty, UnitPrice, DispatchedQty)
                        VALUES (@SOID, @ProductID, @OrderedQty, @UnitPrice, 0)", 
                        connection, transaction);

                    cmdItem.Parameters.AddWithValue("@SOID", soId);
                    cmdItem.Parameters.AddWithValue("@ProductID", item.ProductID);
                    cmdItem.Parameters.AddWithValue("@OrderedQty", item.OrderedQty);
                    cmdItem.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);

                    await cmdItem.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return soId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error creating Sales Order.", ex);
            }
        }

        public async Task<bool> UpdateSalesOrderWithItemsAsync(SalesOrderUpdateDto so)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                decimal totalAmount = 0;
                foreach (var item in so.Items)
                {
                    totalAmount += (item.OrderedQty * item.UnitPrice);
                }

                using var cmdHeader = new SqlCommand(@"
                    UPDATE SalesOrders 
                    SET SONumber = @SONumber, CustomerID = @CustomerID, OrderDate = @OrderDate, 
                        RequiredDate = @RequiredDate, TotalAmount = @TotalAmount, Remarks = @Remarks
                    WHERE SOID = @SOID AND Status = 'Draft'", connection, transaction);

                cmdHeader.Parameters.AddWithValue("@SOID", so.SOID);
                cmdHeader.Parameters.AddWithValue("@SONumber", so.SONumber);
                cmdHeader.Parameters.AddWithValue("@CustomerID", so.CustomerID);
                cmdHeader.Parameters.AddWithValue("@OrderDate", so.OrderDate);
                cmdHeader.Parameters.AddWithValue("@RequiredDate", so.RequiredDate ?? (object)DBNull.Value);
                cmdHeader.Parameters.AddWithValue("@TotalAmount", totalAmount);
                cmdHeader.Parameters.AddWithValue("@Remarks", so.Remarks ?? (object)DBNull.Value);

                var rowsAffected = await cmdHeader.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                using var cmdDelete = new SqlCommand("DELETE FROM SalesOrderItems WHERE SOID = @SOID", connection, transaction);
                cmdDelete.Parameters.AddWithValue("@SOID", so.SOID);
                await cmdDelete.ExecuteNonQueryAsync();

                foreach (var item in so.Items)
                {
                    using var cmdItem = new SqlCommand(@"
                        INSERT INTO SalesOrderItems (SOID, ProductID, OrderedQty, UnitPrice, DispatchedQty)
                        VALUES (@SOID, @ProductID, @OrderedQty, @UnitPrice, 0)", 
                        connection, transaction);

                    cmdItem.Parameters.AddWithValue("@SOID", so.SOID);
                    cmdItem.Parameters.AddWithValue("@ProductID", item.ProductID);
                    cmdItem.Parameters.AddWithValue("@OrderedQty", item.OrderedQty);
                    cmdItem.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);

                    await cmdItem.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error updating Sales Order.", ex);
            }
        }

        public async Task<bool> ConfirmSalesOrderAsync(int soId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE SalesOrders 
                    SET Status = 'Confirmed'
                    WHERE SOID = @SOID AND Status = 'Draft'", connection);

                command.Parameters.AddWithValue("@SOID", soId);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error confirming Sales Order.", ex);
            }
        }

        public async Task<bool> CancelSalesOrderAsync(int soId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    UPDATE SalesOrders 
                    SET Status = 'Cancelled'
                    WHERE SOID = @SOID AND Status IN ('Draft', 'Confirmed')", connection);

                command.Parameters.AddWithValue("@SOID", soId);

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error cancelling Sales Order.", ex);
            }
        }

        public async Task<SalesOrderDto?> GetSalesOrderByIdAsync(int soId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand("SELECT * FROM SalesOrders WHERE SOID = @SOID", connection);
                command.Parameters.AddWithValue("@SOID", soId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new SalesOrderDto
                    {
                        SOID = reader.GetInt32("SOID"),
                        SONumber = reader.GetString("SONumber"),
                        CustomerID = reader.GetInt32("CustomerID"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        RequiredDate = reader.IsDBNull("RequiredDate") ? (DateTime?)null : reader.GetDateTime("RequiredDate"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        Remarks = reader.IsDBNull("Remarks") ? "" : reader.GetString("Remarks")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Sales Order details.", ex);
            }
        }

        public async Task<bool> IsSONumberExistsAsync(string soNumber)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM SalesOrders WHERE SONumber = @SONumber", connection);
            command.Parameters.AddWithValue("@SONumber", soNumber);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<bool> IsSONumberExistsForOtherSOAsync(string soNumber, int soId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM SalesOrders WHERE SONumber = @SONumber AND SOID != @SOID", connection);
            command.Parameters.AddWithValue("@SONumber", soNumber);
            command.Parameters.AddWithValue("@SOID", soId);
            await connection.OpenAsync();
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<IEnumerable<dynamic>> GetActiveCustomersAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT CustomerID, CustomerCode, CustomerName FROM Customers WHERE Status = 'Active' ORDER BY CustomerName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.GetString(2) });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetActiveProductsAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ProductID, SKU, ProductName FROM Products WHERE Status = 'Active' ORDER BY ProductName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new { ID = reader.GetInt32(0), SKU = reader.GetString(1), Name = reader.GetString(2) });
            }
            return results;
        }

        public async Task<decimal> GetAvailableStockForProductAsync(int productId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT SUM(dbo.fn_GetAvailableStock(ProductID, ShelfID)) FROM InventoryStocks WHERE ProductID = @ProductID", connection);
            command.Parameters.AddWithValue("@ProductID", productId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result == DBNull.Value || result == null ? 0m : Convert.ToDecimal(result);
        }

        public async Task<IEnumerable<InventoryOverviewDto>> GetProductStockLocationsAsync(int productId)
        {
            var results = new List<InventoryOverviewDto>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(@"
                    SELECT WarehouseName, ShelfCode, CurrentQty, AvailableQty, ReservedQty, QCHoldQty, DamagedQty
                    FROM vw_InventoryOverview
                    WHERE ProductID = @ProductID AND CurrentQty > 0
                    ORDER BY WarehouseName, ShelfCode", connection);
                command.Parameters.AddWithValue("@ProductID", productId);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new InventoryOverviewDto
                    {
                        ProductID = productId,
                        WarehouseName = reader.GetString("WarehouseName"),
                        ShelfCode = reader.GetString("ShelfCode"),
                        CurrentQty = reader.GetDecimal("CurrentQty"),
                        AvailableQty = reader.GetDecimal("AvailableQty"),
                        ReservedQty = reader.GetDecimal("ReservedQty"),
                        QCHoldQty = reader.GetDecimal("QCHoldQty"),
                        DamagedQty = reader.GetDecimal("DamagedQty")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching product stock locations.", ex);
            }
            return results;
        }
    }
}

