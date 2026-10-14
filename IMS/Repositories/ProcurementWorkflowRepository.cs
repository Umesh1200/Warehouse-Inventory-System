using IMS.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace IMS.Repositories;

public class ProcurementWorkflowRepository : IProcurementWorkflowRepository
{
    private readonly string _connectionString;

    public ProcurementWorkflowRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<ProcurementOrderWorkflowDto>> GetOrdersAsync()
    {
        var rows = new List<ProcurementOrderWorkflowDto>();
        const string sql = @"
            SELECT po.POID, po.PONumber, po.Status, po.OrderDate, po.ExpectedDate, po.TotalAmount, po.Remarks,
                   s.SupplierName, s.ContactPerson, s.Email, s.Phone, s.Address,
                   ISNULL(sc.ConfirmationStatus, 'Pending') ConfirmationStatus,
                   sc.ConfirmedAt, sc.PromisedDeliveryDate,
                   ISNULL(rv.ReceivedValue, 0) ReceivedValue, ISNULL(rv.AcceptedValue, 0) AcceptedValue
            FROM PurchaseOrders po
            JOIN Suppliers s ON s.SupplierID = po.SupplierID
            OUTER APPLY (
                SELECT TOP 1 ConfirmationStatus, ConfirmedAt, PromisedDeliveryDate
                FROM SupplierConfirmations c WHERE c.POID = po.POID
                ORDER BY c.ConfirmedAt DESC, c.ConfirmationID DESC
            ) sc
            OUTER APPLY (
                SELECT SUM(ri.ReceivedQty * poi.UnitPrice) ReceivedValue,
                       SUM(ISNULL(q.AcceptedQty, 0) * poi.UnitPrice) AcceptedValue
                FROM PurchaseOrderItems poi
                LEFT JOIN ReceivingItems ri ON ri.POItemID = poi.POItemID
                OUTER APPLY (SELECT SUM(qc.AcceptedQty) AcceptedQty FROM QCChecks qc WHERE qc.ReceiptID = ri.ReceiptID) q
                WHERE poi.POID = po.POID
            ) rv
            ORDER BY po.OrderDate DESC, po.POID DESC;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add(MapOrder(reader));
        return rows;
    }

    public async Task<ProcurementOrderWorkflowDto?> GetOrderAsync(int poId)
    {
        ProcurementOrderWorkflowDto? order = null;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string headerSql = @"
            SELECT po.POID, po.PONumber, po.Status, po.OrderDate, po.ExpectedDate, po.TotalAmount, po.Remarks,
                   s.SupplierName, s.ContactPerson, s.Email, s.Phone, s.Address,
                   ISNULL(sc.ConfirmationStatus, 'Pending') ConfirmationStatus,
                   sc.ConfirmedAt, sc.PromisedDeliveryDate,
                   ISNULL(rv.ReceivedValue, 0) ReceivedValue, ISNULL(rv.AcceptedValue, 0) AcceptedValue
            FROM PurchaseOrders po JOIN Suppliers s ON s.SupplierID = po.SupplierID
            OUTER APPLY (SELECT TOP 1 ConfirmationStatus, ConfirmedAt, PromisedDeliveryDate FROM SupplierConfirmations c WHERE c.POID = po.POID ORDER BY c.ConfirmedAt DESC, c.ConfirmationID DESC) sc
            OUTER APPLY (
                SELECT SUM(ri.ReceivedQty * poi.UnitPrice) ReceivedValue, SUM(ISNULL(q.AcceptedQty, 0) * poi.UnitPrice) AcceptedValue
                FROM PurchaseOrderItems poi LEFT JOIN ReceivingItems ri ON ri.POItemID = poi.POItemID
                OUTER APPLY (SELECT SUM(qc.AcceptedQty) AcceptedQty FROM QCChecks qc WHERE qc.ReceiptID = ri.ReceiptID) q
                WHERE poi.POID = po.POID
            ) rv WHERE po.POID = @POID;";
        await using (var command = new SqlCommand(headerSql, connection))
        {
            command.Parameters.AddWithValue("@POID", poId);
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) order = MapOrder(reader);
        }
        if (order == null) return null;

