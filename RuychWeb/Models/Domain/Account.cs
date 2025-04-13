using Microsoft.AspNetCore.Identity;

namespace RuychWeb.Models.Domain
{
    public class Account : IdentityUser
    {
        public ICollection<Customer> Customers { get; set; }
        public ICollection<Employee> Employees { get; set; }
    }
}