namespace RuychWeb.Areas.Admin.Models
{
    public class OrderCreateViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "COD";
        public string OrderStatus { get; set; } = "Chờ xác nhận";
        public string PaymentStatus { get; set; } = "Chưa thanh toán";
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();

        public class OrderItem
        {
            public int ProductDetailId { get; set; }
            public string ProductName { get; set; } = string.Empty; // Tên sản phẩm
            public string Color { get; set; } = string.Empty;    // Màu sắc
            public string Size { get; set; } = string.Empty;     // Kích thước
            public int Quantity { get; set; }                    // Số lượng
            public decimal Price { get; set; }                   // Giá sản phẩm
            public decimal Discount { get; set; }                // Giảm giá
        }
    }
}

