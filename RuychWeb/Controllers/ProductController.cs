using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Repository;

namespace RuychWeb.Controllers
{
    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductController> _logger;
        public ProductController(DataContext dataContext, IWebHostEnvironment webHostEnvironment, ILogger<ProductController> logger)
        {
            _dataContext = dataContext;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int Id)
        {
            // Kiểm tra xem sản phẩm có tồn tại không trước
            var product = await _dataContext.Products
                .FirstOrDefaultAsync(p => p.ProductId == Id);

            if (product == null)
            {
                return NotFound(); // Không tìm thấy sản phẩm
            }

            // Load đầy đủ thông tin liên quan đến sản phẩm
            product = await _dataContext.Products
                .Where(p => p.ProductId == Id)
                .Include(p => p.Category)
                .Include(p => p.Colors)
                    .ThenInclude(c => c.ProductDetails)
                .Include(p => p.SaleDetails)
                    .ThenInclude(sd => sd.Sale)
                .FirstOrDefaultAsync();

            // Tạo ViewModel để truyền qua View
            var productViewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                Thumbnail = product.Thumbnail,
                Description = product.Description,
                CategoryName = product.Category?.Name,
                Colors = product.Colors?.Select(c => new ColorViewModel
                {
                    ColorName = c.Name,
                    Sizes = c.ProductDetails?.Select(pd => new SizeQuantityViewModel
                    {
                        Size = pd.Size,
                        Quantity = pd.Quantity
                    }).ToList() ?? new List<SizeQuantityViewModel>()
                }).ToList() ?? new List<ColorViewModel>(),
                Sales = product.SaleDetails?.Select(sd => new SaleViewModel
                {
                    SaleName = sd.Sale.Name,
                    Discount = sd.Sale.Discount,
                    StartDate = sd.Sale.StartDate,
                    EndDate = sd.Sale.EndDate
                }).ToList() ?? new List<SaleViewModel>(),
                SaleIds = product.SaleDetails?.Select(sd => sd.SaleId).ToList() ?? new List<int>()
            };

            return View(productViewModel);
        }


    }
}
