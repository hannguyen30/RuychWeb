using RuychWeb.Models.DTO;
using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.Domain
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string Email { get; set; }
        public string? Address { get; set; }
        public string AccountId { get; set; }
        public virtual Account Account { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Cart> Carts { get; set; }
    }
}
