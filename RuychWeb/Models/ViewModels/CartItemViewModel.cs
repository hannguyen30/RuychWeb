namespace RuychWeb.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int CartDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string ProductThumbnail { get; set; }
        public decimal Price { get; set; }
        public string Size { get; set; }
        public string ColorName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
