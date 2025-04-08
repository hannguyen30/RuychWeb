using RuychWeb.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class Receipt
    {
        [Key]
        public int ReceiptId { get; set; }
        public DateTime CreatedDate { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public ICollection<ReceiptDetail> ReceiptDetails { get; set; }
    }
}
