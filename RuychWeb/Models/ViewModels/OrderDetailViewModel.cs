namespace RuychWeb.Models.ViewModels
{
    public class OrderDetailViewModel
    {
        public int ProductDetailId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ProductName { get; set; }
        public string ColorName { get; set; }
        public decimal Discount { get; set; } // Assuming this is the discount percentage
        public string Size { get; set; }
    }
}
