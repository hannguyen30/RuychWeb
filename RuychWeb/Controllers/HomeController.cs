using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Models;
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
        // T?ng s? s?n ph?m
        var totalProducts = _dataContext.Products.Count();

        // T�nh to�n s? trang d?a tr�n t?ng s? s?n ph?m v� k�ch th�?c trang
        var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

        // L?y danh s�ch s?n ph?m cho trang hi?n t?i
        var products = _dataContext.Products
                    .OrderBy(p => p.ProductId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

        return Json(new { data = products, TotalPages = totalPages, CurrentPage = page });
    }
}
