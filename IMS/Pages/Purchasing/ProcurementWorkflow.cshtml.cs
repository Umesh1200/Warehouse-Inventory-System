using IMS.Models.DTOs;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Pages.Purchasing
{
    public class ProcurementWorkflowModel : PageModel
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx", ".xls", ".xlsx" };
        private readonly IProcurementWorkflowService _service;
        private readonly IUserContext _userContext;
        private readonly IWebHostEnvironment _environment;

        public ProcurementWorkflowModel(IProcurementWorkflowService service, IUserContext userContext, IWebHostEnvironment environment)
        {
            _service = service;
            _userContext = userContext;
            _environment = environment;
        }

        public IReadOnlyList<ProcurementOrderWorkflowDto> Orders { get; set; } = [];
        public ProcurementOrderWorkflowDto? SelectedOrder { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public List<string> Suppliers { get; set; } = [];
        public List<string> POStatuses { get; set; } = [];
        public List<string> ConfirmationStatuses { get; set; } = [];

        [BindProperty] public POCommunicationDto Communication { get; set; } = new();
        [BindProperty] public SupplierConfirmationDto Confirmation { get; set; } = new();
        [BindProperty] public int DocumentPOID { get; set; }
        [BindProperty] public string DocumentType { get; set; } = "Supplier Confirmation";
        [BindProperty] public string DocumentNotes { get; set; } = string.Empty;
        [BindProperty] public IFormFile? UploadedDocument { get; set; }

        public async Task OnGetAsync(int? poid) => await LoadAsync(poid);

        public async Task<IActionResult> OnPostCommunicationAsync()
        {
            Communication.RecordedBy = _userContext.CurrentUserID;
            var result = await _service.RecordCommunicationAsync(Communication);
            return await CompleteAsync(result, Communication.POID);
        }

        public async Task<IActionResult> OnPostConfirmationAsync()
        {
            Confirmation.RecordedBy = _userContext.CurrentUserID;
            var result = await _service.RecordConfirmationAsync(Confirmation);
            return await CompleteAsync(result, Confirmation.POID);
        }

        public async Task<IActionResult> OnPostDocumentAsync()
        {
            if (UploadedDocument == null || UploadedDocument.Length == 0)
                return await CompleteAsync(new() { Success = false, Message = "Choose a document to upload." }, DocumentPOID);
            if (UploadedDocument.Length > 10 * 1024 * 1024)
                return await CompleteAsync(new() { Success = false, Message = "Document must be 10 MB or smaller." }, DocumentPOID);

            var extension = Path.GetExtension(UploadedDocument.FileName);
            if (!AllowedExtensions.Contains(extension))
                return await CompleteAsync(new() { Success = false, Message = "Allowed formats: PDF, image, Word, and Excel." }, DocumentPOID);

            var directory = Path.Combine(_environment.ContentRootPath, "App_Data", "ProcurementDocuments");
            Directory.CreateDirectory(directory);
            var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var path = Path.Combine(directory, storedName);
            await using (var stream = System.IO.File.Create(path)) await UploadedDocument.CopyToAsync(stream);

            var result = await _service.AddDocumentAsync(new ProcurementDocumentDto
            {
                POID = DocumentPOID,
                DocumentType = DocumentType,
                OriginalFileName = Path.GetFileName(UploadedDocument.FileName),
                StoredFileName = storedName,
                ContentType = UploadedDocument.ContentType,
                FileSize = UploadedDocument.Length,
                Notes = DocumentNotes,
                UploadedBy = _userContext.CurrentUserID
            });
            if (!result.Success) System.IO.File.Delete(path);
            return await CompleteAsync(result, DocumentPOID);
        }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var document = await _service.GetDocumentAsync(id);
            if (document == null) return NotFound();
            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "ProcurementDocuments", Path.GetFileName(document.StoredFileName));
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType, document.OriginalFileName);
        }

        private async Task<IActionResult> CompleteAsync(ProcurementActionResultDto result, int poId)
        {
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage(new { poid = poId });
            }
            ErrorMessage = result.Message;
            await LoadAsync(poId);
            return Page();
        }

        private async Task LoadAsync(int? poId)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? string.Empty;
            try
            {
                Orders = await _service.GetOrdersAsync();

                Suppliers = ["All Suppliers"];
                Suppliers.AddRange(Orders.Select(o => o.SupplierName).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s));

                POStatuses = ["All PO Statuses", "Draft", "Pending Approval", "Confirmed", "InTransit", "Received", "QCCheck", "Putaway", "Closed", "Rejected", "Cancelled"];
                ConfirmationStatuses = ["All Responses", "Confirmed", "Pending", "Changes Requested", "Declined"];

                var selectedId = poId ?? Orders.FirstOrDefault()?.POID;
                if (selectedId.HasValue)
                {
                    SelectedOrder = await _service.GetOrderAsync(selectedId.Value);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Procurement workflow is unavailable. {ex.Message}";
            }
        }
    }
}
