using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Repository;

namespace RuychWeb.Controllers
{
    [Route("report")]
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ReportController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<ReportController> _logger;  // Khai báo ILogger

        public ReportController(DataContext context, ILogger<ReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Revenue")]
        public async Task<IActionResult> Revenue(DateTime? from, DateTime? to, string periodType)
        {
            if (!from.HasValue && !to.HasValue)
            {
                return View(new List<dynamic>());
            }

            var query = _context.Orders
                .Where(o => o.PaymentDate != null && o.PaymentStatus == "Đã thanh toán" && o.OrderStatus == "Đã hoàn thành");

            if (from.HasValue)
            {
                query = query.Where(o => o.PaymentDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(o => o.PaymentDate.Value.Date <= to.Value);
            }

            List<dynamic> revenueData;

            // Nếu periodType không được cung cấp, mặc định là theo "tháng"
            if (string.IsNullOrEmpty(periodType))
            {
                periodType = "tháng";
            }

            // Xử lý nhóm dữ liệu theo ngày, tháng, năm
            switch (periodType.ToLower())
            {
                case "ngày":
                    revenueData = await query
                        .GroupBy(o => o.PaymentDate.Value.Date)
                        .Select(g => new
                        {
                            Period = g.Key.ToString("yyyy-MM-dd"),
                            TotalRevenue = g.Sum(o => o.TotalFee)
                        })
                        .Cast<dynamic>()
                        .ToListAsync();
                    break;

                case "tháng":
                    revenueData = await query
                        .GroupBy(o => new { o.PaymentDate.Value.Year, o.PaymentDate.Value.Month })
                        .Select(g => new
                        {
                            Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                            TotalRevenue = g.Sum(o => o.TotalFee)
                        }).Cast<dynamic>()
                        .ToListAsync();
                    break;

                case "năm":
                    revenueData = await query
                        .GroupBy(o => o.PaymentDate.Value.Year)
                        .Select(g => new
                        {
                            Period = g.Key.ToString(),
                            TotalRevenue = g.Sum(o => o.TotalFee)
                        }).Cast<dynamic>()
                        .ToListAsync();
                    break;

                default:
                    // Nếu periodType không hợp lệ, mặc định sẽ theo tháng
                    revenueData = await query
                        .GroupBy(o => new { o.PaymentDate.Value.Year, o.PaymentDate.Value.Month })
                        .Select(g => new
                        {
                            Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                            TotalRevenue = g.Sum(o => o.TotalFee)
                        }).Cast<dynamic>()
                        .ToListAsync();
                    break;
            }

            return View(revenueData);
        }

        [HttpGet("TotalImportCost")]
        public async Task<IActionResult> TotalImportCost(DateTime? from, DateTime? to, string periodType)
        {
            var query = _context.Receipts.AsQueryable();
            if (!from.HasValue && !to.HasValue)
            {
                return View(new List<dynamic>());
            }
            if (from.HasValue)
                query = query.Where(r => r.CreatedDate.Date >= from.Value.Date);
            if (to.HasValue)
                query = query.Where(r => r.CreatedDate.Date <= to.Value.Date);

            // Mặc định là theo tháng
            if (string.IsNullOrEmpty(periodType))
                periodType = "tháng";

            List<dynamic> costData;

            switch (periodType.ToLower())
            {
                case "ngày":
                    costData = await query
                        .GroupBy(r => r.CreatedDate.Date)
                        .Select(g => new
                        {
                            Period = g.Key.ToString("yyyy-MM-dd"),
                            TotalCost = g.SelectMany(r => r.ReceiptDetails).Sum(rd => rd.Quantity * rd.Price)
                        })
                        .Cast<dynamic>()
                        .ToListAsync();
                    break;

                case "tháng":
                    costData = await query
                        .GroupBy(r => new { r.CreatedDate.Year, r.CreatedDate.Month })
                        .Select(g => new
                        {
                            Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                            TotalCost = g.SelectMany(r => r.ReceiptDetails).Sum(rd => rd.Quantity * rd.Price)
                        })
                        .Cast<dynamic>()
                        .ToListAsync();
                    break;

                case "năm":
                    costData = await query
                        .GroupBy(r => r.CreatedDate.Year)
                        .Select(g => new
                        {
                            Period = g.Key.ToString(),
                            TotalCost = g.SelectMany(r => r.ReceiptDetails).Sum(rd => rd.Quantity * rd.Price)
                        })
                        .Cast<dynamic>()
                        .ToListAsync();
                    break;

                default:
                    costData = new List<dynamic>();
                    break;
            }

            return View(costData);
        }

        [HttpGet("TopSellingProducts")]
        public async Task<IActionResult> TopSellingProducts(int? month, int? year)
        {
            // Lấy các chi tiết đơn hàng kèm theo thông tin đơn hàng, sản phẩm và màu
            var query = _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductDetail)
                    .ThenInclude(pd => pd.Color)
                        .ThenInclude(c => c.Product)
                .AsQueryable();

            // Chỉ lấy đơn hàng đã thanh toán và hoàn thành
            query = query.Where(od =>
                od.Order != null &&
                od.Order.PaymentStatus == "Đã thanh toán" &&
                od.Order.OrderStatus == "Đã hoàn thành" &&
                od.Order.PaymentDate.HasValue);

            if (!month.HasValue && !year.HasValue)
            {
                return View(new List<dynamic>());
            }
            // Lọc theo tháng và năm nếu có
            if (month.HasValue)
            {
                query = query.Where(od => od.Order.PaymentDate.Value.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(od => od.Order.PaymentDate.Value.Year == year.Value);
            }

            // Nhóm theo sản phẩm (ProductDetailId) và tính tổng số lượng bán
            var data = await query
                .GroupBy(od => od.ProductDetailId)
                .Select(g => new
                {
                    ProductName = g.First().ProductDetail.Color.Product.Name,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Color = g.First().ProductDetail.Color.Name
                })
                .OrderByDescending(x => x.QuantitySold)
                .ToListAsync();

            // Nếu không có dữ liệu thì trả về view rỗng
            if (!data.Any())
            {
                return View(new List<object>());
            }

            // Tính phần trăm cho từng sản phẩm
            var totalQuantitySold = data.Sum(x => x.QuantitySold);
            var result = data.Select(item => new
            {
                item.ProductName,
                item.QuantitySold,
                item.Color,
                Percent = totalQuantitySold > 0 ? (item.QuantitySold * 100.0) / totalQuantitySold : 0
            }).ToList();

            return View(result);
        }

        [HttpGet("StockReport")]
        public async Task<IActionResult> StockReport(string? productName, string? color, string? size)
        {
            var query = _context.ProductDetails
                .Include(pd => pd.Color)
                    .ThenInclude(c => c.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(productName))
            {
                query = query.Where(pd => pd.Color.Product.Name.Contains(productName));
            }

            if (!string.IsNullOrEmpty(color))
            {
                query = query.Where(pd => pd.Color.Name.Contains(color));
            }

            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(pd => pd.Size.Contains(size));
            }

            var stockData = await query
                .Select(pd => new StockReportViewModel
                {
                    ProductName = pd.Color.Product.Name,
                    Color = pd.Color.Name,
                    Size = pd.Size,
                    Quantity = pd.Quantity
                })
                .ToListAsync();

            return View(stockData);
        }

    }
}
