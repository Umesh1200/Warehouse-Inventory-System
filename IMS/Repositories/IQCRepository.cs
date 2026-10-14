using IMS.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMS.Repositories
{
    public interface IQCRepository
    {
        Task<IEnumerable<QCCheckDto>> GetQCTasksAsync();
        Task<int> SubmitQCResultAsync(QCSubmitDto qc);
        Task<ReceivingItemForQCDto?> GetReceivingItemForQCAsync(int receiptId);
        Task<ReceivingItemForQCDto?> GetFGReceiptForQCAsync(int fgReceiptId);
        Task<bool> HasQCCheckAsync(int receiptId);
        Task<bool> HasFGQCCheckAsync(int fgReceiptId);
    }
}
