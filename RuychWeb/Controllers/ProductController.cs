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
        public IActionResult Index(string searchText)
        {
            ViewBag.SearchText = searchText;
            return View();
        }
        public IActionResult GetList(int page = 1, int pageSize = 9, string searchText = "", string sort_by = "", decimal minPrice = 0, decimal maxPrice = decimal.MaxValue)
        {
            var query = _dataContext.Products.Where(p => p.OnSale)
                .Include(p => p.SaleDetails).ThenInclude(sd => sd.Sale)
                .Include(p => p.Colors).ThenInclude(c => c.ProductDetails)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                decimal price;
                bool isPrice = decimal.TryParse(searchText, out price);

                query = query.Where(p =>
                    p.Name.Contains(searchText) ||
                    p.Category.Name.Contains(searchText) ||
                    (isPrice && p.Price == price)
                );
            }

            query = query.Where(p => (p.Price ?? 0) >= minPrice && (p.Price ?? 0) <= maxPrice);

            switch (sort_by)
            {
                case "price_increase":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_decrease":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderBy(p => p.ProductId);
                    break;
            }

            var totalProducts = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var products = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    Price = p.Price ?? 0,
                    p.Thumbnail,
                    p.Description,
                    SaleDetails = p.SaleDetails.Select(sd => new
                    {
                        Discount = (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0
                    }),
                    Colors = p.Colors.Select(c => new
                    {
                        c.Name,
                        Sizes = c.ProductDetails.Select(pd => new { pd.Size, pd.Quantity })
                    })
                }).ToList();

            return Json(new { data = products, totalPages, currentPage = page });
        }


        [HttpGet]
        public async Task<IActionResult> Detail(int Id)
        {

            var product = await _dataContext.Products
                .FirstOrDefaultAsync(p => p.ProductId == Id);

            product = await _dataContext.Products
                .Where(p => p.ProductId == Id)
                .Include(p => p.Category)
                .Include(p => p.Colors)
                    .ThenInclude(c => c.ProductDetails)
                .Include(p => p.SaleDetails)
                    .ThenInclude(sd => sd.Sale)
                .FirstOrDefaultAsync();


            var productViewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = (decimal)product.Price,
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
                    Discount = (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0,
                    StartDate = sd.Sale.StartDate,
                    EndDate = sd.Sale.EndDate
                }).ToList() ?? new List<SaleViewModel>(),
                SaleIds = product.SaleDetails?.Select(sd => sd.SaleId).ToList() ?? new List<int>()
            };

            return View(productViewModel);
        }

        public async Task<IActionResult> ByCategory(string name = "")
        {

            var category = await _dataContext.Categories.FirstOrDefaultAsync(c => c.Name == name);
            if (category == null)
            {
                return RedirectToAction("Index", "Product");
            }


            var products = await _dataContext.Products
                .Where(p => p.CategoryId == category.CategoryId)
                .Include(p => p.SaleDetails).ThenInclude(sd => sd.Sale)
                .Include(p => p.Colors).ThenInclude(c => c.ProductDetails)
                .OrderBy(p => p.ProductId)
                .ToListAsync();


            var viewModel = products.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price ?? 0,
                Thumbnail = p.Thumbnail,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = category.Name,


                Sales = p.SaleDetails.Select(sd => new SaleViewModel
                {
                    Discount = (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0,
                }).ToList(),

                Colors = p.Colors.Select(c => new ColorViewModel
                {
                    ColorId = c.ColorId,
                    ColorName = c.Name,
                    Sizes = c.ProductDetails.Select(pd => new SizeQuantityViewModel
                    {
                        Size = pd.Size,
                        Quantity = pd.Quantity
                    }).ToList()
                }).ToList()

            }).ToList();

            ViewBag.SelectedCategory = category.Name;
            return View(viewModel);
        }

    }
}
