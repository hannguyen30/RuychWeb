using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class Color
    {
        [Key]
        public int ColorId { get; set; }
        public string? Name { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public ICollection<ProductDetail> ProductDetails { get; set; }
    }
}
