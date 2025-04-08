namespace RuychWeb.Areas.Admin.Models
{
    public class SaleViewModel
    {
        public string SaleName { get; set; } // Tên chương trình giảm giá
        public decimal Discount { get; set; } // Mức giảm giá
        public DateTime StartDate { get; set; } // Thời gian bắt đầu
        public DateTime EndDate { get; set; } // Thời gian kết thúc
    }
}
