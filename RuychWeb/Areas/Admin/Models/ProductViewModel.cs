namespace RuychWeb.Areas.Admin.Models
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Thumbnail { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; } // Dùng để chọn Category
        public string CategoryName { get; set; } // Tên danh mục cho hiển thị
        public List<int> SaleIds { get; set; } // Các SaleIds người dùng chọn
        public List<SaleViewModel> Sales { get; set; } // Các thông tin giảm giá của sản phẩm
        public List<ColorViewModel> Colors { get; set; } // Màu sắc và các chi tiết size, quantity

        // Tính giá giảm
        public decimal DiscountedPrice
        {
            get
            {
                if (Sales != null && Sales.Any())
                {
                    var discount = Sales.First().Discount; // Lấy mức giảm giá đầu tiên
                    if (discount < 100)
                    {
                        // Giảm giá theo phần trăm
                        return Price - (Price * discount / 100);
                    }
                    else if (discount > 1000)
                    {
                        // Giảm giá theo số tiền cố định
                        return Price - discount;
                    }
                }
                return Price; // Không có giảm giá
            }
        }

        // Khởi tạo danh sách rỗng khi tạo mới
        public ProductViewModel()
        {
            Colors = new List<ColorViewModel>();
            SaleIds = new List<int>();
            Sales = new List<SaleViewModel>();
        }
    }
}