        const string itemSql = @"
            SELECT poi.POItemID, poi.POID, poi.ProductID, p.SKU, p.ProductName, poi.OrderedQty, poi.ReceivedQty,
                   poi.UnitPrice, poi.TotalPrice
            FROM PurchaseOrderItems poi JOIN Products p ON p.ProductID = poi.ProductID
            WHERE poi.POID = @POID ORDER BY poi.POItemID;";
        await using (var command = new SqlCommand(itemSql, connection))
        {
            command.Parameters.AddWithValue("@POID", poId);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                order.Items.Add(new PurchaseOrderItemDto
                {
                    POItemID = reader.GetInt32(reader.GetOrdinal("POItemID")), POID = poId,
                    ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")), SKU = reader.GetString(reader.GetOrdinal("SKU")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")), OrderedQty = reader.GetDecimal(reader.GetOrdinal("OrderedQty")),
                    ReceivedQty = reader.GetDecimal(reader.GetOrdinal("ReceivedQty")), UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("TotalPrice"))
                });
            }
        }

        const string activitySql = @"
            SELECT c.CommunicationID, c.POID, c.Channel, c.Direction, c.ContactDetail, c.CommunicatedAt, c.Notes, c.RecordedBy, u.FullName
            FROM PurchaseOrderCommunications c JOIN Users u ON u.UserID = c.RecordedBy WHERE c.POID = @POID ORDER BY c.CommunicatedAt DESC;
            SELECT c.ConfirmationID, c.POID, c.ConfirmationStatus, c.SupplierContactName, c.ConfirmedAt, c.PromisedDeliveryDate, c.Notes, c.RecordedBy, u.FullName
            FROM SupplierConfirmations c JOIN Users u ON u.UserID = c.RecordedBy WHERE c.POID = @POID ORDER BY c.ConfirmedAt DESC;
            SELECT d.DocumentID, d.POID, d.ReceiptID, d.DocumentType, d.OriginalFileName, d.StoredFileName, d.ContentType, d.FileSize, d.Notes, d.UploadedBy, u.FullName, d.UploadedAt
            FROM ProcurementDocuments d JOIN Users u ON u.UserID = d.UploadedBy WHERE d.POID = @POID ORDER BY d.UploadedAt DESC;";
        await using (var command = new SqlCommand(activitySql, connection))
        {
            command.Parameters.AddWithValue("@POID", poId);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) order.Communications.Add(MapCommunication(reader));
            await reader.NextResultAsync();
            while (await reader.ReadAsync()) order.Confirmations.Add(MapConfirmation(reader));
            await reader.NextResultAsync();
            while (await reader.ReadAsync()) order.Documents.Add(MapDocument(reader));
        }
        return order;
    }

    public async Task<int> AddCommunicationAsync(POCommunicationDto value)
    {
        const string sql = @"INSERT INTO PurchaseOrderCommunications (POID, Channel, Direction, ContactDetail, CommunicatedAt, Notes, RecordedBy)
            OUTPUT INSERTED.CommunicationID VALUES (@POID, @Channel, @Direction, @Contact, @At, @Notes, @User);";
        return await ExecuteInsertAsync(sql, command =>
        {
            command.Parameters.AddWithValue("@POID", value.POID); command.Parameters.AddWithValue("@Channel", value.Channel);
            command.Parameters.AddWithValue("@Direction", value.Direction); command.Parameters.AddWithValue("@Contact", Db(value.ContactDetail));
            command.Parameters.AddWithValue("@At", value.CommunicatedAt); command.Parameters.AddWithValue("@Notes", Db(value.Notes));
            command.Parameters.AddWithValue("@User", value.RecordedBy);
        });
    }

    public async Task<int> AddConfirmationAsync(SupplierConfirmationDto value)
    {
        const string sql = @"INSERT INTO SupplierConfirmations (POID, ConfirmationStatus, SupplierContactName, ConfirmedAt, PromisedDeliveryDate, Notes, RecordedBy)
            OUTPUT INSERTED.ConfirmationID VALUES (@POID, @Status, @Contact, @At, @Delivery, @Notes, @User);";
        return await ExecuteInsertAsync(sql, command =>
        {
            command.Parameters.AddWithValue("@POID", value.POID); command.Parameters.AddWithValue("@Status", value.ConfirmationStatus);
            command.Parameters.AddWithValue("@Contact", Db(value.SupplierContactName)); command.Parameters.AddWithValue("@At", value.ConfirmedAt);
            command.Parameters.AddWithValue("@Delivery", value.PromisedDeliveryDate.HasValue ? value.PromisedDeliveryDate.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Notes", Db(value.Notes)); command.Parameters.AddWithValue("@User", value.RecordedBy);
        });
    }

    public async Task<int> AddDocumentAsync(ProcurementDocumentDto value)
    {
        const string sql = @"INSERT INTO ProcurementDocuments (POID, ReceiptID, DocumentType, OriginalFileName, StoredFileName, ContentType, FileSize, Notes, UploadedBy)
            OUTPUT INSERTED.DocumentID VALUES (@POID, @Receipt, @Type, @Original, @Stored, @Content, @Size, @Notes, @User);";
        return await ExecuteInsertAsync(sql, command =>
        {
            command.Parameters.AddWithValue("@POID", value.POID); command.Parameters.AddWithValue("@Receipt", value.ReceiptID.HasValue ? value.ReceiptID.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Type", value.DocumentType); command.Parameters.AddWithValue("@Original", value.OriginalFileName);
            command.Parameters.AddWithValue("@Stored", value.StoredFileName); command.Parameters.AddWithValue("@Content", Db(value.ContentType));
            command.Parameters.AddWithValue("@Size", value.FileSize); command.Parameters.AddWithValue("@Notes", Db(value.Notes)); command.Parameters.AddWithValue("@User", value.UploadedBy);
        });
    }

    public async Task<ProcurementDocumentDto?> GetDocumentAsync(int documentId)
    {
        const string sql = @"SELECT d.DocumentID, d.POID, d.ReceiptID, d.DocumentType, d.OriginalFileName, d.StoredFileName, d.ContentType, d.FileSize, d.Notes, d.UploadedBy, u.FullName, d.UploadedAt
            FROM ProcurementDocuments d JOIN Users u ON u.UserID = d.UploadedBy WHERE d.DocumentID = @ID;";
        await using var connection = new SqlConnection(_connectionString); await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ID", documentId); await connection.OpenAsync(); await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapDocument(reader) : null;
    }

    public async Task<IReadOnlyList<SupplierInvoiceDto>> GetInvoicesAsync()
    {
        var rows = new List<SupplierInvoiceDto>();
        const string sql = @"
            SELECT i.InvoiceID, i.POID, po.PONumber, s.SupplierName, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                   i.Subtotal, i.TaxAmount, i.TotalAmount, po.TotalAmount POAmount, i.Status, i.Notes,
                   ISNULL(pay.PaidAmount, 0) PaidAmount, ISNULL(rv.ReceivedValue, 0) ReceivedValue, ISNULL(rv.AcceptedValue, 0) AcceptedValue
            FROM SupplierInvoices i JOIN PurchaseOrders po ON po.POID = i.POID JOIN Suppliers s ON s.SupplierID = po.SupplierID
            OUTER APPLY (SELECT SUM(Amount) PaidAmount FROM SupplierPayments p WHERE p.InvoiceID = i.InvoiceID) pay
            OUTER APPLY (
                SELECT SUM(ri.ReceivedQty * poi.UnitPrice) ReceivedValue, SUM(ISNULL(q.AcceptedQty, 0) * poi.UnitPrice) AcceptedValue
                FROM PurchaseOrderItems poi LEFT JOIN ReceivingItems ri ON ri.POItemID = poi.POItemID
                OUTER APPLY (SELECT SUM(qc.AcceptedQty) AcceptedQty FROM QCChecks qc WHERE qc.ReceiptID = ri.ReceiptID) q WHERE poi.POID = po.POID
            ) rv ORDER BY i.InvoiceDate DESC, i.InvoiceID DESC;";
        await using var connection = new SqlConnection(_connectionString); await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync(); await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var invoice = new SupplierInvoiceDto
            {
                InvoiceID = reader.GetInt32(reader.GetOrdinal("InvoiceID")), POID = reader.GetInt32(reader.GetOrdinal("POID")),
                PONumber = reader.GetString(reader.GetOrdinal("PONumber")), SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                InvoiceNumber = reader.GetString(reader.GetOrdinal("InvoiceNumber")), InvoiceDate = reader.GetDateTime(reader.GetOrdinal("InvoiceDate")),
                DueDate = DateOrNull(reader, "DueDate"), Subtotal = reader.GetDecimal(reader.GetOrdinal("Subtotal")), TaxAmount = reader.GetDecimal(reader.GetOrdinal("TaxAmount")),
                TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")), POAmount = reader.GetDecimal(reader.GetOrdinal("POAmount")),
                ReceivedValue = reader.GetDecimal(reader.GetOrdinal("ReceivedValue")), AcceptedValue = reader.GetDecimal(reader.GetOrdinal("AcceptedValue")),
                PaidAmount = reader.GetDecimal(reader.GetOrdinal("PaidAmount")), Status = reader.GetString(reader.GetOrdinal("Status")), Notes = StringOrEmpty(reader, "Notes")
            };
            invoice.MatchStatus = GetMatchStatus(invoice);
            rows.Add(invoice);
        }
        return rows;
    }

    public async Task<int> CreateInvoiceAsync(SupplierInvoiceCreateDto value)
    {
        const string sql = @"INSERT INTO SupplierInvoices (POID, InvoiceNumber, InvoiceDate, DueDate, Subtotal, TaxAmount, TotalAmount, Notes, RecordedBy)
            OUTPUT INSERTED.InvoiceID VALUES (@POID, @Number, @Date, @Due, @Subtotal, @Tax, @Total, @Notes, @User);";
        return await ExecuteInsertAsync(sql, command =>
        {
            command.Parameters.AddWithValue("@POID", value.POID); command.Parameters.AddWithValue("@Number", value.InvoiceNumber);
            command.Parameters.AddWithValue("@Date", value.InvoiceDate); command.Parameters.AddWithValue("@Due", value.DueDate.HasValue ? value.DueDate.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Subtotal", value.Subtotal); command.Parameters.AddWithValue("@Tax", value.TaxAmount);
            command.Parameters.AddWithValue("@Total", value.TotalAmount); command.Parameters.AddWithValue("@Notes", Db(value.Notes)); command.Parameters.AddWithValue("@User", value.RecordedBy);
        });
    }

    public async Task<bool> SetInvoiceStatusAsync(int invoiceId, string status, int userId)
    {
        const string sql = @"UPDATE SupplierInvoices SET Status = @Status, ApprovedBy = CASE WHEN @Status = 'Approved' THEN @User ELSE ApprovedBy END,
            ApprovedAt = CASE WHEN @Status = 'Approved' THEN SYSDATETIME() ELSE ApprovedAt END WHERE InvoiceID = @ID;";
        await using var connection = new SqlConnection(_connectionString); await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ID", invoiceId); command.Parameters.AddWithValue("@Status", status); command.Parameters.AddWithValue("@User", userId);
        await connection.OpenAsync(); return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<int> AddPaymentAsync(SupplierPaymentCreateDto value)
    {
        await using var connection = new SqlConnection(_connectionString); await connection.OpenAsync(); await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            const string insertSql = @"INSERT INTO SupplierPayments (InvoiceID, PaymentDate, Amount, PaymentMethod, ReferenceNumber, Notes, RecordedBy)
                OUTPUT INSERTED.PaymentID VALUES (@Invoice, @Date, @Amount, @Method, @Reference, @Notes, @User);";
            int id;
            await using (var command = new SqlCommand(insertSql, connection, (SqlTransaction)transaction))
            {
                command.Parameters.AddWithValue("@Invoice", value.InvoiceID); command.Parameters.AddWithValue("@Date", value.PaymentDate);
                command.Parameters.AddWithValue("@Amount", value.Amount); command.Parameters.AddWithValue("@Method", value.PaymentMethod);
                command.Parameters.AddWithValue("@Reference", Db(value.ReferenceNumber)); command.Parameters.AddWithValue("@Notes", Db(value.Notes)); command.Parameters.AddWithValue("@User", value.RecordedBy);
                id = Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            const string updateSql = @"UPDATE i SET Status = CASE WHEN x.Paid >= i.TotalAmount THEN 'Paid' ELSE 'Partially Paid' END
                FROM SupplierInvoices i CROSS APPLY (SELECT SUM(p.Amount) Paid FROM SupplierPayments p WHERE p.InvoiceID = i.InvoiceID) x WHERE i.InvoiceID = @Invoice;";
            await using (var command = new SqlCommand(updateSql, connection, (SqlTransaction)transaction))
            { command.Parameters.AddWithValue("@Invoice", value.InvoiceID); await command.ExecuteNonQueryAsync(); }
            await transaction.CommitAsync(); return id;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    private async Task<int> ExecuteInsertAsync(string sql, Action<SqlCommand> bind)
    {
        await using var connection = new SqlConnection(_connectionString); await using var command = new SqlCommand(sql, connection);
        bind(command); await connection.OpenAsync(); return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static ProcurementOrderWorkflowDto MapOrder(SqlDataReader reader) => new()
    {
        POID = reader.GetInt32(reader.GetOrdinal("POID")), PONumber = reader.GetString(reader.GetOrdinal("PONumber")),
        POStatus = reader.GetString(reader.GetOrdinal("Status")), OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
        ExpectedDate = DateOrNull(reader, "ExpectedDate"), TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")), Remarks = StringOrEmpty(reader, "Remarks"),
        SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")), ContactPerson = StringOrEmpty(reader, "ContactPerson"),
        SupplierEmail = StringOrEmpty(reader, "Email"), SupplierPhone = StringOrEmpty(reader, "Phone"), SupplierAddress = StringOrEmpty(reader, "Address"),
        SupplierConfirmationStatus = reader.GetString(reader.GetOrdinal("ConfirmationStatus")), SupplierConfirmedAt = DateOrNull(reader, "ConfirmedAt"),
        PromisedDeliveryDate = DateOrNull(reader, "PromisedDeliveryDate"), ReceivedValue = reader.GetDecimal(reader.GetOrdinal("ReceivedValue")),
        AcceptedValue = reader.GetDecimal(reader.GetOrdinal("AcceptedValue"))
    };

    private static POCommunicationDto MapCommunication(SqlDataReader r) => new()
    {
        CommunicationID = r.GetInt32(r.GetOrdinal("CommunicationID")), POID = r.GetInt32(r.GetOrdinal("POID")), Channel = r.GetString(r.GetOrdinal("Channel")),
        Direction = r.GetString(r.GetOrdinal("Direction")), ContactDetail = StringOrEmpty(r, "ContactDetail"), CommunicatedAt = r.GetDateTime(r.GetOrdinal("CommunicatedAt")),
        Notes = StringOrEmpty(r, "Notes"), RecordedBy = r.GetInt32(r.GetOrdinal("RecordedBy")), RecordedByName = r.GetString(r.GetOrdinal("FullName"))
    };

    private static SupplierConfirmationDto MapConfirmation(SqlDataReader r) => new()
    {
        ConfirmationID = r.GetInt32(r.GetOrdinal("ConfirmationID")), POID = r.GetInt32(r.GetOrdinal("POID")), ConfirmationStatus = r.GetString(r.GetOrdinal("ConfirmationStatus")),
        SupplierContactName = StringOrEmpty(r, "SupplierContactName"), ConfirmedAt = r.GetDateTime(r.GetOrdinal("ConfirmedAt")), PromisedDeliveryDate = DateOrNull(r, "PromisedDeliveryDate"),
        Notes = StringOrEmpty(r, "Notes"), RecordedBy = r.GetInt32(r.GetOrdinal("RecordedBy")), RecordedByName = r.GetString(r.GetOrdinal("FullName"))
    };

    private static ProcurementDocumentDto MapDocument(SqlDataReader r) => new()
    {
        DocumentID = r.GetInt32(r.GetOrdinal("DocumentID")), POID = r.GetInt32(r.GetOrdinal("POID")), ReceiptID = IntOrNull(r, "ReceiptID"),
        DocumentType = r.GetString(r.GetOrdinal("DocumentType")), OriginalFileName = r.GetString(r.GetOrdinal("OriginalFileName")), StoredFileName = r.GetString(r.GetOrdinal("StoredFileName")),
        ContentType = StringOrEmpty(r, "ContentType"), FileSize = r.GetInt64(r.GetOrdinal("FileSize")), Notes = StringOrEmpty(r, "Notes"),
        UploadedBy = r.GetInt32(r.GetOrdinal("UploadedBy")), UploadedByName = r.GetString(r.GetOrdinal("FullName")), UploadedAt = r.GetDateTime(r.GetOrdinal("UploadedAt"))
    };

    private static string GetMatchStatus(SupplierInvoiceDto i)
    {
        if (i.AcceptedValue <= 0) return "Awaiting QC";
        var tolerance = Math.Max(1m, i.TotalAmount * 0.005m);
        if (Math.Abs(i.TotalAmount - i.POAmount) <= tolerance && i.AcceptedValue + tolerance >= i.Subtotal) return "Matched";
        return "Variance";
    }

    private static object Db(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    private static string StringOrEmpty(SqlDataReader r, string name) => r.IsDBNull(r.GetOrdinal(name)) ? string.Empty : r.GetString(r.GetOrdinal(name));
    private static DateTime? DateOrNull(SqlDataReader r, string name) => r.IsDBNull(r.GetOrdinal(name)) ? null : r.GetDateTime(r.GetOrdinal(name));
    private static int? IntOrNull(SqlDataReader r, string name) => r.IsDBNull(r.GetOrdinal(name)) ? null : r.GetInt32(r.GetOrdinal(name));
}
