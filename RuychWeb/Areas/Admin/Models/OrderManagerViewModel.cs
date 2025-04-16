using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;

namespace RuychWeb.Areas.Admin.Models
{
    public class OrderManagerViewModel
    {
        // Order properties (from Order class)
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
        public string CarrierName { get; set; }
        public decimal TotalFee { get; set; }
        public int? CustomerId { get; set; }
        public int EmployeeId { get; set; }

        // OrderDetails (from OrderDetail class)
        public List<OrderDetail> OrderDetails { get; set; }

        // Geographical Information (Ward, District, Province)
        public string Ward { get; set; }
        public string District { get; set; }
        public string Province { get; set; }

        // Customer and Employee related data (optional for additional details)
        public Customer Customer { get; set; }
        public Employee Employee { get; set; }

        public decimal? Discount { get; set; }
    }
}
