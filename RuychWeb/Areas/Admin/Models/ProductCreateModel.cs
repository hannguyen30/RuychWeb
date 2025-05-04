using RuychWeb.Areas.Admin.Repository.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace RuychWeb.Areas.Admin.Models
{
    public class ProductCreateModel
    {
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public string? Thumbnail { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int? Quantity { get; set; }
        public int? SaleId { get; set; }
        public bool OnSale { get; set; } = false;
        [NotMapped]
        [FileExtension]
        public IFormFile? ThumbnailFile { get; set; } // Thay đổi kiểu dữ liệu thành IFormFile
    }
}
