using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class CartDetail
    {
        [Key]
        public int CartDetailId { get; set; }
        public int Quantity { get; set; }
        public int CartId { get; set; }
        public Cart Cart { get; set; }
        public int ProductDetailId { get; set; }
        public ProductDetail ProductDetail { get; set; }
    }
}
