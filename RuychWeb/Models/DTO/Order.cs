using RuychWeb.Models.Domain;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace RuychWeb.Models.DTO
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }
        public string? CancelReason { get; set; }
        public string? CarrierName { get; set; }
        public decimal TotalFee { get; set; }
        [AllowNull]
        public int? CustomerId { get; set; }
        public Customer Customer { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
