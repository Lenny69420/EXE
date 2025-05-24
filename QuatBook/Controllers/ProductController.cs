using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuatBook.Dto;
using QuatBook.Models;
using X.PagedList.Extensions;
using X.PagedList;
using QuatBook.Helpers;
using Microsoft.AspNetCore.SignalR;
using QuatBook.Hubs;
using QuatBook.Filters;

namespace QuatBook.Controllers
{
    public class ProductController : Controller
    {
        private readonly QuatBookContext _context;
        private readonly IHubContext<ProductHub> _hubContext;

        public ProductController(QuatBookContext context, IHubContext<ProductHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }


        // GET: ProductController
        public ActionResult Index(int? categoryId, int? authorId, int page = 1, int pageSize = 12)
        {
            var products = _context.Products.AsQueryable();

            if (categoryId.HasValue) // Nếu categoryId tồn tại
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }
            if (authorId.HasValue) // Nếu authorId tồn tại
            {
                products = products.Where(p => p.AuthorId == authorId);
            }

            var result = products.Select(p => new ProductDTO
            {
                BookId = p.BookId,
                BookName = p.BookName,
                Image = p.Image ?? "",
                Price = p.Price ?? 0,
                CategoryName = p.Category.CategoryName
            }).ToList();
            var pagedResult = PaginateList(result, page, pageSize); // Gọi phương thức phân trang

            return View(pagedResult);
        }

        //Page
        private IPagedList<T> PaginateList<T>(List<T> items, int page, int pageSize)
        {
            return items.ToPagedList(page, pageSize);
        }

        //Search
        public ActionResult Search(string keyword, int page = 1, int pageSize = 12)
        {
            var products = _context.Products.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
            {
                products = products.Where(p => p.BookName.Contains(keyword));
            }
            var result = products.Select(p => new ProductDTO
            {
                BookId = p.BookId,
                BookName = p.BookName,
                Image = p.Image ?? "",
                Price = p.Price ?? 0,
                CategoryName = p.Category.CategoryName
            }).ToList();
            var pagedResult = PaginateList(result, page, pageSize); // Phân trang
            return View("Index", pagedResult);
        }

        // GET: ProductController/Details/5
        public ActionResult Detail(int bookId)
        {
            // Lấy thông tin chi tiết sản phẩm
            var data = _context.Products
                .Include(p => p.Category) // Lấy thêm category
                .Include(p => p.Author)   // Lấy thêm author
                .SingleOrDefault(p => p.BookId == bookId);

            if (data == null)
            {
                TempData["PopupMessage"] = $"Product not found have id: {bookId}";
                TempData["PopupType"] = "error";
                return Redirect("/404");
            }

            var result = new ProductDetailDTO
            {
                BookId = data.BookId,
                BookName = data.BookName,
                Image = data.Image ?? "",
                Description = data.Description ?? string.Empty,
                Price = data.Price ?? 0,
                Quantity = data.Quantity ?? 0,
                CategoryName = data.Category.CategoryName,
                AuthorName = data.Author?.AuthorName ?? "Unknown"
            };

            // Lấy danh sách sản phẩm bán chạy nhất (top 4)
            var bestSellingProducts = _context.OrderDetails
                .GroupBy(od => od.ProductId) // Nhóm theo ProductId
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalSold = g.Sum(od => od.Quantity) // Tính tổng số lượng bán ra
                })
                .OrderByDescending(g => g.TotalSold) // Sắp xếp theo số lượng bán ra giảm dần
                .Take(4) // Lấy top 4 sản phẩm
                .Join(_context.Products, // Kết hợp với bảng Products để lấy thông tin sản phẩm
                      od => od.ProductId,
                      p => p.BookId,
                      (od, p) => new ProductDTO
                      {
                          BookId = p.BookId,
                          BookName = p.BookName,
                          Image = p.Image ?? "",
                          Price = p.Price ?? 0,
                          CategoryName = p.Category.CategoryName
                      })
                .ToList();

            // Truyền danh sách sản phẩm bán chạy nhất vào ViewBag
            ViewBag.BestSellingProducts = bestSellingProducts;

            return View(result);
        }


        // API lấy danh sách sản phẩm active
        [HttpGet]
        public IActionResult GetActiveProducts()
        {
            var products = _context.Products
                .Where(p => p.active == true)
                .Select(p => new ProductDTO
                {
                    BookId = p.BookId,
                    BookName = p.BookName,
                    Image = p.Image ?? "",
                    Price = p.Price ?? 0,
                    CategoryName = p.Category.CategoryName
                })
                .ToList();
            return Json(products);
        }

        // POST: ProductController/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product, IFormFile ImageFile)
        {
            try
            {

                // Xử lý upload ảnh
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    product.Image = UploadImage.UploadHinh(ImageFile, "product");
                }


                // ✅ Thêm sản phẩm vào database
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Gửi thông báo SignalR
                await _hubContext.Clients.All.SendAsync("RefreshProducts");

                TempData["SuccessMessage"] = "Book added successfully!";
                return RedirectToAction("Shop", "Manager");



            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding book: " + ex.Message + " | StackTrace: " + ex.StackTrace;
                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Authors = await _context.Authors.ToListAsync();
                return View("Index");
            }
        }


        // GET: ProductController/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.BookId == id);

            if (product == null)
            {
                TempData["Message"] = $"Product not found with id: {id}";
                return Redirect("/404");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Authors = await _context.Authors.ToListAsync();

            return PartialView("_EditProductModal", product); // Trả về partial view với dữ liệu
        }

        // POST: ProductController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(Product product, IFormFile ImageFile)
        {
            try
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.BookId == product.BookId);

                if (existingProduct == null)
                {
                    TempData["ErrorMessage"] = "Product not found!";
                    return RedirectToAction("Shop", "Manager");
                }

                // Cập nhật các trường
                existingProduct.BookName = product.BookName;
                existingProduct.Description = product.Description;
                existingProduct.Quantity = product.Quantity;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.AuthorId = product.AuthorId;
                existingProduct.Created = product.Created;

                // Xử lý upload ảnh nếu có
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    existingProduct.Image = UploadImage.UploadHinh(ImageFile, "product");
                }

                _context.Products.Update(existingProduct);
                await _context.SaveChangesAsync();


                // Gửi thông báo SignalR
                await _hubContext.Clients.All.SendAsync("RefreshProducts");

                TempData["SuccessMessage"] = "Book updated successfully!";
                return RedirectToAction("Shop", "Manager");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating book: " + ex.Message;
                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Authors = await _context.Authors.ToListAsync();
                return View("_EditProductModal", product);
            }
        }

        // GET: ProductController/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var checkId = _context.Products.FirstOrDefault(i => i.BookId == id);
            if (checkId == null)
            {
                TempData["Message"] = $"Product not found have id: {id}";
                return Redirect("/404");
            }
            else
            {
                _context.Products.Remove(checkId);
                await _context.SaveChangesAsync();


                // Gửi thông báo SignalR
                await _hubContext.Clients.All.SendAsync("RefreshProducts");

            }
            return RedirectToAction("Shop", "Manager");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Toggle trạng thái bool
            product.active = !product.active.GetValueOrDefault(); // Nếu null thì mặc định false
            await _context.SaveChangesAsync();

            // Gửi thông báo SignalR
            await _hubContext.Clients.All.SendAsync("RefreshProducts");

            return Json(new
            {
                success = true,
                active = product.active,
                message = $"Product is now {(product.active == true ? "Active" : "Inactive")}"
            });
        }


    }
}
