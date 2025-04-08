namespace RuychWeb.Areas.Admin.Models
{
    using RuychWeb.Areas.Admin.Models;

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

        // Khởi tạo danh sách rỗng khi tạo mới
        public ProductViewModel()
        {
            Colors = new List<ColorViewModel>();
            SaleIds = new List<int>();
            Sales = new List<SaleViewModel>();
        }
    }
}