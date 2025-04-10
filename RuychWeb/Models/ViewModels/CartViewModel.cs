namespace RuychWeb.Models.ViewModels
{
    public class CartViewModel
    {
        public int CartId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        // Danh sách các sản phẩm trong giỏ hàng
        public List<CartItemViewModel> CartItems { get; set; }

        // Tổng giá trị giỏ hàng
        public decimal TotalPrice { get; set; }
    }

}
