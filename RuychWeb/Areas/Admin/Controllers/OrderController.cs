using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;
using RuychWeb.Repository;
using Unidecode.NET;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class OrderController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly DataContext _context;
        private readonly UserManager<Account> _userManager;

        public OrderController(DataContext context, UserManager<Account> userManager, HttpClient httpClient)
        {
            _context = context;
            _userManager = userManager;
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
        {
            var totalOrder = await _context.Orders.CountAsync();

            // Tính tổng số trang
            var totalPages = (int)Math.Ceiling(totalOrder / (double)pageSize);

            // Đảm bảo pageNumber không vượt quá tổng số trang
            pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages));
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                            .ThenInclude(c => c.Product)
                .OrderByDescending(o => o.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)  // Bỏ qua các sản phẩm của các trang trước
                .Take(pageSize)
                .ToListAsync();
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            var userName = User.Identity.Name;

            var currentEmployee = await _userManager.FindByNameAsync(userName);
            if (currentEmployee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.AccountId == currentEmployee.Id);
            ViewBag.Products = new SelectList(_context.Products, "ProductId", "Name");

            return View(new OrderCreateViewModel
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.Name,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Products = new SelectList(_context.Products, "ProductId", "Name");
                ViewBag.EmployeeId = model.EmployeeId;
                return View(model);
            }

            // Tạo đơn hàng mới
            var order = new Order
            {
                Name = model.Name,
                Phone = model.Phone,
                Address = model.Address,
                PaymentMethod = model.PaymentMethod,
                OrderStatus = model.OrderStatus,
                PaymentStatus = model.PaymentStatus,
                CreatedDate = DateTime.Now,
                EmployeeId = model.EmployeeId,
                TotalFee = model.TotalAmount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Lưu chi tiết đơn hàng
            decimal recalculatedTotalFee = 0;

            foreach (var item in model.OrderItems.Where(x => x.Quantity > 0))
            {
                var productDetail = await _context.ProductDetails
                    .FirstOrDefaultAsync(pd => pd.ProductDetailId == item.ProductDetailId);

                if (productDetail != null)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductDetailId = item.ProductDetailId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };

                    _context.OrderDetails.Add(detail);

                    // Giảm tồn kho
                    productDetail.Quantity -= item.Quantity;

                    // Tính tổng tiền
                    recalculatedTotalFee += item.Price * item.Quantity;
                }
            }

            // Cập nhật lại tổng tiền
            order.TotalFee = recalculatedTotalFee;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo đơn thành công!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            var product = await _context.Products
                .Include(p => p.SaleDetails)
                    .ThenInclude(sd => sd.Sale)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return NotFound();
            }

            var price = product.Price;
            var discount = product.SaleDetails
                .Select(sd => (sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now) ? sd.Sale.Discount : 0)
                .FirstOrDefault();

            var details = await _context.Colors
                .Where(c => c.ProductId == productId)
                .Select(c => new
                {
                    ColorId = c.ColorId,
                    ColorName = c.Name,
                    Sizes = c.ProductDetails.Select(pd => new
                    {
                        pd.ProductDetailId,
                        pd.Size,
                        pd.Quantity
                    })
                }).ToListAsync();

            // Trả về thêm tên sản phẩm
            return Json(new { name = product.Name, price, discount, details });
        }


        [HttpGet("Admin/Order/Details/{orderId}")]
        public IActionResult Details(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                            .ThenInclude(c => c.Product)
                            .ThenInclude(c => c.SaleDetails)
                            .ThenInclude(c => c.Sale)
                .FirstOrDefault(o => o.OrderId == orderId);
            var employeeName = _context.Employees
                .Where(e => e.EmployeeId == order.EmployeeId)
                .Select(e => e.Name)
                .FirstOrDefault();
            if (order == null) return NotFound();
            var (address, ward, district, province) = SplitAddress(order.Address);

            var orderViewModel = new OrderManagerViewModel
            {
                OrderId = order.OrderId,
                Name = order.Name,
                EmployeeName = employeeName,
                Phone = order.Phone,
                Address = address,
                Ward = ward,
                District = district,
                Province = province,
                CreatedDate = order.CreatedDate,
                CompletedDate = order.CompletedDate,
                PaymentMethod = order.PaymentMethod,
                PaymentDate = order.PaymentDate,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                CancelReason = order.CancelReason,
                CarrierName = order.CarrierName,
                TotalFee = order.TotalFee,
                CustomerId = order.CustomerId,
                EmployeeId = order.EmployeeId,
                Discount = order.OrderDetails
                .SelectMany(od => od.ProductDetail.Color.Product.SaleDetails)
                .Where(sd => sd.Sale.StartDate <= DateTime.Now && sd.Sale.EndDate >= DateTime.Now)  // Kiểm tra thời gian hợp lệ của chương trình khuyến mãi
                .Select(sd => sd.Sale.Discount)  // Lấy mức giảm giá của các chương trình hợp lệ
                .DefaultIfEmpty(0)  // Nếu không có chương trình khuyến mãi hợp lệ, mặc định là 0
                .Max(),
                OrderDetails = order.OrderDetails.ToList(),
            };

            return View(orderViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userName = User.Identity.Name;

            var currentEmployee = await _userManager.FindByNameAsync(userName);
            if (currentEmployee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.AccountId == currentEmployee.Id);
            if (employee == null)
            {

                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            string employeeName = employee.Name;
            var viewModel = new OrderEditViewModel
            {
                OrderId = order.OrderId,
                OrderStatus = order.OrderStatus,
                CancelReason = order.CancelReason,
                CompletedDate = order.CompletedDate,
                EmployeeName = employeeName
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userName = User.Identity.Name;
            var currentEmployee = await _userManager.FindByNameAsync(userName);
            if (currentEmployee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductDetail)
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

            if (order == null)
            {
                return NotFound();
            }

            if (model.OrderStatus == "Đã hủy" && order.PaymentStatus == "Chưa thanh toán" && order.CancelReason == "")
            {
                foreach (var item in order.OrderDetails)
                {
                    item.ProductDetail.Quantity += item.Quantity;
                }
            }

            if (model.OrderStatus == "Đã hủy" && order.PaymentStatus == "Đã thanh toán")
            {
                foreach (var item in order.OrderDetails)
                {
                    order.PaymentStatus = "Chờ hoàn tiền";
                    item.ProductDetail.Quantity += item.Quantity;
                }
            }

            order.OrderStatus = model.OrderStatus;
            order.CancelReason = model.CancelReason;
            var Eid = _context.Employees
                .Where(e => e.AccountId == currentEmployee.Id)
                .Select(e => e.EmployeeId)
                .FirstOrDefault();
            order.EmployeeId = Eid;

            if (model.OrderStatus == "Đã hoàn thành" && !order.CompletedDate.HasValue)
            {
                order.CompletedDate = DateTime.Now;
                if (order.PaymentStatus == "Chưa thanh toán")
                {
                    order.PaymentStatus = "Đã thanh toán";
                    order.PaymentDate = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thay đổi trạng thái đơn hàng thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var order = _context.Orders.Find(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                _context.SaveChanges();
            }
            TempData["Success"] = "Xóa đơn hàng thành công!";
            return RedirectToAction("Index");
        }
        public static (string Address, string Ward, string District, string Province) SplitAddress(string fullAddress)
        {
            // Giả sử bạn có địa chỉ theo định dạng: "Số nhà, Đường, Phường, Quận, Tỉnh"
            var parts = fullAddress?.Split(',') ?? new string[0];

            // Tạo giá trị mặc định nếu không có đủ phần
            string address = parts.Length > 0 ? parts[0].Trim() : string.Empty;
            string ward = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            string district = parts.Length > 2 ? parts[2].Trim() : string.Empty;
            string province = parts.Length > 3 ? parts[3].Trim() : string.Empty;

            return (address, ward, district, province);
        }


        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            keyword = keyword?.Trim();

            // Retrieve all orders from the database first
            var orders = await _context.Orders.ToListAsync();

            // Normalize the keyword (remove accents and convert to lowercase)
            string normalizedKeyword = keyword?.Unidecode().ToLower();

            // Filter the orders in memory (after retrieving them)
            var filteredOrders = orders.Where(o =>
                o.Name?.Unidecode().ToLower().Contains(normalizedKeyword) == true ||
                o.Phone.Contains(normalizedKeyword)
            ).ToList();

            return Json(filteredOrders);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(int orderId, decimal discount, decimal shippingFee)
        {
            // Lấy thông tin đơn hàng từ database
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                            .ThenInclude(c => c.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Tách địa chỉ từ thông tin đơn hàng
            if (string.IsNullOrEmpty(order.Address))
            {
                order.Address = " ";
            }
            else
            {
                var (address, ward, district, province) = SplitAddress(order.Address);
                if (string.IsNullOrEmpty(province))
                {
                    province = "";
                }

                if (int.TryParse(province, out int tinhId))
                {
                    if (int.TryParse(district, out int quanId) && int.TryParse(ward, out int phuongId))
                    {
                        var fullAddress = await GetAddressFromApi(tinhId, quanId, phuongId);
                        order.Address = $"{address}, {fullAddress}";
                    }
                    else
                    {
                        order.Address = $"{address}, {province}";
                    }
                }
                else
                {
                    order.Address = $"{address}, {province}";
                }
            }

            // Lấy dữ liệu cho OrderViewModel
            var orderViewModel = new OrderPdfViewModel
            {
                OrderId = order.OrderId,
                CustomerName = order.Name,
                Phone = order.Phone,
                Address = order.Address, // Đặt lại địa chỉ từ API vào đây
                OrderDetails = order.OrderDetails.Select((od, index) =>
                {
                    decimal discountedUnitPrice;

                    if (discount >= 1000)
                    {
                        // Giảm trực tiếp theo số tiền
                        var totalItemDiscount = discount / order.OrderDetails.Sum(d => d.Quantity); // chia đều giảm cho từng sản phẩm
                        discountedUnitPrice = od.Price - totalItemDiscount;
                    }
                    else
                    {
                        // Giảm theo phần trăm
                        discountedUnitPrice = od.Price * (1 - discount / 100);
                    }

                    var totalPrice = discountedUnitPrice * od.Quantity;

                    return new OrderDetailPdfViewModel
                    {
                        Index = index + 1,
                        ProductCode = od.ProductDetail.Color.Product.ProductId,
                        ProductName = od.ProductDetail.Color.Product.Name,
                        Color = od.ProductDetail.Color.Name,
                        Size = od.ProductDetail.Size,
                        Quantity = od.Quantity,
                        UnitPrice = discountedUnitPrice,
                        TotalPrice = totalPrice
                    };
                }).ToList(),
                ShippingFee = shippingFee,
                TotalAmount = order.TotalFee,
                EmployeeName = _context.Employees
                    .Where(e => e.EmployeeId == order.EmployeeId)
                    .Select(e => e.Name)
                    .FirstOrDefault(),
                CreatedDate = order.CreatedDate
            };

            //Tạo PDF
            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdfDocument = new PdfDocument(writer);
                var document = new Document(pdfDocument);

                var fontRegular = PdfFontFactory.CreateFont("C:\\Windows\\Fonts\\Tahoma.ttf", PdfEncodings.IDENTITY_H);
                var fontBold = PdfFontFactory.CreateFont("C:\\Windows\\Fonts\\Tahoma.ttf", PdfEncodings.IDENTITY_H);


                // Tạo bảng có 2 cột bằng nhau
                var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 2 }))
                    .UseAllAvailableWidth()
                    .SetMarginBottom(10);

                // Ảnh bên trái
                var imagePath = "C:\\Users\\nguye\\source\\repos\\RuychWeb\\RuychWeb\\wwwroot\\images\\logo.jpg";
                var image = new Image(ImageDataFactory.Create(imagePath))
                    .SetWidth(80)
                    .SetHeight(80)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER);

                var imageCell = new Cell()
                    .Add(image)
                    .SetBorder(Border.NO_BORDER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE);
                headerTable.AddCell(imageCell);

                // Tạo Div chứa nội dung căn giữa toàn trang
                var storeInfoDiv = new Div()
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetWidth(UnitValue.CreatePercentValue(100)); // Chiếm toàn bộ chiều ngang

                storeInfoDiv
                    .Add(new Paragraph("Cửa hàng thời trang Ruych Studio").SetFont(fontRegular))
                    .Add(new Paragraph("Địa chỉ: 81 Tuệ Tĩnh - Hai Bà Trưng - Hà Nội").SetFont(fontRegular))
                    .Add(new Paragraph("Liên hệ: 037 903 4609").SetFont(fontRegular))
                    .Add(new Paragraph("Email: jeanruych@gmail.com").SetFont(fontRegular));

                var infoCell = new Cell()
                    .Add(storeInfoDiv)
                    .SetBorder(Border.NO_BORDER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE);
                headerTable.AddCell(infoCell);
                document.Add(headerTable);
                document.Add(new Paragraph("\n"));

                // Tiêu đề hóa đơn
                document.Add(new Paragraph("HÓA ĐƠN BÁN LẺ")
                .SetFont(fontBold).SetFontSize(20).SetTextAlignment(TextAlignment.CENTER));

                // Căn giữa mã hóa đơn
                document.Add(new Paragraph($"Mã hóa đơn: {orderViewModel.OrderId}")
                    .SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("\n"));

                // Thông tin khách hàng
                document.Add(new Paragraph($"Họ tên khách hàng: {orderViewModel.CustomerName}                        SĐT: {orderViewModel.Phone}")
                    .SetFont(fontRegular));
                document.Add(new Paragraph($"Địa chỉ: {orderViewModel.Address}")
                    .SetFont(fontRegular));

                // Đảm bảo font có hỗ trợ tiếng Việt
                var table = new Table(8).UseAllAvailableWidth();

                // Thêm header cho bảng với font đã được định nghĩa
                table.AddHeaderCell(new Cell().Add(new Paragraph("Số TT").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Mã sản phẩm").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Tên sản phẩm").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Màu sắc").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Kích thước").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Số lượng").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Đơn giá").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Thành tiền").SetFont(fontRegular).SetTextAlignment(TextAlignment.CENTER)));
                foreach (var detail in orderViewModel.OrderDetails)
                {
                    table.AddCell(new Cell().Add(new Paragraph(detail.Index.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(detail.ProductCode.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(detail.ProductName).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(detail.Color).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(detail.Size).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(detail.Quantity.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph($"{detail.UnitPrice:N0} VND").SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph($"{detail.TotalPrice:N0} VND").SetTextAlignment(TextAlignment.CENTER)));
                }

                table.AddCell(new Cell(1, 2).Add(new Paragraph("Phí ship").SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER)));
                table.AddCell(new Cell(1, 5).Add(new Paragraph("").SetTextAlignment(TextAlignment.CENTER)));  // Các cột còn lại không có nội dung
                table.AddCell(new Cell().Add(new Paragraph($"{orderViewModel.ShippingFee:N0} VND").SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER)));

                table.AddCell(new Cell(1, 2).Add(new Paragraph("CỘNG").SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER)));
                table.AddCell(new Cell(1, 5).Add(new Paragraph("").SetTextAlignment(TextAlignment.CENTER)));  // Các cột còn lại không có nội dung
                table.AddCell(new Cell().Add(new Paragraph($"{orderViewModel.TotalAmount:N0} VND").SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER)));
                document.Add(table);
                document.Add(new Paragraph("\n"));

                // Tổng tiền

                var totalAmountInWords = ConvertNumberToWords((long)orderViewModel.TotalAmount);

                document.Add(new Paragraph($"Cộng thành tiền (Viết bằng chữ): {totalAmountInWords} đồng")
                    .SetFont(fontRegular));

                // Ngày tháng năm góc phải
                var day = orderViewModel.CreatedDate.Value.Day;
                var month = orderViewModel.CreatedDate.Value.Month;
                var year = orderViewModel.CreatedDate.Value.Year;

                // Điền vào PDF
                document.Add(new Paragraph($"Ngày {day:00} tháng {month:00} năm {year}")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.RIGHT));

                // Người mua hàng (Người mua ký tên)
                var tableSignatures = new Table(2).UseAllAvailableWidth();


                // Người mua hàng (Ký, ghi rõ họ tên)
                var buyerCell = new Cell().Add(new Paragraph("Người mua hàng")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.LEFT));
                buyerCell.SetBorder(Border.NO_BORDER);

                // "Ký, ghi rõ họ tên" cho người mua hàng
                var buyerSignatureCell = new Cell().Add(new Paragraph("(Ký, ghi rõ họ tên)")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.LEFT));
                buyerSignatureCell.SetBorder(Border.NO_BORDER);

                // Người bán hàng (Ký, ghi rõ họ tên)
                var sellerCell = new Cell().Add(new Paragraph($"Người bán hàng: ")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.RIGHT));
                sellerCell.SetBorder(Border.NO_BORDER);

                // "Ký, ghi rõ họ tên" cho người bán hàng
                var sellerSignatureCell = new Cell().Add(new Paragraph("(Ký, ghi rõ họ tên)")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.RIGHT));
                sellerSignatureCell.SetBorder(Border.NO_BORDER);

                var buyerSignatureCell1 = new Cell().Add(new Paragraph("")
                   .SetFont(fontRegular).SetTextAlignment(TextAlignment.LEFT));
                buyerSignatureCell1.SetBorder(Border.NO_BORDER);

                var sellerSignatureCell1 = new Cell().Add(new Paragraph($"{orderViewModel.EmployeeName}")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.RIGHT));
                sellerSignatureCell1.SetBorder(Border.NO_BORDER);
                // Thêm các ô vào bảng
                tableSignatures.AddCell(buyerCell);
                tableSignatures.AddCell(sellerCell);
                tableSignatures.AddCell(buyerSignatureCell);
                tableSignatures.AddCell(sellerSignatureCell);
                tableSignatures.AddCell(buyerSignatureCell1);
                tableSignatures.AddCell(sellerSignatureCell1);

                // Thêm bảng vào PDF
                document.Add(tableSignatures);
                document.Add(new Paragraph("\n\n"));

                document.Close();
                var byteArray = memoryStream.ToArray();
                return File(byteArray, "application/pdf", $"Order_{orderViewModel.OrderId}.pdf");
            }
        }

        // Hàm lấy địa chỉ từ API
        public async Task<string> GetAddressFromApi(int tinhId, int quanId, int phuongId)
        {
            // Lấy Tỉnh
            var tinhRes = await _httpClient.GetStringAsync($"https://esgoo.net/api-tinhthanh/1/0.htm");
            var tinhData = JsonConvert.DeserializeObject<ApiResponse>(tinhRes);
            var tinh = tinhData?.Data?.FirstOrDefault(t => t.Id == tinhId);
            Console.WriteLine($"Tỉnh ID {tinhId} -> Tên: {tinh?.Name}");

            // Lấy Quận/Huyện
            var quanRes = await _httpClient.GetStringAsync($"https://esgoo.net/api-tinhthanh/2/0{tinhId}.htm");
            var quanData = JsonConvert.DeserializeObject<ApiResponse>(quanRes);
            var quan = quanData?.Data?.FirstOrDefault(q => q.Id == quanId);
            Console.WriteLine($"Quận/Huyện ID {quanId} -> Tên: {quan?.Name}");

            // Lấy Phường/Xã
            var phuongRes = await _httpClient.GetStringAsync($"https://esgoo.net/api-tinhthanh/3/{quanId}.htm");
            var phuongData = JsonConvert.DeserializeObject<ApiResponse>(phuongRes);
            var phuong = phuongData?.Data?.FirstOrDefault(p => p.Id == phuongId);
            Console.WriteLine($"Phường/Xã ID {phuongId} -> Tên: {phuong?.Name}");

            // Debug ra màn hình
            Console.WriteLine($"Tỉnh: {tinh?.Name}, Quận: {quan?.Name}, Phường: {phuong?.Name}");

            // Nếu thiếu thông tin quận hoặc phường thì chỉ hiện những cái có
            var parts = new List<string>();
            if (phuong != null) parts.Add(phuong.Name);
            if (quan != null) parts.Add(quan.Name);
            if (tinh != null) parts.Add(tinh.Name);

            return string.Join(", ", parts);
        }

        // Định nghĩa kiểu dữ liệu cho API response
        public class ApiResponse
        {
            public int Error { get; set; }
            public string ErrorText { get; set; }
            public List<Location> Data { get; set; }
        }

        public class Location
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public static string ConvertNumberToWords(long number)
        {
            if (number == 0) return "Không đồng";

            string[] ones = { "", "Một", "Hai", "Ba", "Bốn", "Năm", "Sáu", "Bảy", "Tám", "Chín" };
            string[] tens = { "", "Mười", "Hai mươi", "Ba mươi", "Bốn mươi", "Năm mươi", "Sáu mươi", "Bảy mươi", "Tám mươi", "Chín mươi" };
            string[] hundreds = { "", "Một trăm", "Hai trăm", "Ba trăm", "Bốn trăm", "Năm trăm", "Sáu trăm", "Bảy trăm", "Tám trăm", "Chín trăm" };

            string[] units = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };

            List<string> parts = new List<string>();

            int unitIndex = 0;
            while (number > 0)
            {
                int part = (int)(number % 1000);
                if (part > 0)
                {
                    string partString = ConvertPartToWords(part, ones, tens, hundreds);
                    parts.Insert(0, partString + " " + units[unitIndex]);
                }
                number /= 1000;
                unitIndex++;
            }

            return string.Join(" ", parts).Trim();
        }

        private static string ConvertPartToWords(int part, string[] ones, string[] tens, string[] hundreds)
        {
            int hundred = part / 100;
            int ten = (part % 100) / 10;
            int one = part % 10;

            string result = "";

            if (hundred > 0) result += hundreds[hundred] + " ";
            if (ten > 1) result += tens[ten] + " ";
            else if (ten == 1) result += "Mười ";

            if (one > 0) result += ones[one];
            else if (ten == 0 && hundred > 0) result += "Lẻ";

            return result.Trim();
        }

    }
}
