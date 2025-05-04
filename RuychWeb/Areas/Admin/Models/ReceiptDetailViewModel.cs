namespace RuychWeb.Areas.Admin.Models
{
    public class ReceiptDetailViewModel
    {
        public int ReceiptId { get; set; }
        public string EmployeeName { get; set; }
        public string SupplierName { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<ReceiptItem> ReceiptItems { get; set; }

        public class ReceiptItem
        {
            public string ProductName { get; set; }
            public string Size { get; set; }
            public string Color { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal TotalPrice => Quantity * Price;
        }
    }
}
