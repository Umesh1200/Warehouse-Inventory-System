using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly string _connectionString;

        public WarehouseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<IEnumerable<dynamic>> GetWarehousesAsync()
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT WarehouseID, WarehouseName, WarehouseCode FROM Warehouses WHERE IsActive = 1 ORDER BY WarehouseName", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                string code = reader.IsDBNull(2) ? "" : reader.GetString(2);
                string displayName = string.IsNullOrWhiteSpace(code) ? name : $"{name} ({code})";
                results.Add(new { ID = id, Name = displayName, Code = code });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetFloorsByWarehouseAsync(int warehouseId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT FloorID, FloorName, FloorCode FROM Floors WHERE WarehouseID = @WarehouseID AND IsActive = 1 ORDER BY FloorName", connection);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                string code = reader.IsDBNull(2) ? "" : reader.GetString(2);
                string displayName = string.IsNullOrWhiteSpace(code) 
                    ? (string.IsNullOrWhiteSpace(name) ? $"Floor #{id}" : name)
                    : (string.IsNullOrWhiteSpace(name) ? code : $"{name} ({code})");
                results.Add(new { ID = id, Name = displayName, Code = code });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetRoomsByFloorAsync(int floorId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT RoomID, RoomName, RoomCode FROM Rooms WHERE FloorID = @FloorID AND IsActive = 1 ORDER BY RoomName", connection);
            command.Parameters.AddWithValue("@FloorID", floorId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                string code = reader.IsDBNull(2) ? "" : reader.GetString(2);
                string displayName = string.IsNullOrWhiteSpace(code) 
                    ? (string.IsNullOrWhiteSpace(name) ? $"Room #{id}" : name)
                    : (string.IsNullOrWhiteSpace(name) ? code : $"{name} ({code})");
                results.Add(new { ID = id, Name = displayName, Code = code });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetRacksByRoomAsync(int roomId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT RackID, RackName, RackCode FROM Racks WHERE RoomID = @RoomID AND IsActive = 1 ORDER BY RackName", connection);
            command.Parameters.AddWithValue("@RoomID", roomId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                string code = reader.IsDBNull(2) ? "" : reader.GetString(2);
                string displayName = string.IsNullOrWhiteSpace(code) 
                    ? (string.IsNullOrWhiteSpace(name) ? $"Rack #{id}" : name)
                    : (string.IsNullOrWhiteSpace(name) ? code : $"{name} ({code})");
                results.Add(new { ID = id, Name = displayName, Code = code });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetShelvesByRackAsync(int rackId)
        {
            var results = new List<dynamic>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ShelfID, ShelfCode, ShelfName, ShelfType FROM Shelves WHERE RackID = @RackID AND IsActive = 1 ORDER BY ShelfCode", connection);
            command.Parameters.AddWithValue("@RackID", rackId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                string code = reader.GetString(1);
                string name = reader.IsDBNull(2) ? "" : reader.GetString(2);
                string type = reader.IsDBNull(3) ? "General" : reader.GetString(3);
                string displayName = string.IsNullOrWhiteSpace(name) ? $"{code} [{type}]" : $"{name} ({code}) [{type}]";
                results.Add(new { ID = id, Name = displayName, Code = code, Type = type });
            }
            return results;
        }

        public async Task<IEnumerable<dynamic>> GetShelvesByFloorOrWarehouseAsync(int? floorId, int? warehouseId)
        {
            var results = new List<dynamic>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                string query = @"
                    SELECT DISTINCT s.ShelfID, s.ShelfCode, s.ShelfName
                    FROM Shelves s
                    LEFT JOIN Racks rk ON s.RackID = rk.RackID
                    LEFT JOIN Rooms rm ON rk.RoomID = rm.RoomID
                    LEFT JOIN Floors f ON rm.FloorID = f.FloorID
                    LEFT JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                    WHERE s.IsActive = 1
                      AND (@FloorID IS NULL OR f.FloorID = @FloorID)
                      AND (@WarehouseID IS NULL OR w.WarehouseID = @WarehouseID)
                    ORDER BY s.ShelfCode";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FloorID", (object?)floorId ?? DBNull.Value);
                command.Parameters.AddWithValue("@WarehouseID", (object?)warehouseId ?? DBNull.Value);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string shelfCode = reader.GetString(1);
                    string shelfName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    results.Add(new {
                        ID = reader.GetInt32(0),
                        Name = string.IsNullOrWhiteSpace(shelfName) ? shelfCode : $"{shelfCode} ({shelfName})"
                    });
                }

                // If hierarchy had no matching shelves, fallback to active shelves
                if (results.Count == 0 && (floorId.HasValue || warehouseId.HasValue))
                {
                    using var fbCommand = new SqlCommand("SELECT ShelfID, ShelfCode, ShelfName FROM Shelves WHERE IsActive = 1 ORDER BY ShelfCode", connection);
                    using var fbReader = await fbCommand.ExecuteReaderAsync();
                    while (await fbReader.ReadAsync())
                    {
                        string shelfCode = fbReader.GetString(1);
                        string shelfName = fbReader.IsDBNull(2) ? "" : fbReader.GetString(2);
                        results.Add(new {
                            ID = fbReader.GetInt32(0),
                            Name = string.IsNullOrWhiteSpace(shelfName) ? shelfCode : $"{shelfCode} ({shelfName})"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetShelvesByFloorOrWarehouseAsync: " + ex.Message);
            }
            return results;
        }

        public async Task<int> CreateWarehouseAsync(string code, string name, string location)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"INSERT INTO Warehouses (WarehouseCode, WarehouseName, Location, IsActive) VALUES (@Code, @Name, @Location, 1); SELECT SCOPE_IDENTITY();", connection);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Location", location);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateWarehouseAsync(int id, string code, string name, string location)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Warehouses SET WarehouseCode = @Code, WarehouseName = @Name, Location = @Location WHERE WarehouseID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Location", location);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ToggleWarehouseStatusAsync(int id, bool isActive)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Warehouses SET IsActive = @IsActive WHERE WarehouseID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@IsActive", isActive);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<dynamic?> GetWarehouseByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT WarehouseID, WarehouseCode, WarehouseName, Location FROM Warehouses WHERE WarehouseID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return new { ID = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.GetString(2), Location = reader.IsDBNull(3) ? "" : reader.GetString(3) };
            return null;
        }

        public async Task<int> CreateFloorAsync(int warehouseId, string code, string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO Floors (WarehouseID, FloorCode, FloorName, IsActive) VALUES (@WarehouseID, @Code, @Name, 1); SELECT SCOPE_IDENTITY();", connection);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateFloorAsync(int id, int warehouseId, string code, string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Floors SET WarehouseID = @WarehouseID, FloorCode = @Code, FloorName = @Name WHERE FloorID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ToggleFloorStatusAsync(int id, bool isActive)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Floors SET IsActive = @IsActive WHERE FloorID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@IsActive", isActive);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> FloorCodeExistsAsync(int warehouseId, string code, int? excludeId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT COUNT(*) FROM Floors WHERE WarehouseID = @WarehouseID AND FloorCode = @Code";
            if (excludeId.HasValue) sql += " AND FloorID <> @ExcludeID";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@WarehouseID", warehouseId);
            command.Parameters.AddWithValue("@Code", code);
            if (excludeId.HasValue) command.Parameters.AddWithValue("@ExcludeID", excludeId.Value);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<int> GetFloorStockCountAsync(int floorId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM InventoryStocks ist INNER JOIN Shelves s ON ist.ShelfID = s.ShelfID INNER JOIN Racks r ON s.RackID = r.RackID INNER JOIN Rooms rm ON r.RoomID = rm.RoomID WHERE rm.FloorID = @FloorID AND ist.CurrentQty > 0", connection);
            command.Parameters.AddWithValue("@FloorID", floorId);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<dynamic?> GetFloorByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT FloorID, FloorCode, FloorName, WarehouseID FROM Floors WHERE FloorID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return new { ID = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.GetString(2), WarehouseID = reader.GetInt32(3) };
            return null;
        }

        public async Task<int> CreateRoomAsync(int floorId, string code, string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO Rooms (FloorID, RoomCode, RoomName, IsActive) VALUES (@FloorID, @Code, @Name, 1); SELECT SCOPE_IDENTITY();", connection);
            command.Parameters.AddWithValue("@FloorID", floorId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateRoomAsync(int id, int floorId, string code, string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Rooms SET FloorID = @FloorID, RoomCode = @Code, RoomName = @Name WHERE RoomID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@FloorID", floorId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ToggleRoomStatusAsync(int id, bool isActive)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Rooms SET IsActive = @IsActive WHERE RoomID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@IsActive", isActive);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RoomCodeExistsAsync(int floorId, string code, int? excludeId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT COUNT(*) FROM Rooms WHERE FloorID = @FloorID AND RoomCode = @Code";
            if (excludeId.HasValue) sql += " AND RoomID <> @ExcludeID";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@FloorID", floorId);
            command.Parameters.AddWithValue("@Code", code);
            if (excludeId.HasValue) command.Parameters.AddWithValue("@ExcludeID", excludeId.Value);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<int> GetRoomStockCountAsync(int roomId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM InventoryStocks ist INNER JOIN Shelves s ON ist.ShelfID = s.ShelfID INNER JOIN Racks r ON s.RackID = r.RackID WHERE r.RoomID = @RoomID AND ist.CurrentQty > 0", connection);
            command.Parameters.AddWithValue("@RoomID", roomId);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<dynamic?> GetRoomByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT RoomID, RoomCode, RoomName, FloorID FROM Rooms WHERE RoomID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return new { ID = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.GetString(2), FloorID = reader.GetInt32(3) };
            return null;
        }

        public async Task<int> CreateRackAsync(int roomId, string code, string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO Racks (RoomID, RackCode, RackName, IsActive) VALUES (@RoomID, @Code, @Name, 1); SELECT SCOPE_IDENTITY();", connection);
            command.Parameters.AddWithValue("@RoomID", roomId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateRackAsync(int id, int roomId, string code, string name)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Racks SET RoomID = @RoomID, RackCode = @Code, RackName = @Name WHERE RackID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@RoomID", roomId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ToggleRackStatusAsync(int id, bool isActive)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Racks SET IsActive = @IsActive WHERE RackID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@IsActive", isActive);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RackCodeExistsAsync(int roomId, string code, int? excludeId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT COUNT(*) FROM Racks WHERE RoomID = @RoomID AND RackCode = @Code";
            if (excludeId.HasValue) sql += " AND RackID <> @ExcludeID";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RoomID", roomId);
            command.Parameters.AddWithValue("@Code", code);
            if (excludeId.HasValue) command.Parameters.AddWithValue("@ExcludeID", excludeId.Value);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<int> GetRackStockCountAsync(int rackId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT COUNT(*) FROM InventoryStocks ist INNER JOIN Shelves s ON ist.ShelfID = s.ShelfID WHERE s.RackID = @RackID AND ist.CurrentQty > 0", connection);
            command.Parameters.AddWithValue("@RackID", rackId);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<dynamic?> GetRackByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT RackID, RackCode, RackName, RoomID FROM Racks WHERE RackID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return new { ID = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.GetString(2), RoomID = reader.GetInt32(3) };
            return null;
        }

        public async Task<int> CreateShelfAsync(int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("INSERT INTO Shelves (RackID, ShelfCode, ShelfName, ShelfType, MaxQty, MaxWeightKg, IsActive) VALUES (@RackID, @Code, @Name, @Type, @MaxQty, @MaxWeight, 1); SELECT SCOPE_IDENTITY();", connection);
            command.Parameters.AddWithValue("@RackID", rackId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Type", type);
            command.Parameters.AddWithValue("@MaxQty", maxQty);
            command.Parameters.AddWithValue("@MaxWeight", maxWeight);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateShelfAsync(int id, int rackId, string code, string name, string type, decimal maxQty, decimal maxWeight)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Shelves SET RackID = @RackID, ShelfCode = @Code, ShelfName = @Name, ShelfType = @Type, MaxQty = @MaxQty, MaxWeightKg = @MaxWeight WHERE ShelfID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@RackID", rackId);
            command.Parameters.AddWithValue("@Code", code);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Type", type);
            command.Parameters.AddWithValue("@MaxQty", maxQty);
            command.Parameters.AddWithValue("@MaxWeight", maxWeight);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ToggleShelfStatusAsync(int id, bool isActive)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("UPDATE Shelves SET IsActive = @IsActive WHERE ShelfID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            command.Parameters.AddWithValue("@IsActive", isActive);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ShelfCodeExistsAsync(int rackId, string code, int? excludeId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT COUNT(*) FROM Shelves WHERE RackID = @RackID AND ShelfCode = @Code";
            if (excludeId.HasValue) sql += " AND ShelfID <> @ExcludeID";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RackID", rackId);
            command.Parameters.AddWithValue("@Code", code);
            if (excludeId.HasValue) command.Parameters.AddWithValue("@ExcludeID", excludeId.Value);
            await connection.OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<(decimal UsedQty, decimal UsedWeight)> GetShelfUsageAsync(int shelfId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT SUM(ist.CurrentQty), SUM(ist.CurrentQty * p.WeightKg)
                FROM InventoryStocks ist
                INNER JOIN Products p ON ist.ProductID = p.ProductID
                WHERE ist.ShelfID = @ShelfID", connection);
            command.Parameters.AddWithValue("@ShelfID", shelfId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return (reader.IsDBNull(0) ? 0 : reader.GetDecimal(0), reader.IsDBNull(1) ? 0 : reader.GetDecimal(1));
            return (0, 0);
        }

        public async Task<dynamic?> GetShelfByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT ShelfID, ShelfCode, ShelfName, ShelfType, MaxQty, MaxWeightKg, RackID FROM Shelves WHERE ShelfID = @ID", connection);
            command.Parameters.AddWithValue("@ID", id);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return new { ID = reader.GetInt32(0), Code = reader.GetString(1), Name = reader.IsDBNull(2) ? "" : reader.GetString(2), Type = reader.GetString(3), MaxQty = reader.GetDecimal(4), MaxWeight = reader.GetDecimal(5), RackID = reader.GetInt32(6) };
            return null;
        }

        public async Task<IEnumerable<WarehouseSetupDto>> GetAllShelvesWithHierarchyAsync()
        {
            var results = new List<WarehouseSetupDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT
                    w.WarehouseID,
                    w.WarehouseCode,
                    w.WarehouseName,
                    w.IsActive AS WarehouseActive,
                    ISNULL(f.FloorID, 0) AS FloorID,
                    ISNULL(f.FloorCode, '') AS FloorCode,
                    ISNULL(f.FloorName, '') AS FloorName,
                    ISNULL(f.IsActive, 0) AS FloorActive,
                    ISNULL(rm.RoomID, 0) AS RoomID,
                    ISNULL(rm.RoomCode, '') AS RoomCode,
                    ISNULL(rm.RoomName, '') AS RoomName,
                    ISNULL(rm.IsActive, 0) AS RoomActive,
                    ISNULL(rk.RackID, 0) AS RackID,
                    ISNULL(rk.RackCode, '') AS RackCode,
                    ISNULL(rk.RackName, '') AS RackName,
                    ISNULL(rk.IsActive, 0) AS RackActive,
                    ISNULL(s.ShelfID, 0) AS ShelfID,
                    ISNULL(s.ShelfCode, '') AS ShelfCode,
                    ISNULL(s.ShelfName, '') AS ShelfName,
                    ISNULL(s.ShelfType, '') AS ShelfType,
                    ISNULL(s.MaxQty, 0) AS MaxQty,
                    ISNULL(s.MaxWeightKg, 0) AS MaxWeightKg,
                    ISNULL(stock.CurrentQty, 0) AS CurrentQty,
                    ISNULL(stock.CurrentWeightKg, 0) AS CurrentWeightKg,
                    CASE WHEN s.MaxQty > 0 THEN (ISNULL(stock.CurrentQty, 0) / s.MaxQty) * 100 ELSE 0 END AS UtilizationPercentage,
                    ISNULL(mainStock.SKU, '') AS SKU,
                    ISNULL(mainStock.ProductName, '') AS ProductName,
                    ISNULL(mainStock.ProductType, '') AS ProductType,
                    ISNULL(mainStock.AvailableQty, 0) AS AvailableQty,
                    ISNULL(mainStock.ReorderLevel, 0) AS ReorderLevel,
                    ISNULL(s.IsActive, 0) AS IsActive
                FROM Warehouses w
                LEFT JOIN Floors f ON w.WarehouseID = f.WarehouseID
                LEFT JOIN Rooms rm ON f.FloorID = rm.FloorID
                LEFT JOIN Racks rk ON rm.RoomID = rk.RoomID
                LEFT JOIN Shelves s ON rk.RackID = s.RackID
                OUTER APPLY (
                    SELECT
                        SUM(ist.CurrentQty) AS CurrentQty,
                        SUM(ist.CurrentQty * ISNULL(p.WeightKg, 0)) AS CurrentWeightKg
                    FROM InventoryStocks ist
                    INNER JOIN Products p ON ist.ProductID = p.ProductID
                    WHERE ist.ShelfID = s.ShelfID
                ) stock
                OUTER APPLY (
                    SELECT TOP 1
                        p.SKU,
                        p.ProductName,
                        p.ProductType,
                        ist.CurrentQty - ist.ReservedQty - ist.QCHoldQty - ist.DamagedQty AS AvailableQty,
                        p.ReorderLevel
                    FROM InventoryStocks ist
                    INNER JOIN Products p ON ist.ProductID = p.ProductID
                    WHERE ist.ShelfID = s.ShelfID
                    ORDER BY ist.CurrentQty DESC, p.SKU
                ) mainStock
                ORDER BY w.WarehouseCode, f.FloorCode, rm.RoomCode, rk.RackCode, s.ShelfCode", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new WarehouseSetupDto
                {
                    WarehouseID = reader.GetInt32("WarehouseID"),
                    WarehouseCode = reader.GetString("WarehouseCode"),
                    WarehouseName = reader.GetString("WarehouseName"),
                    WarehouseActive = reader.GetBoolean("WarehouseActive"),
                    FloorID = reader.GetInt32("FloorID"),
                    FloorCode = reader.GetString("FloorCode"),
                    FloorName = reader.IsDBNull("FloorName") ? "" : reader.GetString("FloorName"),
                    FloorActive = reader.GetBoolean("FloorActive"),
                    RoomID = reader.GetInt32("RoomID"),
                    RoomCode = reader.GetString("RoomCode"),
                    RoomName = reader.IsDBNull("RoomName") ? "" : reader.GetString("RoomName"),
                    RoomActive = reader.GetBoolean("RoomActive"),
                    RackID = reader.GetInt32("RackID"),
                    RackCode = reader.GetString("RackCode"),
                    RackName = reader.IsDBNull("RackName") ? "" : reader.GetString("RackName"),
                    RackActive = reader.GetBoolean("RackActive"),
                    ShelfID = reader.GetInt32("ShelfID"),
                    ShelfCode = reader.GetString("ShelfCode"),
                    ShelfName = reader.IsDBNull("ShelfName") ? "" : reader.GetString("ShelfName"),
                    ShelfType = reader.GetString("ShelfType"),
                    MaxQty = reader.GetDecimal("MaxQty"),
                    MaxWeightKg = reader.GetDecimal("MaxWeightKg"),
                    CurrentQty = reader.GetDecimal("CurrentQty"),
                    CurrentWeightKg = reader.GetDecimal("CurrentWeightKg"),
                    UtilizationPercentage = reader.GetDecimal("UtilizationPercentage"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    ProductType = reader.GetString("ProductType"),
                    AvailableQty = reader.GetDecimal("AvailableQty"),
                    ReorderLevel = reader.GetDecimal("ReorderLevel"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }

            return results;
        }

        public async Task<IEnumerable<WarehouseSetupDto>> GetWarehouseVisualizationAsync()
        {
            return await GetAllShelvesWithHierarchyAsync();
        }

        public async Task<IEnumerable<ProductLocatorSearchResultDto>> SearchProductsForLocatorAsync(string query)
        {
            var results = new List<ProductLocatorSearchResultDto>();
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT TOP 20
                    p.ProductID,
                    p.SKU,
                    p.ProductName,
                    ISNULL(c.CategoryName, '') AS CategoryName,
                    ISNULL(p.ProductType, '') AS ProductType,
                    ISNULL(SUM(ist.CurrentQty), 0) AS TotalCurrentQty,
                    ISNULL(SUM(dbo.fn_GetAvailableStock(p.ProductID, ist.ShelfID)), 0) AS TotalAvailableQty,
                    COUNT(DISTINCT ist.ShelfID) AS ShelfCount
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                LEFT JOIN InventoryStocks ist ON p.ProductID = ist.ProductID AND ist.CurrentQty > 0
                WHERE (@Query = '' OR p.SKU LIKE '%' + @Query + '%' OR p.ProductName LIKE '%' + @Query + '%')
                GROUP BY p.ProductID, p.SKU, p.ProductName, c.CategoryName, p.ProductType
                ORDER BY 
                    CASE 
                        WHEN p.SKU = @Query THEN 0 
                        WHEN p.SKU LIKE @Query + '%' THEN 1 
                        WHEN p.ProductName LIKE @Query + '%' THEN 2
                        ELSE 3 
                    END,
                    p.SKU, p.ProductName";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Query", query?.Trim() ?? string.Empty);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new ProductLocatorSearchResultDto
                {
                    ProductID = reader.GetInt32("ProductID"),
                    SKU = reader.GetString("SKU"),
                    ProductName = reader.GetString("ProductName"),
                    CategoryName = reader.GetString("CategoryName"),
                    ProductType = reader.GetString("ProductType"),
                    TotalCurrentQty = reader.GetDecimal("TotalCurrentQty"),
                    TotalAvailableQty = reader.GetDecimal("TotalAvailableQty"),
                    ShelfCount = reader.GetInt32("ShelfCount")
                });
            }

            return results;
        }

        public async Task<ProductLocatorDetailDto?> GetProductLocationDetailsAsync(int? productId, string? sku)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // 1. Fetch Product
            ProductLocatorDetailDto? detail = null;
            var productSql = @"
                SELECT TOP 1
                    p.ProductID,
                    p.SKU,
                    p.ProductName,
                    ISNULL(c.CategoryName, '') AS CategoryName,
                    ISNULL(p.ProductType, '') AS ProductType,
                    ISNULL(u.UnitName, '') AS UnitName,
                    ISNULL(p.StandardCost, 0) AS StandardCost,
                    ISNULL(p.SellingPrice, 0) AS SellingPrice,
                    ISNULL(p.ReorderLevel, 0) AS ReorderLevel,
                    ISNULL(p.WeightKg, 0) AS WeightKg,
                    ISNULL(p.Dimensions, '') AS Dimensions,
                    ISNULL(p.Status, 'Active') AS Status
                FROM Products p
                LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                LEFT JOIN Units u ON p.UnitID = u.UnitID
                WHERE (@ProductID IS NOT NULL AND p.ProductID = @ProductID)
                   OR (@SKU IS NOT NULL AND @SKU <> '' AND UPPER(LTRIM(RTRIM(p.SKU))) = UPPER(LTRIM(RTRIM(@SKU))))";

            using (var cmd = new SqlCommand(productSql, connection))
            {
                cmd.Parameters.AddWithValue("@ProductID", (object?)productId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SKU", (object?)sku?.Trim() ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    detail = new ProductLocatorDetailDto
                    {
                        ProductID = reader.GetInt32("ProductID"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        CategoryName = reader.GetString("CategoryName"),
                        ProductType = reader.GetString("ProductType"),
                        UnitName = reader.GetString("UnitName"),
                        StandardCost = reader.GetDecimal("StandardCost"),
                        SellingPrice = reader.GetDecimal("SellingPrice"),
                        ReorderLevel = reader.GetDecimal("ReorderLevel"),
                        WeightKg = reader.GetDecimal("WeightKg"),
                        Dimensions = reader.GetString("Dimensions"),
                        Status = reader.GetString("Status")
                    };
                }
            }

            if (detail == null) return null;

            // 2. Fetch Shelf Locations
            var locationsSql = @"
                SELECT 
                    w.WarehouseID,
                    w.WarehouseCode,
                    w.WarehouseName,
                    f.FloorID,
                    f.FloorCode,
                    ISNULL(f.FloorName, '') AS FloorName,
                    rm.RoomID,
                    rm.RoomCode,
                    ISNULL(rm.RoomName, '') AS RoomName,
                    rk.RackID,
                    rk.RackCode,
                    ISNULL(rk.RackName, '') AS RackName,
                    s.ShelfID,
                    s.ShelfCode,
                    ISNULL(s.ShelfName, '') AS ShelfName,
                    ISNULL(s.ShelfType, 'General') AS ShelfType,
                    ISNULL(s.MaxQty, 0) AS ShelfMaxQty,
                    ISNULL(s.MaxWeightKg, 0) AS ShelfMaxWeightKg,
                    ISNULL(totalShelfStock.ShelfCurrentTotalQty, 0) AS ShelfCurrentTotalQty,
                    ISNULL(dbo.fn_GetShelfAvailableCapacity(s.ShelfID), 0) AS ShelfAvailableCapacity,
                    CASE WHEN s.MaxQty > 0 THEN (ISNULL(totalShelfStock.ShelfCurrentTotalQty, 0) / s.MaxQty) * 100 ELSE 0 END AS ShelfUtilizationPercentage,
                    ISNULL(ist.CurrentQty, 0) AS ProductCurrentQty,
                    ISNULL(dbo.fn_GetAvailableStock(p.ProductID, s.ShelfID), 0) AS ProductAvailableQty,
                    ISNULL(ist.ReservedQty, 0) AS ProductReservedQty,
                    ISNULL(ist.QCHoldQty, 0) AS ProductQCHoldQty,
                    ISNULL(ist.DamagedQty, 0) AS ProductDamagedQty,
                    ISNULL(dbo.fn_GetStockStatus(p.ProductID, s.ShelfID), 'In Stock') AS ProductStockStatus,
                    ist.LastUpdated
                FROM InventoryStocks ist
                INNER JOIN Products p ON ist.ProductID = p.ProductID
                INNER JOIN Shelves s ON ist.ShelfID = s.ShelfID
                INNER JOIN Racks rk ON s.RackID = rk.RackID
                INNER JOIN Rooms rm ON rk.RoomID = rm.RoomID
                INNER JOIN Floors f ON rm.FloorID = f.FloorID
                INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
                OUTER APPLY (
                    SELECT SUM(ist2.CurrentQty) AS ShelfCurrentTotalQty
                    FROM InventoryStocks ist2
                    WHERE ist2.ShelfID = s.ShelfID
                ) totalShelfStock
                WHERE p.ProductID = @ProductID
                  AND (ist.CurrentQty > 0 OR ist.ReservedQty > 0 OR ist.QCHoldQty > 0 OR ist.DamagedQty > 0)
                ORDER BY w.WarehouseName, f.FloorName, rm.RoomName, rk.RackName, s.ShelfCode";

            using (var cmd = new SqlCommand(locationsSql, connection))
            {
                cmd.Parameters.AddWithValue("@ProductID", detail.ProductID);
                using var reader = await cmd.ExecuteReaderAsync();
                int locIndex = 0;
                while (await reader.ReadAsync())
                {
                    var shelfCode = reader.GetString("ShelfCode");
                    int tier = 1;
                    var parts = shelfCode.Split('-');
                    if (parts.Length > 0)
                    {
                        var lastPart = parts[^1].Trim();
                        if (lastPart.StartsWith("S", StringComparison.OrdinalIgnoreCase) && int.TryParse(lastPart.Substring(1), out int parsedTier))
                        {
                            tier = parsedTier;
                        }
                        else if (int.TryParse(lastPart, out int numTier))
                        {
                            tier = numTier;
                        }
                        else
                        {
                            tier = (locIndex % 4) + 1;
                        }
                    }
                    else
                    {
                        tier = (locIndex % 4) + 1;
                    }

                    detail.Locations.Add(new ProductShelfLocationDto
                    {
                        WarehouseID = reader.GetInt32("WarehouseID"),
                        WarehouseCode = reader.GetString("WarehouseCode"),
                        WarehouseName = reader.GetString("WarehouseName"),
                        FloorID = reader.GetInt32("FloorID"),
                        FloorCode = reader.GetString("FloorCode"),
                        FloorName = reader.GetString("FloorName"),
                        RoomID = reader.GetInt32("RoomID"),
                        RoomCode = reader.GetString("RoomCode"),
                        RoomName = reader.GetString("RoomName"),
                        RackID = reader.GetInt32("RackID"),
                        RackCode = reader.GetString("RackCode"),
                        RackName = reader.GetString("RackName"),
                        ShelfID = reader.GetInt32("ShelfID"),
                        ShelfCode = shelfCode,
                        ShelfName = reader.GetString("ShelfName"),
                        ShelfType = reader.GetString("ShelfType"),
                        ShelfMaxQty = reader.GetDecimal("ShelfMaxQty"),
                        ShelfMaxWeightKg = reader.GetDecimal("ShelfMaxWeightKg"),
                        ShelfCurrentTotalQty = reader.GetDecimal("ShelfCurrentTotalQty"),
                        ShelfAvailableCapacity = reader.GetDecimal("ShelfAvailableCapacity"),
                        ShelfUtilizationPercentage = Math.Round(reader.GetDecimal("ShelfUtilizationPercentage"), 1),
                        ProductCurrentQty = reader.GetDecimal("ProductCurrentQty"),
                        ProductAvailableQty = reader.GetDecimal("ProductAvailableQty"),
                        ProductReservedQty = reader.GetDecimal("ProductReservedQty"),
                        ProductQCHoldQty = reader.GetDecimal("ProductQCHoldQty"),
                        ProductDamagedQty = reader.GetDecimal("ProductDamagedQty"),
                        ProductStockStatus = reader.GetString("ProductStockStatus"),
                        LastUpdated = reader.IsDBNull("LastUpdated") ? null : reader.GetDateTime("LastUpdated"),
                        TierLevel = Math.Max(1, tier),
                        SlotPosition = 1
                    });
                    locIndex++;
                }
            }

            // Totals calculations
            detail.TotalCurrentQty = detail.Locations.Sum(l => l.ProductCurrentQty);
            detail.TotalAvailableQty = detail.Locations.Sum(l => l.ProductAvailableQty);
            detail.TotalReservedQty = detail.Locations.Sum(l => l.ProductReservedQty);
            detail.TotalQCHoldQty = detail.Locations.Sum(l => l.ProductQCHoldQty);
            detail.TotalDamagedQty = detail.Locations.Sum(l => l.ProductDamagedQty);
            detail.TotalValuation = detail.TotalCurrentQty * detail.StandardCost;
            detail.ShelfCount = detail.Locations.Count;
            detail.WarehouseCount = detail.Locations.Select(l => l.WarehouseID).Distinct().Count();

            if (detail.TotalAvailableQty <= 0)
                detail.OverallStockStatus = "Out of Stock";
            else if (detail.TotalAvailableQty <= detail.ReorderLevel)
                detail.OverallStockStatus = "Low Stock";
            else
                detail.OverallStockStatus = "In Stock";

            // 3. Fetch Recent Movements (Last 5)
            var movementSql = @"
                SELECT TOP 5
                    MovementID,
                    MovementDate,
                    SKU,
                    ProductName,
                    WarehouseName,
                    ShelfCode,
                    MovementType,
                    Quantity,
                    BeforeQty,
                    AfterQty,
                    ISNULL(ReferenceType, '') AS ReferenceType,
                    ISNULL(ReferenceNo, '') AS ReferenceNo,
                    ISNULL(PerformedBy, '') AS PerformedBy,
                    ISNULL(Remarks, '') AS Remarks
                FROM dbo.vw_StockMovementReport
                WHERE SKU = @SKU
                ORDER BY MovementDate DESC";

            try
            {
                using var cmd = new SqlCommand(movementSql, connection);
                cmd.Parameters.AddWithValue("@SKU", detail.SKU);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    detail.RecentMovements.Add(new StockMovementDto
                    {
                        MovementID = reader.GetInt32("MovementID"),
                        MovementDate = reader.GetDateTime("MovementDate"),
                        SKU = reader.GetString("SKU"),
                        ProductName = reader.GetString("ProductName"),
                        WarehouseName = reader.GetString("WarehouseName"),
                        ShelfCode = reader.GetString("ShelfCode"),
                        MovementType = reader.GetString("MovementType"),
                        Quantity = reader.GetDecimal("Quantity"),
                        BeforeQty = reader.GetDecimal("BeforeQty"),
                        AfterQty = reader.GetDecimal("AfterQty"),
                        ReferenceType = reader.GetString("ReferenceType"),
                        ReferenceNo = reader.GetString("ReferenceNo"),
                        PerformedBy = reader.GetString("PerformedBy"),
                        Remarks = reader.GetString("Remarks")
                    });
                }
            }
            catch
            {
                // Softly ignore if view schema differs in edge environments
            }

            return detail;
        }
    }
}
