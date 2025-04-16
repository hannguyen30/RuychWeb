namespace RuychWeb.Models.ViewModels
{
    public class OrderHistoryViewModel
    {
        public int OrderId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }
        public decimal TotalFee { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string Address { get; set; }
        public string Ward { get; set; }
        public string District { get; set; }
        public string Province { get; set; }
        public decimal ShippingFee { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }
}
