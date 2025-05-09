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
using RuychWeb.Areas.Admin.Models;
using RuychWeb.Models.Domain;
using RuychWeb.Models.DTO;
using RuychWeb.Repository;

namespace RuychWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ReceiptController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<ReceiptController> _logger;
        private readonly UserManager<Account> _userManager;
        public ReceiptController(DataContext context, ILogger<ReceiptController> logger, UserManager<Account> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string keyword = "")
        {
            var receiptsQuery = _context.Receipts
                .Include(r => r.Employee)
                .Include(r => r.Supplier)
                .Select(r => new ReceiptViewModel
                {
                    ReceiptId = r.ReceiptId,
                    EmployeeName = r.Employee.Name,
                    SupplierName = r.Supplier.Name,
                    CreatedDate = r.CreatedDate,
                    // Dữ liệu ReceiptItems cần được truy vấn và tạo tại đây
                    ReceiptItems = r.ReceiptDetails.Select(rd => new ReceiptViewModel.ReceiptItem
                    {
                        ProductName = rd.ProductDetail.Color.Product.Name,
                        Size = rd.ProductDetail.Size,
                        Color = rd.ProductDetail.Color.Name,
                        Quantity = rd.Quantity,
                        Price = rd.Price
                    }).ToList()
                })
                .Where(r => string.IsNullOrEmpty(keyword) || r.EmployeeName.Contains(keyword) || r.SupplierName.Contains(keyword))
                .OrderByDescending(r => r.ReceiptId);

            var totalReceipts = await receiptsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalReceipts / (double)pageSize);

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var receipts = await receiptsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(receipts);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var receipt = await _context.Receipts
                .Include(r => r.Employee)
                .Include(r => r.Supplier)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(rd => rd.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(rd => rd.ProductDetail)
                        .ThenInclude(pd => pd.Color.Product)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
            {
                return NotFound();
            }

            var receiptViewModel = new ReceiptViewModel
            {
                ReceiptId = receipt.ReceiptId,
                EmployeeName = receipt.Employee.Name,
                SupplierName = receipt.Supplier.Name,
                CreatedDate = receipt.CreatedDate,
                ReceiptItems = receipt.ReceiptDetails.Select(rd => new ReceiptViewModel.ReceiptItem
                {
                    ProductName = rd.ProductDetail.Color.Product.Name,
                    Size = rd.ProductDetail.Size,
                    Color = rd.ProductDetail.Color.Name,
                    Quantity = rd.Quantity,
                    Price = rd.Price
                }).ToList()
            };

            return View(receiptViewModel);
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

            ViewBag.Suppliers = new SelectList(_context.Suppliers, "SupplierId", "Name");
            ViewBag.Products = new SelectList(_context.Products, "ProductId", "Name");
            ViewBag.Colors = new List<string> { "Black", "White", "Gray", "Brown", "Blue", "Red", "Green", "Purple", "Pink" };
            ViewBag.Sizes = new List<string> { "S", "M", "L", "XL", "XXL" };

            return View(new ReceiptViewModel
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.Name
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ReceiptViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = new SelectList(_context.Suppliers, "SupplierId", "Name");
                ViewBag.Products = new SelectList(_context.Products, "ProductId", "Name");
                ViewBag.Colors = new List<string> { "Blue", "Red", "Green", "Purple", "Pink", "Gray", "Brown", "Black", "White" };
                ViewBag.Sizes = new List<string> { "S", "M", "L", "XL", "XXL" };

                return View(model);
            }

            var receipt = new Receipt
            {
                CreatedDate = model.CreatedDate,
                EmployeeId = model.EmployeeId,
                SupplierId = model.SupplierId,
                ReceiptDetails = new List<ReceiptDetail>()
            };

            foreach (var item in model.ReceiptItems)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product != null)
                {
                    if (product.Price == null || product.Price == 0 || product.Price < item.Price)
                    {
                        product.Price = item.Price;
                    }
                }
                var color = await _context.Colors
                    .FirstOrDefaultAsync(c => c.Name == item.Color && c.ProductId == item.ProductId);
                if (color == null)
                {
                    color = new Color
                    {
                        Name = item.Color,
                        ProductId = item.ProductId
                    };
                    _context.Colors.Add(color);
                    await _context.SaveChangesAsync();
                }

                // 2. Tìm hoặc tạo ProductDetail
                var productDetail = await _context.ProductDetails
                    .FirstOrDefaultAsync(pd => pd.ColorId == color.ColorId && pd.Size == item.Size);

                if (productDetail == null)
                {
                    productDetail = new ProductDetail
                    {
                        Size = item.Size,
                        Quantity = 0,
                        ColorId = color.ColorId
                    };
                    _context.ProductDetails.Add(productDetail);
                    await _context.SaveChangesAsync();
                }

                // 3. Cập nhật tồn kho
                productDetail.Quantity += item.Quantity;

                // 4. Tạo ReceiptDetail
                var receiptDetail = new ReceiptDetail
                {
                    ProductDetailId = productDetail.ProductDetailId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                receipt.ReceiptDetails.Add(receiptDetail);
            }

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo đơn nhập hàng thành công!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var receipt = await _context.Receipts
                .Include(r => r.Supplier)
                .Include(r => r.Employee)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(rd => rd.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(rd => rd.ProductDetail)
                        .ThenInclude(pd => pd.Color.Product)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
                return NotFound();

            var model = new ReceiptViewModel
            {
                ReceiptId = receipt.ReceiptId,
                SupplierId = receipt.SupplierId,
                EmployeeId = receipt.EmployeeId,
                CreatedDate = receipt.CreatedDate,
                ReceiptItems = receipt.ReceiptDetails.Select(rd => new ReceiptViewModel.ReceiptItem
                {
                    ProductId = rd.ProductDetail.Color.ProductId,
                    ProductName = rd.ProductDetail.Color.Product.Name,
                    Color = rd.ProductDetail.Color.Name,
                    Size = rd.ProductDetail.Size,
                    Quantity = rd.Quantity,
                    Price = rd.Price
                }).ToList()
            };

            ViewBag.Suppliers = new SelectList(_context.Suppliers, "SupplierId", "Name", receipt.SupplierId);
            ViewBag.Products = new SelectList(_context.Products, "ProductId", "Name");
            ViewBag.Colors = new List<string> { "Blue", "Red", "Green", "Purple", "Pink", "Gray", "Brown", "Black", "White" };
            ViewBag.Sizes = new List<string> { "S", "M", "L", "XL", "XXL" };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ReceiptViewModel model)
        {
            var receipt = await _context.Receipts
                .Include(r => r.ReceiptDetails)
                .ThenInclude(rd => rd.ProductDetail)
                .FirstOrDefaultAsync(r => r.ReceiptId == model.ReceiptId);

            if (receipt == null)
                return NotFound();
            var endOfDay = receipt.CreatedDate.Date.AddDays(1); // Thời điểm 00:00 của ngày hôm sau
            if (DateTime.Now >= endOfDay)
            {
                TempData["Failed"] = "Chỉ có thể sửa đơn hàng trong ngày.";
                return RedirectToAction("Index");
            }
            // Cập nhật thông tin Receipt
            receipt.SupplierId = model.SupplierId;
            receipt.EmployeeId = model.EmployeeId;
            receipt.CreatedDate = model.CreatedDate;

            // Xoá ReceiptDetails cũ và rollback tồn kho
            foreach (var oldDetail in receipt.ReceiptDetails)
            {
                var productDetail = await _context.ProductDetails
                    .FirstOrDefaultAsync(pd => pd.ProductDetailId == oldDetail.ProductDetailId);
                if (productDetail != null)
                {
                    productDetail.Quantity -= oldDetail.Quantity;
                }
            }
            _context.ReceiptDetails.RemoveRange(receipt.ReceiptDetails);
            receipt.ReceiptDetails.Clear();

            // Thêm lại chi tiết mới
            foreach (var item in model.ReceiptItems)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product != null)
                {
                    if (product.Price == null || product.Price == 0 || product.Price < item.Price)
                    {
                        product.Price = item.Price;
                    }
                }
                var color = await _context.Colors
                    .FirstOrDefaultAsync(c => c.Name == item.Color && c.ProductId == item.ProductId);
                if (color == null)
                {
                    color = new Color
                    {
                        Name = item.Color,
                        ProductId = item.ProductId
                    };
                    _context.Colors.Add(color);
                    await _context.SaveChangesAsync();
                }

                var productDetail = await _context.ProductDetails
                    .FirstOrDefaultAsync(pd => pd.ColorId == color.ColorId && pd.Size == item.Size);
                if (productDetail == null)
                {
                    productDetail = new ProductDetail
                    {
                        Size = item.Size,
                        Quantity = 0,
                        ColorId = color.ColorId
                    };
                    _context.ProductDetails.Add(productDetail);
                    await _context.SaveChangesAsync();
                }

                productDetail.Quantity += item.Quantity;

                var receiptDetail = new ReceiptDetail
                {
                    ProductDetailId = productDetail.ProductDetailId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                receipt.ReceiptDetails.Add(receiptDetail);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thay đổi thông tin đơn nhập hàng thành công!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ExportReceiptToPdf(int id)
        {
            // Lấy thông tin phiếu nhập từ cơ sở dữ liệu
            var receipt = await _context.Receipts
                .Include(r => r.Employee)
                .Include(r => r.Supplier)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(rd => rd.ProductDetail)
                        .ThenInclude(pd => pd.Color)
                .Include(r => r.ReceiptDetails)
                    .ThenInclude(rd => rd.ProductDetail)
                        .ThenInclude(pd => pd.Color.Product)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
            {
                return NotFound();
            }

            // Tạo PDF
            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdfDocument = new PdfDocument(writer);
                var document = new Document(pdfDocument);

                var fontRegular = PdfFontFactory.CreateFont("C:\\Windows\\Fonts\\Tahoma.ttf", PdfEncodings.IDENTITY_H);
                var fontBold = PdfFontFactory.CreateFont("C:\\Windows\\Fonts\\Tahoma.ttf", PdfEncodings.IDENTITY_H);

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


                // Tiêu đề
                document.Add(new Paragraph("HÓA ĐƠN NHẬP HÀNG")
                    .SetFont(fontBold).SetFontSize(20).SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Mã hóa đơn: {receipt.ReceiptId}")
                   .SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph("\n"));

                document.Add(new Paragraph($"Nhà cung cấp: {receipt.Supplier.Name}                            SĐT: {receipt.Supplier.Phone}")
                    .SetFont(fontRegular));

                document.Add(new Paragraph($"Địa chỉ: {receipt.Supplier.Address}")
                    .SetFont(fontRegular));

                document.Add(new Paragraph("\n"));

                // Bảng chi tiết sản phẩm
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

                int index = 1;
                foreach (var item in receipt.ReceiptDetails)
                {
                    table.AddCell(new Cell().Add(new Paragraph(index.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(item.ProductDetail.Color.Product.ProductId.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(item.ProductDetail.Color.Product.Name).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(item.ProductDetail.Color.Name).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(item.ProductDetail.Size).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString()).SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph($"{item.Price:N0} VND").SetTextAlignment(TextAlignment.CENTER)));
                    table.AddCell(new Cell().Add(new Paragraph($"{(item.Quantity * item.Price):N0} VND").SetTextAlignment(TextAlignment.CENTER)));
                    index++;
                }
                var totalAmount = receipt.ReceiptDetails.Sum(rd => rd.Quantity * rd.Price);
                table.AddCell(new Cell(1, 2).Add(new Paragraph("CỘNG").SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER)));
                table.AddCell(new Cell(1, 5).Add(new Paragraph("").SetTextAlignment(TextAlignment.CENTER)));  // Các cột còn lại không có nội dung
                table.AddCell(new Cell().Add(new Paragraph($"{totalAmount:N0} VND").SetFont(fontBold).SetTextAlignment(TextAlignment.CENTER)));
                document.Add(table);

                document.Add(new Paragraph("\n"));

                // Tổng cộng
                var totalAmountInWords = ConvertNumberToWords((long)totalAmount);

                document.Add(new Paragraph($"Cộng thành tiền (Viết bằng chữ): {totalAmountInWords} đồng")
                .SetFont(fontRegular));


                var day = receipt.CreatedDate.Day;
                var month = receipt.CreatedDate.Month;
                var year = receipt.CreatedDate.Year;

                document.Add(new Paragraph($"Ngày {day:00} tháng {month:00} năm {year}")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.RIGHT));

                // Chữ ký
                document.Add(new Paragraph("Người lập")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.RIGHT));
                document.Add(new Paragraph($"{receipt.Employee.Name}")
                    .SetFont(fontRegular).SetTextAlignment(TextAlignment.RIGHT));
                // Đóng và trả về PDF
                document.Close();
                var byteArray = memoryStream.ToArray();
                return File(byteArray, "application/pdf", $"Receipt_{receipt.ReceiptId}.pdf");
            }

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

