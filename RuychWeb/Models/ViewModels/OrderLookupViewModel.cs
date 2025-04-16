namespace RuychWeb.Models.ViewModels
{
    public class OrderLookupViewModel
    {
        public int? OrderId { get; set; }
        public string Phone { get; set; }

        // Kết quả tra cứu
        public OrderHistoryViewModel OrderResult { get; set; }
        public bool NotFound { get; set; } = false;
    }
}
