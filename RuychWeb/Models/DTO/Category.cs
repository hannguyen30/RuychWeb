using System.ComponentModel.DataAnnotations;

namespace RuychWeb.Models.DTO
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
