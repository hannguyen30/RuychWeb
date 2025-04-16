namespace RuychWeb.Areas.Admin.Models
{
    public class ProductIndexViewModel
    {
        public List<ProductViewModel> Products { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public string SearchText { get; set; }
    }
}
