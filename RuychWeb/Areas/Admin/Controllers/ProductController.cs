using RuychWeb.Models;
using RuychWeb.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Models.DTO;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ProductController> _logger;

        public ProductController(DataContext context, IWebHostEnvironment webHostEnvironment, ILogger<ProductController> logger)
        {
            _dataContext = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/Product/Create
        public IActionResult Create()
        {
            // Lấy danh sách các danh mục từ cơ sở dữ liệu
            ViewBag.Categories = new SelectList(_dataContext.Categories, "CategoryId", "Name");

            // Lấy danh sách các khuyến mãi từ cơ sở dữ liệu
            ViewBag.Sales = new SelectList(_dataContext.Sales, "SaleId", "Name");

            return View();
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateModel model)
        {
            if (ModelState.IsValid)
            {
                var existingProduct = await _dataContext.Products
                     .FirstOrDefaultAsync(p => p.Name == model.Name);

                if (existingProduct != null)
                {
                    // Nếu sản phẩm đã tồn tại, hiển thị thông báo lỗi
                    ModelState.AddModelError("Name", "Sản phẩm với tên này đã tồn tại.");
                    ViewBag.Categories = new SelectList(_dataContext.Categories, "CategoryId", "Name");
                    return View(model);
                }
                if (model.ThumbnailFile != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images/Products");
                    string imageName = Guid.NewGuid().ToString() + "_" + model.ThumbnailFile.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new FileStream(filePath, FileMode.Create);
                    await model.ThumbnailFile.CopyToAsync(fs);
                    fs.Close();
                    model.Thumbnail = imageName;
                }

                // Tạo sản phẩm mới
                var product = new Product
                {
                    Name = model.Name,
                    Price = model.Price,
                    Thumbnail = model.Thumbnail ?? "default.png",  // Nếu không có thumbnail thì sử dụng ảnh mặc định
                    Description = model.Description,
                    CategoryId = model.CategoryId
                };

                _dataContext.Products.Add(product);
                await _dataContext.SaveChangesAsync();  // Lưu sản phẩm

                // Tạo màu sắc
                var color = new Color
                {
                    Name = model.Color,
                    ProductId = product.ProductId
                };
                _dataContext.Colors.Add(color);
                await _dataContext.SaveChangesAsync();  // Lưu màu sắc

                // Tạo chi tiết sản phẩm
                var productDetail = new ProductDetail
                {
                    ColorId = color.ColorId,
                    Size = model.Size,
                    Quantity = model.Quantity
                };
                _dataContext.ProductDetails.Add(productDetail);
                await _dataContext.SaveChangesAsync();  // Lưu chi tiết sản phẩm

                // Nếu có thông tin về khuyến mãi
                if (model.SaleId.HasValue)  // Kiểm tra nếu SaleId không null
                {
                    var saleDetail = new SaleDetail
                    {
                        SaleId = model.SaleId.Value,  // Lấy SaleId
                        ProductId = product.ProductId
                    };
                    _dataContext.SaleDetails.Add(saleDetail);
                    await _dataContext.SaveChangesAsync();  // Lưu chi tiết khuyến mãi
                }

                // Quay lại danh sách sản phẩm sau khi tạo thành công
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi, hiển thị lại form
            ViewBag.Categories = new SelectList(_dataContext.Categories, "CategoryId", "Name");
            ViewBag.Sales = new SelectList(_dataContext.Sales, "SaleId", "Name");
            return View(model);
        }

        // GET: Admin/Product/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.Colors)
                    .ThenInclude(c => c.ProductDetails)
                .Include(p => p.SaleDetails)
                    .ThenInclude(sd => sd.Sale)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductEditModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                Thumbnail = product.Thumbnail,
                Description = product.Description,
                CategoryId = product.CategoryId,
                // Kiểm tra xem product.Colors có tồn tại không và có ít nhất một màu không
                Color = product.Colors?.FirstOrDefault()?.Name,  // Chọn màu đầu tiên nếu có
                                                                 // Kiểm tra xem ProductDetails có tồn tại không trước khi lấy Size và Quantity
                Size = product.Colors?.SelectMany(c => c.ProductDetails)?.Select(pd => pd.Size).FirstOrDefault(),
                Quantity = (int)product.Colors?.SelectMany(c => c.ProductDetails)?.Select(pd => pd.Quantity).FirstOrDefault(),
                // Kiểm tra SaleDetails trước khi lấy SaleId
                SaleId = product.SaleDetails?.Select(sd => sd.SaleId).FirstOrDefault()  // Lấy SaleId đầu tiên nếu có
            };

            // Lấy danh sách categories và sales cho dropdown
            ViewBag.Categories = new SelectList(_dataContext.Categories, "CategoryId", "Name");
            ViewBag.Sales = new SelectList(_dataContext.Sales, "SaleId", "Name");

            return View(model);
        }


        //[HttpPost("{id}")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, ProductEditModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        model.ProductId = id;

        //        var product = await _dataContext.Products
        //            .Include(p => p.Colors)
        //                .ThenInclude(c => c.ProductDetails)
        //            .Include(p => p.SaleDetails)
        //            .FirstOrDefaultAsync(p => p.ProductId == id);

        //        if (product == null)
        //        {
        //            return NotFound();
        //        }

        //        // Kiểm tra và xử lý nếu có file Thumbnail mới
        //        if (model.ThumbnailFile != null)
        //        {
        //            string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images/Products");
        //            string imageName = Guid.NewGuid().ToString() + "_" + model.ThumbnailFile.FileName;
        //            string filePath = Path.Combine(uploadsDir, imageName);

        //            using (FileStream fs = new FileStream(filePath, FileMode.Create))
        //            {
        //                await model.ThumbnailFile.CopyToAsync(fs);
        //            }
        //            model.Thumbnail = imageName;
        //        }
        //        else
        //        {
        //            // Giữ nguyên Thumbnail cũ nếu không có file mới
        //            model.Thumbnail = product.Thumbnail;
        //        }

        //        // Cập nhật các thông tin sản phẩm
        //        product.Name = model.Name;
        //        product.Price = model.Price;
        //        product.Thumbnail = model.Thumbnail;
        //        product.Description = model.Description;
        //        product.CategoryId = model.CategoryId;

        //        _dataContext.Products.Update(product);
        //        await _dataContext.SaveChangesAsync();

        //        // Cập nhật hoặc tạo màu sắc mới nếu không có
        //        var color = product.Colors.FirstOrDefault(c => c.Name == model.Color);
        //        if (color == null && !string.IsNullOrEmpty(model.Color))
        //        {
        //            color = new Color
        //            {
        //                Name = model.Color,
        //                ProductId = product.ProductId
        //            };
        //            _dataContext.Colors.Add(color);
        //            await _dataContext.SaveChangesAsync();
        //            await Task.Delay(500);// Lưu màu vào cơ sở dữ liệu
        //        }
                
        //        // Đảm bảo color đã được tạo trước khi tiếp tục thêm chi tiết sản phẩm
        //        if (color != null)  // Kiểm tra nếu màu đã được tạo
        //        {
        //            // Cập nhật hoặc tạo chi tiết sản phẩm (Size và Quantity)
        //            var productDetail = color.ProductDetails
        //                .FirstOrDefault(pd => pd.Size == model.Size);

        //            if (productDetail == null && !string.IsNullOrEmpty(model.Size))
        //            {
        //                productDetail = new ProductDetail
        //                {
        //                    ColorId = color.ColorId,  // Liên kết với màu đã tạo
        //                    Size = model.Size,
        //                    Quantity = model.Quantity
        //                };
        //                _dataContext.ProductDetails.Add(productDetail);
        //            }
        //            else if (productDetail != null)
        //            {
        //                // Cập nhật số lượng nếu đã có chi tiết sản phẩm
        //                productDetail.Quantity = model.Quantity;
        //            }

        //            await _dataContext.SaveChangesAsync();
        //        }

        //        // Cập nhật thông tin khuyến mãi
        //        if (model.SaleId.HasValue)
        //        {
        //            var existingSaleDetail = product.SaleDetails.FirstOrDefault();
        //            if (existingSaleDetail == null)
        //            {
        //                var newSaleDetail = new SaleDetail
        //                {
        //                    SaleId = model.SaleId.Value,
        //                    ProductId = product.ProductId
        //                };
        //                _dataContext.SaleDetails.Add(newSaleDetail);
        //            }
        //            else
        //            {
        //                existingSaleDetail.SaleId = model.SaleId.Value;
        //            }
        //        }
        //        else
        //        {
        //            // Nếu không có SaleId, loại bỏ chi tiết khuyến mãi nếu có
        //            var existingSaleDetail = product.SaleDetails.FirstOrDefault();
        //            if (existingSaleDetail != null)
        //            {
        //                _dataContext.SaleDetails.Remove(existingSaleDetail);
        //            }
        //        }

        //        await _dataContext.SaveChangesAsync();

        //        return RedirectToAction(nameof(Index));
        //    }

        //    // Nếu model không hợp lệ, giữ lại danh sách categories và sales
        //    ViewBag.Categories = new SelectList(_dataContext.Categories, "CategoryId", "Name");
        //    ViewBag.Sales = new SelectList(_dataContext.Sales, "SaleId", "Name");
        //    return View(model);
        //}


        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditModel model)
        {
            if (ModelState.IsValid)
            {
                model.ProductId = id;

                var product = await _dataContext.Products
                    .Include(p => p.Colors)
                        .ThenInclude(c => c.ProductDetails)
                    .Include(p => p.SaleDetails)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin sản phẩm
                await UpdateProductAsync(product, model);

                // Xử lý màu sắc
                var color = await UpdateOrCreateColorAsync(product, model.Color);

                if (color != null)  // Kiểm tra xem màu đã được tạo chưa
                {
                    // Nếu có màu, thực hiện cập nhật chi tiết sản phẩm (size và quantity)
                    await UpdateProductDetailsAsync(color, model);
                }
                else
                {
                    // Nếu không có màu, bạn có thể chọn cách xử lý như tạo mới hoặc không làm gì
                    // Ví dụ: Tạo màu mới nếu cần
                    if (!string.IsNullOrEmpty(model.Color))
                    {
                        color = new Color
                        {
                            Name = model.Color,
                            ProductId = product.ProductId
                        };
                        _dataContext.Colors.Add(color);
                        await _dataContext.SaveChangesAsync();  // Lưu màu mới vào DB
                        await UpdateProductDetailsAsync(color, model); // Tiến hành cập nhật chi tiết sản phẩm sau khi tạo màu
                    }
                }


                // Cập nhật thông tin khuyến mãi
                await UpdateSaleDetailsAsync(product, model);

                return RedirectToAction(nameof(Index));
            }

            // Nếu model không hợp lệ, giữ lại danh sách categories và sales
            ViewBag.Categories = new SelectList(_dataContext.Categories, "CategoryId", "Name");
            ViewBag.Sales = new SelectList(_dataContext.Sales, "SaleId", "Name");
            return View(model);
        }

        private async Task UpdateProductAsync(Product product, ProductEditModel model)
        {
            // Cập nhật hoặc xử lý Thumbnail
            if (model.ThumbnailFile != null)
            {
                string imageName = await SaveThumbnailAsync(model);
                model.Thumbnail = imageName;
            }
            else
            {
                model.Thumbnail = product.Thumbnail;
            }

            // Cập nhật thông tin sản phẩm
            product.Name = model.Name;
            product.Price = model.Price;
            product.Thumbnail = model.Thumbnail;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;

            _dataContext.Products.Update(product);
            await _dataContext.SaveChangesAsync(); // Lưu sản phẩm
        }

        private async Task<string> SaveThumbnailAsync(ProductEditModel model)
        {
            string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "images/Products");
            string imageName = Guid.NewGuid().ToString() + "_" + model.ThumbnailFile.FileName;
            string filePath = Path.Combine(uploadsDir, imageName);

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                await model.ThumbnailFile.CopyToAsync(fs);
            }

            return imageName;
        }
         
        private async Task<Color> UpdateOrCreateColorAsync(Product product, string colorName)
        {
            // Kiểm tra và tạo mới màu sắc nếu chưa có
            var color = product.Colors.FirstOrDefault(c => c.Name == colorName);
            if (color == null && !string.IsNullOrEmpty(colorName))
            {
                color = new Color
                {
                    Name = colorName,
                    ProductId = product.ProductId
                };
                _dataContext.Colors.Add(color);
                await _dataContext.SaveChangesAsync(); // Lưu màu vào cơ sở dữ liệu
            }

            return color;
        }

        private async Task UpdateProductDetailsAsync(Color color, ProductEditModel model)
        {
            // Kiểm tra nếu có size và quantity, rồi xử lý chi tiết sản phẩm
            if (!string.IsNullOrEmpty(model.Size) && model.Quantity.HasValue)
            {
                var productDetail = color.ProductDetails
                    .FirstOrDefault(pd => pd.Size == model.Size);

                if (productDetail == null)
                {
                    productDetail = new ProductDetail
                    {
                        ColorId = color.ColorId,  // Liên kết với màu đã tạo
                        Size = model.Size,
                        Quantity = model.Quantity.Value
                    };
                    _dataContext.ProductDetails.Add(productDetail);
                }
                else
                {
                    // Cập nhật số lượng nếu đã có chi tiết sản phẩm
                    productDetail.Quantity = model.Quantity.Value;
                }

                await _dataContext.SaveChangesAsync();
            }
        }

        private async Task UpdateSaleDetailsAsync(Product product, ProductEditModel model)
        {
            // Cập nhật thông tin khuyến mãi
            if (model.SaleId.HasValue)
            {
                var existingSaleDetail = product.SaleDetails.FirstOrDefault();
                if (existingSaleDetail == null)
                {
                    var newSaleDetail = new SaleDetail
                    {
                        SaleId = model.SaleId.Value,
                        ProductId = product.ProductId
                    };
                    _dataContext.SaleDetails.Add(newSaleDetail);
                }
                else
                {
                    existingSaleDetail.SaleId = model.SaleId.Value;
                }
            }
            else
            {
                // Nếu không có SaleId, loại bỏ chi tiết khuyến mãi nếu có
                var existingSaleDetail = product.SaleDetails.FirstOrDefault();
                if (existingSaleDetail != null)
                {
                    _dataContext.SaleDetails.Remove(existingSaleDetail);
                }
            }

            await _dataContext.SaveChangesAsync(); // Lưu thông tin khuyến mãi
        }



        // POST: Admin/Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _dataContext.Products
                .Include(p => p.Colors)
                .Include(p => p.SaleDetails)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Xóa chi tiết sản phẩm và màu sắc
            var productDetails = _dataContext.ProductDetails.Where(pd => pd.Color.ProductId == id);
            _dataContext.ProductDetails.RemoveRange(productDetails);

            var colors = _dataContext.Colors.Where(c => c.ProductId == id);
            _dataContext.Colors.RemoveRange(colors);

            // Xóa chi tiết khuyến mãi
            var saleDetails = _dataContext.SaleDetails.Where(sd => sd.ProductId == id);
            _dataContext.SaleDetails.RemoveRange(saleDetails);

            // Cuối cùng xóa sản phẩm
            _dataContext.Products.Remove(product);
            await _dataContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));  // Quay lại trang danh sách sản phẩm
        }




        // GET: Admin/Product/Index

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 4)
        {
            // Lấy tổng số sản phẩm trong cơ sở dữ liệu
            var totalProducts = await _dataContext.Products.CountAsync();

            // Tính tổng số trang
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            // Đảm bảo pageNumber không vượt quá tổng số trang
            pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages));

            // Lấy danh sách sản phẩm cho trang hiện tại
            var products = await _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.Colors)
                    .ThenInclude(c => c.ProductDetails)
                .Include(p => p.SaleDetails)
                    .ThenInclude(sd => sd.Sale)
                .Skip((pageNumber - 1) * pageSize)  // Bỏ qua các sản phẩm của các trang trước
                .Take(pageSize)                     // Lấy sản phẩm cho trang hiện tại
                .ToListAsync();

            // Chuyển đổi các sản phẩm sang ViewModel
            var productViewModels = products.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,
                Thumbnail = p.Thumbnail,
                Description = p.Description,
                CategoryName = p.Category?.Name,
                Colors = p.Colors?.Select(c => new ColorViewModel
                {
                    ColorName = c.Name,
                    Sizes = c.ProductDetails?.Select(pd => new SizeQuantityViewModel
                    {
                        Size = pd.Size,
                        Quantity = pd.Quantity
                    }).ToList() ?? new List<SizeQuantityViewModel>()
                }).ToList() ?? new List<ColorViewModel>(),
                Sales = p.SaleDetails?.Select(sd => new SaleViewModel
                {
                    SaleName = sd.Sale.Name,
                    Discount = sd.Sale.Discount,
                    StartDate = sd.Sale.StartDate,
                    EndDate = sd.Sale.EndDate
                }).ToList() ?? new List<SaleViewModel>(),
                SaleIds = p.SaleDetails?.Select(sd => sd.SaleId).ToList() ?? new List<int>()
            }).ToList();

            // Truyền thông tin phân trang vào View
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;

            return View(productViewModels);
        }


    }
}
