namespace RuychWeb.Areas.Admin.Models
{
    public class ColorViewModel
    {
        public int ColorId { get; set; }  // ID của màu sắc
        public string ColorName { get; set; }  // Tên màu
        public List<SizeQuantityViewModel> Sizes { get; set; }  // Kích thước và số lượng

        public ColorViewModel()
        {
            Sizes = new List<SizeQuantityViewModel>(); // Khởi tạo kích thước và số lượng rỗng khi 1 màu mới được tạo
        }
    }
}
