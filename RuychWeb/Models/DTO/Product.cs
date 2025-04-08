using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Thumbnail { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public ICollection<Color> Colors { get; set; }
        public ICollection<SaleDetail> SaleDetails { get; set; }
    }
}
