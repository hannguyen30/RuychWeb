using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;

namespace RuychWeb.Areas.Admin.Models
{
    public class ReceiptViewModel
    {
        public int ReceiptId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int SupplierId { get; set; }
        public int EmployeeId { get; set; }

        public string? SupplierName { get; set; }
        public string? SupplierPhone { get; set; }
        public string? EmployeeName { get; set; }

        public List<Supplier>? Suppliers { get; set; }
        public List<Employee>? Employees { get; set; }

        public List<ReceiptItem> ReceiptItems { get; set; } = new List<ReceiptItem>();

        public decimal TotalAmount => ReceiptItems?.Sum(item => item.Quantity * item.Price) ?? 0;

        public class ReceiptItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal TotalPrice => Quantity * Price;
        }
    }
}
