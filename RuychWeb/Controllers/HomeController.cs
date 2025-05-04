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

    public IActionResult Index(string success)
    {
        ViewData["Title"] = "Trang chủ | Ruych Studio";
        if (success == "true")
        {
            TempData["OrderSuccess"] = "Đặt hàng thành công!";
        }

        return View();
    }
    [HttpGet]
    public IActionResult GetList(int page = 1, int pageSize = 6)
    {

        var totalProducts = _dataContext.Products.Where(p => p.OnSale).Count();
        var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
        var products = _dataContext.Products.Where(p => p.OnSale)
            .Include(p => p.SaleDetails)
                .ThenInclude(sd => sd.Sale)
            .Include(p => p.Colors)
                .ThenInclude(c => c.ProductDetails)
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
                    Discount = (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0,
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
                    Discount = (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0,
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
        ViewBag.Title = "Liên hệ | Ruych Studio";
        return View();
    }
}
