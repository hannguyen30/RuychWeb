using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class SaleDetail
    {
        [Key]
        public int SaleDetailId { get; set; }
        public int SaleId { get; set; }
        public Sale Sale { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
