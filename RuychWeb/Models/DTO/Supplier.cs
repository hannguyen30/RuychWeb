using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public ICollection<Receipt> Receipts { get; set; }
    }
}
