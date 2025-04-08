using RuychWeb.Models.DTO;
using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.Domain
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? Birthday { get; set; }
        public string AccountId { get; set; }
        public virtual Account Account { get; set; }
        public ICollection<Receipt> Receipts { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}
