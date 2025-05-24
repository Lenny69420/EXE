using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuatBook.Models;
using QuatBook.Dto;
using Microsoft.AspNetCore.Http;
using QuatBook.Helpers;
using QuatBook.Filters;


namespace QuatBook.Controllers
{
    public class ManagerController : Controller
    {
        private readonly QuatBookContext _context;

        public ManagerController(QuatBookContext context)
        {
            _context = context;
        }

        // GET: Manager/Shop (Dashboard) - Chỉ dành cho Admin
        [AdminOnlyFilter]
        public IActionResult Shop()
        {

            var categories = _context.Categories.ToList();
            ViewBag.Categories = categories ?? new List<Category>();

            var authors = _context.Authors.ToList();
            ViewBag.Authors = authors ?? new List<Author>();

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Author)
                .ToList();

            return View(products);
        }


        #region Profile 
        // GET: Manager/Profile
        [UserAccessFilter]
        public async Task<IActionResult> Profile()
        {
            int userId = (int)HttpContext.Items["UserId"];

            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.AccountId == userId);

            if (user == null)
            {
                return Redirect("/404");
            }

            var profileDTO = new ProfileDTO
            {
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                RoleId = user.RoleId,
                Gender = user.Gender,
                Birth = user.Birth,
                Image = user.Image
            };

