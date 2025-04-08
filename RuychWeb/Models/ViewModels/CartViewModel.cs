namespace RuychWeb.Models.ViewModels
{
    public class CartViewModel
    {
        public string ProductName { get; set; }
        public string Image { get; set; }
        public decimal Price { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public int Quantity { get; set; }

        public decimal Total => Price * Quantity;
    }

}
