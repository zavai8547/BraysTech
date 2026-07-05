using System.ComponentModel.DataAnnotations;

namespace BraysTech.Models
{
    public enum TransferStatus
    {
        Pending = 0,
        InTransit = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class StockTransfer
    {
        [Key]
        public int TransferID { get; set; }

        public int FromBranchID { get; set; }
        public Branch? FromBranch { get; set; }

        public int ToBranchID { get; set; }
        public Branch? ToBranch { get; set; }

        public string InitiatedByID { get; set; }
            = string.Empty;
        public AppUser? InitiatedBy { get; set; }

        public string? ReceivedByID { get; set; }
        public AppUser? ReceivedBy { get; set; }

        public TransferStatus Status { get; set; }
            = TransferStatus.Pending;

        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        public ICollection<StockTransferItem> Items
        { get; set; }
            = new List<StockTransferItem>();
    }

    public class StockTransferItem
    {
        [Key]
        public int TransferItemID { get; set; }

        public int TransferID { get; set; }
        public StockTransfer? Transfer { get; set; }

        public int StockID { get; set; }
        public IMEIStock? Phone { get; set; }

        public string IMEI { get; set; }
            = string.Empty;
        public string PhoneName { get; set; }
            = string.Empty;
    }
}