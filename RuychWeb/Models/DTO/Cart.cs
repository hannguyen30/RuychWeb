using RuychWeb.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class Cart
    {
        [Key]
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public ICollection<CartDetail> CartDetails { get; set; }
    }
}
