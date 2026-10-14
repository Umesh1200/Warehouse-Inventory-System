using IMS.Models.DTOs;
using IMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IMS.Pages.Purchasing;

public class PurchaseOrderPrintModel : PageModel
{
    private readonly IProcurementWorkflowService _service;
    public PurchaseOrderPrintModel(IProcurementWorkflowService service) => _service = service;
    public ProcurementOrderWorkflowDto Order { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var order = await _service.GetOrderAsync(id);
        if (order == null) return NotFound();
        Order = order; return Page();
    }
}
