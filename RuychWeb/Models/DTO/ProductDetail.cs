using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace RuychWeb.Models.DTO
{
    public class ProductDetail
    {
        [Key]
        public int ProductDetailId { get; set; }
        public string Size { get; set; }
        public int Quantity { get; set; }
        public int ColorId { get; set; }
        public Color Color { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
        public ICollection<ReceiptDetail> ReceiptDetails { get; set; }
        public ICollection<CartDetail> CartDetails { get; set; }
    }
}
