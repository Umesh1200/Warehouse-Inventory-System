using IMS.Models.DTOs;
using IMS.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Services
{
    public class QCService : IQCService
    {
        private readonly IQCRepository _qcRepository;

        public QCService(IQCRepository qcRepository)
        {
            _qcRepository = qcRepository;
        }

        public async Task<IEnumerable<QCCheckDto>> GetQCTasksAsync()
        {
            return await _qcRepository.GetQCTasksAsync();
        }

        public async Task<QCCheckSummaryDto> GetQCSummaryAsync()
        {
            var tasks = await _qcRepository.GetQCTasksAsync();
            var taskList = tasks.ToList();

            return new QCCheckSummaryDto
            {
                TotalQCItems = taskList.Count,
                PendingQCCount = taskList.Count(t => t.Status == "Pending" || t.Status == "QC Required"),
                TotalAcceptedQty = taskList.Sum(t => t.AcceptedQty),
                TotalRejectedDamagedQty = taskList.Sum(t => t.RejectedQty + t.DamagedQty)
            };
        }

        public async Task<QCActionResultDto> SubmitQCResultAsync(QCSubmitDto qc)
        {
            if (qc.AcceptedQty < 0) return new QCActionResultDto { Success = false, Message = "Accepted quantity cannot be negative." };
            if (qc.RejectedQty < 0) return new QCActionResultDto { Success = false, Message = "Rejected quantity cannot be negative." };
            if (qc.DamagedQty < 0) return new QCActionResultDto { Success = false, Message = "Damaged quantity cannot be negative." };

            if (qc.AcceptedQty == 0 && qc.RejectedQty == 0 && qc.DamagedQty == 0)
            {
                return new QCActionResultDto { Success = false, Message = "At least one quantity (Accepted/Rejected/Damaged) must be greater than zero." };
            }

            if (qc.ItemSource == "Production" || (qc.FGReceiptID.HasValue && qc.FGReceiptID > 0))
            {
                if (!qc.FGReceiptID.HasValue || qc.FGReceiptID.Value <= 0)
                {
                    return new QCActionResultDto { Success = false, Message = "Invalid Finished Goods Receipt ID." };
                }

                var fgItem = await _qcRepository.GetFGReceiptForQCAsync(qc.FGReceiptID.Value);
                if (fgItem == null) return new QCActionResultDto { Success = false, Message = "Finished Goods Receipt not found." };

                if ((qc.AcceptedQty + qc.RejectedQty + qc.DamagedQty) != fgItem.ReceivedQty)
                {
                    return new QCActionResultDto { Success = false, Message = $"Total quantities ({qc.AcceptedQty + qc.RejectedQty + qc.DamagedQty}) must exactly equal the Produced Quantity ({fgItem.ReceivedQty})." };
                }

                if (await _qcRepository.HasFGQCCheckAsync(qc.FGReceiptID.Value))
                {
                    return new QCActionResultDto { Success = false, Message = "A QC check has already been submitted for this finished goods receipt." };
                }
            }
            else
            {
                if (!qc.ReceiptID.HasValue || qc.ReceiptID.Value <= 0)
                {
                    return new QCActionResultDto { Success = false, Message = "Invalid Purchase Receipt ID." };
                }

                var receiptItem = await _qcRepository.GetReceivingItemForQCAsync(qc.ReceiptID.Value);
                if (receiptItem == null) return new QCActionResultDto { Success = false, Message = "Receipt not found." };
                if (receiptItem.Status != "QC Required") return new QCActionResultDto { Success = false, Message = "This receipt does not require QC." };

                if ((qc.AcceptedQty + qc.RejectedQty + qc.DamagedQty) != receiptItem.ReceivedQty)
                {
                    return new QCActionResultDto { Success = false, Message = $"Total quantities ({qc.AcceptedQty + qc.RejectedQty + qc.DamagedQty}) must exactly equal the Received Quantity ({receiptItem.ReceivedQty})." };
                }

                if (await _qcRepository.HasQCCheckAsync(qc.ReceiptID.Value))
                {
                    return new QCActionResultDto { Success = false, Message = "A QC check has already been submitted for this receipt." };
                }
            }

            try
            {
                int newId = await _qcRepository.SubmitQCResultAsync(qc);
                return new QCActionResultDto { Success = true, Message = "QC Result submitted successfully.", NewID = newId };
            }
            catch (Exception ex)
            {
                return new QCActionResultDto { Success = false, Message = ex.Message };
            }
        }
    }
}
