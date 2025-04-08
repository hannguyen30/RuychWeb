using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class ReceiptDetail
    {
        [Key]
        public int ReceiptDetailId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int ReceiptId { get; set; }
        public Receipt Receipt { get; set; }
        public int ProductDetailId { get; set; }
        public ProductDetail ProductDetail { get; set; }
    }
}
