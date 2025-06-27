using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuatBook.Helpers;
using QuatBook.Models;

namespace QuatBook.Controllers
{
    public class BlogController : Controller
    {

        public QuatBookContext _context;

        public BlogController(QuatBookContext context)
        {
            _context = context;
        }
        public IActionResult BlogList()
        {
            // Lấy danh sách blog từ database
            var blogs = _context.Blogs.Where(b => b.Title != null && !b.Title.StartsWith("Thắc mắc")).ToList();

            // Truyền danh sách blog vào ViewBag để sử dụng trong View
            ViewBag.Blogs = blogs;

            return View();
        }

        public IActionResult BlogQNAList()
        {
            // Lấy danh sách blog từ database
            var blogs = _context.Blogs.Where(b => b.Title != null && b.Title.StartsWith("Thắc mắc")).ToList();

            // Truyền danh sách blog vào ViewBag để sử dụng trong View
            ViewBag.Blogs = blogs;

            return View();
        }

        public IActionResult BlogDetail(int id)
        {
            // Lấy thông tin blog theo id
            var blog = _context.Blogs.FirstOrDefault(b => b.Id == id);
            if (blog == null)
            {
                // Nếu không tìm thấy blog, trả về trang 404
                return NotFound();
            }
            // Truyền thông tin blog vào ViewBag để sử dụng trong View
            ViewBag.Blog = blog;

            return View();
        }

        public async Task<IActionResult> AddBlog(Blog blog, IFormFile ImageFile)
        {

            try
            {

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    blog.Image = await UploadImage.ConvertToByteArrayAsync(ImageFile);
                }



                // ✅ Thêm sản phẩm vào database
                _context.Blogs.Add(blog);
                await _context.SaveChangesAsync();

                // Gửi thông báo SignalR
                //await _hubContext.Clients.All.SendAsync("RefreshProducts");

                TempData["SuccessMessage"] = "Blog added successfully!";
                return RedirectToAction("Blog", "Manager");



            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding book: " + ex.Message + " | StackTrace: " + ex.StackTrace;
                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Authors = await _context.Authors.ToListAsync();
                return View("Index");
            }
            // Tạo một đối tượng Blog mới để truyền vào View
       
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var checkId = _context.Blogs.FirstOrDefault(i => i.Id == id);
            if (checkId == null)
            {
                TempData["Message"] = $"Blog not found have id: {id}";
                return Redirect("/404");
            }
            else
            {
                _context.Blogs.Remove(checkId);
                await _context.SaveChangesAsync();


                // Gửi thông báo SignalR
               // await _hubContext.Clients.All.SendAsync("RefreshProducts");

            }
            return RedirectToAction("Blogs", "Manager");
        }
    }
}
