using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Repository;

namespace RuychWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataContext _dataContext;

    public HomeController(ILogger<HomeController> logger, DataContext context)
    {
        _logger = logger;
        _dataContext = context;
    }

    public IActionResult Index()
    {
        return View();
    }
    [HttpGet]
    public IActionResult GetList(int page = 1, int pageSize = 6)
    {
        // Tổng số sản phẩm
        var totalProducts = _dataContext.Products.Count();

        // Tính toán số trang dựa trên tổng số sản phẩm và kích thước trang
        var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

        // Lấy danh sách sản phẩm cho trang hiện tại
        var products = _dataContext.Products
            .Include(p => p.SaleDetails)
                .ThenInclude(sd => sd.Sale)
            .Include(p => p.Colors)
                .ThenInclude(c => c.ProductDetails) // Bao gồm chi tiết sản phẩm (màu, kích thước, số lượng)
            .OrderBy(p => p.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.ProductId,
                p.Name,
                p.Price,
                p.Thumbnail,
                p.Description,
                SaleDetails = p.SaleDetails.Select(sd => new
                {
                    sd.Sale.Discount // Bao gồm thông tin giảm giá
                }),
                Colors = p.Colors.Select(c => new
                {
                    c.Name,
                    Sizes = c.ProductDetails.Select(pd => new
                    {
                        pd.Size,
                        pd.Quantity
                    })
                })
            })
            .ToList();


        return Json(new { data = products, TotalPages = totalPages, CurrentPage = page });
    }

    [HttpGet]
    public IActionResult Search(string searchText)
    {
        var searchResults = _dataContext.Products
            .Include(p => p.SaleDetails)
                .ThenInclude(sd => sd.Sale)
            .Include(p => p.Colors)
                .ThenInclude(c => c.ProductDetails)
            .Where(p =>
                p.Name.Contains(searchText) ||
                p.Price.ToString().Contains(searchText) ||
                p.Category.Name.Contains(searchText))
            .OrderBy(p => p.ProductId)
            .Select(p => new
            {
                p.ProductId,
                p.Name,
                p.Price,
                p.Thumbnail,
                p.Description,
                SaleDetails = p.SaleDetails.Select(sd => new
                {
                    sd.Sale.Discount
                }),
                Colors = p.Colors.Select(c => new
                {
                    c.Name,
                    Sizes = c.ProductDetails.Select(pd => new
                    {
                        pd.Size,
                        pd.Quantity
                    })
                })
            })
            .ToList();

        return Json(searchResults);
    }
    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }
}
