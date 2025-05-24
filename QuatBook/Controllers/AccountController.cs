using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using QuatBook.Dto;
using QuatBook.Helpers;
using QuatBook.Models;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace QuatBook.Controllers
{
    public class AccountController : Controller
    {

        private readonly QuatBookContext _context;

        public AccountController(QuatBookContext context)
        {
            _context = context;
        }

        #region Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> LoginAsync(LoginDTO loginDTO)
        {
            try
            {

                if (ModelState.IsValid)
                {

                    var user = _context.Accounts.SingleOrDefault(u => u.Username == loginDTO.Username);
                    if (user == null)
                    {
                        ModelState.AddModelError("", "Account not exist.");
                        return View();
                    }
                    // Lưu thông tin người dùng vào session (hoặc token JWT nếu cần)
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetInt32("UserId", user.AccountId);
                    HttpContext.Session.SetInt32("RoleId", user.RoleId);

                    // So sánh mật khẩu đã nhập với mật khẩu băm trong database
                    if (!BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.Password))
                    {
                        ModelState.AddModelError("", "Password invalid.");
                        return View();
                    }

                    // Xác thực bằng cookie
                    var claims = new List<Claim>
                     {
                        new Claim(ClaimTypes.Name, user.Username),
                          new Claim("AccountId", user.AccountId.ToString()),

                      };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    return RedirectToAction("Index", "Product");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.");
            }

            return View();
        }

        #endregion

        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterDTO();
            return View(model);
        }
        [HttpPost]
        public IActionResult Register(RegisterDTO registerDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Kiểm tra xem Username đã tồn tại chưa
                    var existingUser = _context.Accounts.SingleOrDefault(u => u.Username == registerDTO.Username);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                        return View(registerDTO); // Trả về view với dữ liệu đã nhập để người dùng sửa
                    }
                    // Kiểm tra ngày sinh không được trong tương lai
                    if (registerDTO.Birth.HasValue && registerDTO.Birth.Value > DateTime.Now)
                    {
                        ModelState.AddModelError("Birth", "Ngày sinh không được là ngày trong tương lai.");
                        return View(registerDTO);
                    }
                    // Nếu không trùng, tạo tài khoản mới
                    var acc = new Account
                    {
                        Username = registerDTO.Username,
                        Password = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password),
                        Email = registerDTO.Email,
                        Phone = registerDTO.Phone,
                        Address = registerDTO.Address,
                        RoleId = 2, // Vai trò mặc định là 2 (User)
                        Gender = registerDTO.Gender ?? true, // Giá trị mặc định nếu null
                        Birth = registerDTO.Birth.HasValue ? DateOnly.FromDateTime(registerDTO.Birth.Value) : null
                    };

                    _context.Accounts.Add(acc);
                    _context.SaveChanges();

                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    // Ghi log lỗi ModelState để debug
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi validation: " + error.ErrorMessage);
                    }
                    return View(registerDTO); // Trả về view với lỗi validation
                }
            }
            catch (Exception ex)
            {
                // Ghi log ngoại lệ nếu cần
                System.Diagnostics.Debug.WriteLine("Lỗi khi đăng ký: " + ex.Message);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại.");
                return View(registerDTO); // Trả về view với dữ liệu đã nhập
            }
        }
        #endregion
        /// <summary>
        /// Logout
        /// </summary>
        /// <returns></returns>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Product");
        }


    }
}
