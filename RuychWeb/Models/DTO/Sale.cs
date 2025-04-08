using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class Sale
    {
        [Key]
        public int SaleId { get; set; }
        public string Name { get; set; }
        public decimal Discount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ICollection<SaleDetail> SaleDetails { get; set; }
    }
}
