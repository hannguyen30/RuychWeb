namespace RuychWeb.Areas.Admin.Models
{
    public class OrderEditViewModel
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? EmployeeName { get; set; }
    }
}
