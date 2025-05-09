namespace RuychWeb.Areas.Admin.Models
{
    public class OrderPdfViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string EmployeeName { get; set; }  // Thêm thông tin người bán
        public DateTime? CreatedDate { get; set; }  // Ngày tạo
        public List<OrderDetailPdfViewModel> OrderDetails { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OrderDetailPdfViewModel
    {
        public int Index { get; set; }
        public int ProductCode { get; set; } // Mã sản phẩm
        public string ProductName { get; set; } // Tên sản phẩm
        public string Color { get; set; } // Màu sắc
        public string Size { get; set; } // Kích thước
        public int Quantity { get; set; } // Số lượng
        public decimal UnitPrice { get; set; } // Đơn giá
        public decimal TotalPrice { get; set; } // Thành tiền
    }
}