            return View(profileDTO);
        }

        // GET: Manager/EditProfile (Load form vào modal)
        [HttpGet]
        [UserAccessFilter]
        public async Task<IActionResult> EditProfile()
        {
            int userId = (int)HttpContext.Items["UserId"];

            var user = await _context.Accounts
                .FirstOrDefaultAsync(u => u.AccountId == userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var profileDTO = new ProfileDTO
            {
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                RoleId = user.RoleId,
                Gender = user.Gender,
                Birth = user.Birth,
                Image = user.Image
            };

            return PartialView("_EditProfileModal", profileDTO);
        }

        // POST: Manager/EditProfile (Xử lý submit form từ modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccessFilter]
        public async Task<IActionResult> EditProfile(ProfileDTO profileDTO, IFormFile ImageFile)
        {
            int userId = (int)HttpContext.Items["UserId"];

            // Kiểm tra ModelState, nhưng bỏ qua validation cho ImageFile
            if (!ModelState.IsValid)
            {
                // Loại bỏ lỗi liên quan đến ImageFile nếu có
                if (ModelState.ContainsKey("ImageFile"))
                {
                    ModelState["ImageFile"].Errors.Clear();
                    ModelState["ImageFile"].ValidationState = Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid;
                }

                // Kiểm tra lại ModelState sau khi bỏ qua ImageFile
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }
            }

            try
            {
                var existingUser = await _context.Accounts
                    .FirstOrDefaultAsync(u => u.AccountId == userId);

                if (existingUser == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Cập nhật thông tin
                existingUser.Username = profileDTO.Username;
                existingUser.Email = profileDTO.Email;
                existingUser.Phone = profileDTO.Phone;
                existingUser.Address = profileDTO.Address;
                existingUser.Gender = (bool)profileDTO.Gender;
                existingUser.Birth = profileDTO.Birth;

                // Xử lý upload ảnh (không bắt buộc)
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    existingUser.Image = UploadImage.UploadHinh(ImageFile, "profile");
                }
                // Nếu không upload ảnh mới, giữ nguyên ảnh cũ (không cần làm gì vì Image không thay đổi)

                _context.Accounts.Update(existingUser);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("Username", existingUser.Username);

                return Json(new { success = true, message = "Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating profile: " + ex.Message });
            }
        }
        #endregion
        // GET: Manager/Orders - Hiển thị trang quản lý đơn hàng
        [UserAccessFilter]
        public async Task<IActionResult> Orders()
        {
            int userId = (int)HttpContext.Items["UserId"];
            int? roleId = HttpContext.Session.GetInt32("RoleId");

            // Lấy danh sách đơn hàng
            IQueryable<Order> ordersQuery = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product) // Bao gồm thông tin sản phẩm trong chi tiết đơn hàng
                .Include(o => o.Account); // Bao gồm thông tin người dùng

            // Nếu là User (RoleId = 2), chỉ hiển thị đơn hàng của họ
            if (roleId == 2)
            {
                ordersQuery = ordersQuery.Where(o => o.AccountId == userId);
            }
            // Nếu là Admin (RoleId = 1), hiển thị tất cả đơn hàng

            var orders = await ordersQuery
                .OrderByDescending(o => o.CreateTime) // Sắp xếp theo thời gian tạo (mới nhất trước)
                .ToListAsync();

            return View(orders);
        }

        // GET: Manager/OrderDetails - Trả về chi tiết đơn hàng cho modal
        [HttpGet]
        [UserAccessFilter]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            int userId = (int)HttpContext.Items["UserId"];
            int? roleId = HttpContext.Session.GetInt32("RoleId");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Account)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null || (roleId == 2 && order.AccountId != userId))
            {
                return NotFound();
            }

            return PartialView("_OrderDetails", order);
        }


        // GET: Manager/DeleteOrder
        [HttpGet]
        [AdminOnlyFilter]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Xóa các OrderDetails liên quan trước
            _context.OrderDetails.RemoveRange(order.OrderDetails);
            // Xóa Order
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }
        // GET: Manager/Statistics
        [AdminOnlyFilter]
        public async Task<IActionResult> Statistics()
        {
            // Thống kê số lượng đơn hàng theo trạng thái
            var orderStatusStats = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Thống kê doanh thu theo ngày (7 ngày gần nhất)
            var startDate = DateTime.Now.AddDays(-6); // 7 ngày tính từ hôm nay
            var revenueStats = await _context.Orders
                .Where(o => o.CreateTime != null && o.CreateTime >= startDate && o.Status == "Paid")
                .GroupBy(o => o.CreateTime.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(o => o.Ammount)
                })
                .OrderBy(g => g.Date)
                .ToListAsync();


            // Thống kê top 5 sản phẩm bán chạy nhất
            var topProducts = await _context.OrderDetails
                .GroupBy(od => od.Product)
                .Select(g => new
                {
                    ProductId = g.Key.BookId,
                    ProductName = g.Key.BookName,
                    ProductImage = g.Key.Image,
                    TotalQuantity = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * g.Key.Price.GetValueOrDefault()) // Lấy Price từ Product
                })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(5)
                .ToListAsync();

            // Thống kê top 5 người dùng mua hàng nhiều nhất
            var topUsers = await _context.Orders
                .Where(o => o.Status == "Paid") // Chỉ tính đơn hàng đã thanh toán
                .GroupBy(o => o.Account)
                .Select(g => new
                {
                    UserId = g.Key.AccountId,
                    Username = g.Key.Username,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.Ammount)
                })
                .OrderByDescending(g => g.TotalSpent)
                .Take(5)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho biểu đồ
            ViewBag.OrderStatusLabels = orderStatusStats.Select(s => s.Status).ToList();
            ViewBag.OrderStatusData = orderStatusStats.Select(s => s.Count).ToList();
            ViewBag.OrderStatusTotal = orderStatusStats.Sum(s => s.Count); // Calculate sum here

            ViewBag.RevenueLabels = revenueStats.Select(r => r.Date.ToString("dd/MM/yyyy")).ToList();
            ViewBag.RevenueData = revenueStats.Select(r => r.TotalRevenue).ToList();
            ViewBag.RevenueTotal = revenueStats.Sum(r => r.TotalRevenue); // Calculate sum here


            ViewBag.TopProducts = topProducts;
            ViewBag.TopUsers = topUsers;
            return View();
        }


        #region Category
        // GET: Manager/Categories - Hiển thị trang quản lý danh mục
        [AdminOnlyFilter]
        public IActionResult Categories()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int? roleId = HttpContext.Session.GetInt32("RoleId");

            if (!userId.HasValue || roleId != 1) // Chỉ Admin mới có quyền xóa
            {
                return RedirectToAction("Login", "Account");
            }
            var categories = _context.Categories.ToList();
            return View(categories);
        }
        // GET: Manager/CreateCategory - Load form tạo danh mục (Modal)
        [HttpGet]
        [AdminOnlyFilter]
        public IActionResult CreateCategory()
        {
            return PartialView("_CreateCategoryModal", new Category());
        }

        // POST: Manager/CreateCategory - Xử lý tạo danh mục mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnlyFilter]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Danh mục đã được thêm thành công!";
                    return RedirectToAction("Categories");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi thêm danh mục: " + ex.Message;
                    return RedirectToAction("Categories");
                }
            }
            return View(category); // Trả về view nếu dữ liệu không hợp lệ
        }

        // GET: Manager/EditCategory - Load form chỉnh sửa danh mục (Modal)
        [HttpGet]
        [AdminOnlyFilter]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Danh mục không tồn tại." });
            }

            return PartialView("_EditCategoryModal", category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnlyFilter]
        public async Task<IActionResult> EditCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingCategory = await _context.Categories.FindAsync(category.CategoryId);
                    if (existingCategory == null)
                    {
                        TempData["Error"] = "Danh mục không tồn tại.";
                        return RedirectToAction("Categories");
                    }

                    existingCategory.CategoryName = category.CategoryName;
                    existingCategory.CategoryDescription = category.CategoryDescription;

                    _context.Categories.Update(existingCategory);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Danh mục đã được cập nhật thành công!";
                    return RedirectToAction("Categories"); // Chuyển hướng về trang danh sách
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi cập nhật danh mục: " + ex.Message;
                    return RedirectToAction("Categories");
                }
            }

            // Nếu ModelState không hợp lệ, trả về lại modal với dữ liệu hiện tại
            return PartialView("_EditCategoryModal", category);
        }

        // GET: Manager/DeleteCategory - Xử lý xóa danh mục
        [HttpGet]
        [AdminOnlyFilter]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                TempData["Error"] = "Danh mục không tồn tại.";
                return RedirectToAction("Categories");
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục này vì nó đang chứa sản phẩm.";
                return RedirectToAction("Categories");
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Danh mục đã được xóa thành công.";
                return RedirectToAction("Categories");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa danh mục: " + ex.Message;
                return RedirectToAction("Categories");
            }
        }
        #endregion
    }
}