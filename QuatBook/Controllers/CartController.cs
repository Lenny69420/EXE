using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuatBook.Dto;
using QuatBook.Helpers;
using QuatBook.Hubs;
using QuatBook.Models;
using QuatBook.Service;

namespace QuatBook.Controllers
{
    public class CartController : Controller
    {
        private readonly QuatBookContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IVietQRService _vietQRService;
        private readonly IHubContext<ProductHub> _hubContext;

        public CartController(QuatBookContext context, IVnPayService vnPayService, IVietQRService vietQRService, IHubContext<ProductHub> hubContext)
        {
            _context = context;
            _vnPayService = vnPayService;
            _vietQRService = vietQRService;
            _hubContext = hubContext;
        }



        //const string CART_KEY = "MYCART ";
        public List<CartDTO> Cart => HttpContext.Session.Get<List<CartDTO>>(MySettings.CART_KEY) ?? new List<CartDTO>();
        public IActionResult Index()
        {
            var cart = Cart;
            bool hasExpiredItems = false;

            // Kiểm tra và xóa các mục hết hạn
            foreach (var item in cart.ToList()) // ToList() để tránh lỗi sửa đổi collection
            {
                var timeElapsed = DateTime.Now - item.AddedTime;
                if (timeElapsed.TotalMinutes >= 1)
                {
                    var product = _context.Products.SingleOrDefault(p => p.BookId == item.bookId);
                    if (product != null)
                    {
                        product.Quantity += item.Quantity; // Khôi phục tồn kho
                        _context.SaveChanges();
                        // Gửi thông báo SignalR để cập nhật giao diện
                        _hubContext.Clients.All.SendAsync("RefreshProducts");
                    }
                    cart.Remove(item);
                    hasExpiredItems = true;
                }
            }

            if (hasExpiredItems)
            {
                HttpContext.Session.Set(MySettings.CART_KEY, cart);
                TempData["Message"] = "Some items were removed from your cart due to a 5-minute timeout.";
            }

            return View(cart);
        }

        //ADD TO CART
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.bookId == id);
            var product = _context.Products.SingleOrDefault(p => p.BookId == id);

            if (product == null)
            {
                TempData["Message"] = $"Product not found have id = {id}";
                return Redirect("/404");
            }

            // Tính tổng số lượng yêu cầu (số lượng hiện có trong giỏ + số lượng mới)
            int currentQuantityInCart = item?.Quantity ?? 0;
            int totalRequestedQuantity = currentQuantityInCart + quantity;

            // Kiểm tra nếu tổng số lượng yêu cầu vượt quá tồn kho
            if (product.Quantity.GetValueOrDefault() < totalRequestedQuantity)
            {
                TempData["Message"] = $"Not enough stock for {product.BookName}. Available: {product.Quantity}, Requested: {totalRequestedQuantity}";
                return Redirect("/");
            }

            if (item == null)
            {
                // Giảm số lượng tồn kho trong database
                product.Quantity -= quantity;
                _context.SaveChanges();

                // Tạo item mới cho giỏ hàng
                item = new CartDTO
                {
                    bookId = product.BookId,
                    BookName = product.BookName,
                    Image = product.Image ?? string.Empty,
                    Price = product.Price ?? 0,
                    Quantity = quantity,
                    AddedTime = DateTime.Now
                };
                cart.Add(item);
            }
            else
            {
                // Giảm thêm tồn kho dựa trên số lượng mới
                product.Quantity -= quantity;
                _context.SaveChanges();
                item.Quantity += quantity;
            }

            HttpContext.Session.Set(MySettings.CART_KEY, cart);
            return RedirectToAction("Index");
        }


        //REMOVE FROM CART
        public IActionResult RemoveCart(int id)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.bookId == id);
            if (item != null)
            {
                var product = _context.Products.SingleOrDefault(p => p.BookId == id);
                if (product != null)
                {
                    product.Quantity += item.Quantity; // Khôi phục số lượng
                    _context.SaveChanges();
                }
                cart.Remove(item);
                HttpContext.Session.Set(MySettings.CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        //UPDATE CART BY QUANTITY
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.bookId == id);
            var product = _context.Products.SingleOrDefault(p => p.BookId == id);

            if (item == null || product == null)
            {
                TempData["Message"] = "Item or product not found.";
                return RedirectToAction("Index");
            }

            if (quantity <= 0)
            {
                // Nếu số lượng <= 0, xóa sản phẩm khỏi giỏ hàng và khôi phục tồn kho
                product.Quantity += item.Quantity;
                cart.Remove(item);
                _context.SaveChanges();
                HttpContext.Session.Set(MySettings.CART_KEY, cart);
                return RedirectToAction("Index");
            }

            // Tính toán sự thay đổi số lượng
            int quantityDifference = quantity - item.Quantity;

            // Tính tổng số lượng tối đa có thể sử dụng (tồn kho + số lượng hiện tại trong giỏ)
            int availableQuantity = product.Quantity.GetValueOrDefault() + item.Quantity;

            // Kiểm tra nếu số lượng yêu cầu vượt quá tồn kho khả dụng
            if (quantity > availableQuantity)
            {
                TempData["Message"] = $"Not enough stock for {product.BookName}. Available: {product.Quantity}, Requested: {quantity}";
                return RedirectToAction("Index");
            }

            // Cập nhật tồn kho và số lượng trong giỏ hàng
            product.Quantity -= quantityDifference;
            item.Quantity = quantity;
            _context.SaveChanges();

            // Lưu giỏ hàng vào session
            HttpContext.Session.Set(MySettings.CART_KEY, cart);

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Checkout()
        {
            var cart = Cart;
            if (cart.Count == 0)
            {
                TempData["Message"] = "Cart is empty";
                return Redirect("/");
            }

            return View(cart);

        }

        // Hàm giảm số lượng tồn kho
        private void ReduceProduct(List<CartDTO> cart)
        {
            foreach (var item in cart)
            {
                var product = _context.Products.SingleOrDefault(p => p.BookId == item.bookId);
                if (product != null)
                {
                    int newQuantity = (product.Quantity ?? 0) - item.Quantity;
                    if (newQuantity < 0)
                    {
                        throw new Exception($"Not enough stock for {item.BookName}. Available: {product.Quantity}, Requested: {item.Quantity}");
                    }
                    product.Quantity = newQuantity; // Giảm số lượng tồn kho
                    _context.Products.Update(product);
                }
            }
            _context.SaveChanges(); // Lưu thay đổi vào database
        }

        [HttpPost]
        [Authorize]
        public IActionResult Checkout(CheckoutDTO model)
        {
            var cart = Cart;
            if (cart.Count == 0)
            {
                TempData["Message"] = "Cart is empty";
                return Redirect("/");
            }

            if (!ModelState.IsValid)
            {
                return View(cart);
            }

            // Lấy UserId từ session
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) // Check if the value is null
            {
                return RedirectToAction("Login", "Account");
            }

            int accountId = userId.Value; // Safe to access Value now

            // Xử lý đặt hàng dựa trên phương thức thanh toán
            switch (model.PaymentMethod)
            {
                case "COD":
                    var order = new Order
                    {
                        FullName = model.FullName,
                        Address = model.Address,
                        Phone = model.Phone,
                        Note = model.Note,
                        CreateTime = DateTime.Now,
                        Ammount = cart.Sum(p => p.Ammount),
                        PaymentMethod = "COD",
                        Status = "Pending",
                        AccountId = accountId
                    };

                    order.OrderDetails = cart.Select(item => new OrderDetail
                    {
                        ProductId = item.bookId,
                        Quantity = item.Quantity,
                        Order = order
                    }).ToList();

                    //add and save
                    _context.Orders.Add(order);
                    _context.SaveChanges();

                    //reduce quantity
                    ReduceProduct(cart);

                    //clear cart
                    HttpContext.Session.Remove(MySettings.CART_KEY);

                    // Lấy danh sách sản phẩm đã mua
                    var purchasedItems = order.OrderDetails
                        .Select(od => $"{od.Product.BookName} (x{od.Quantity})")
                        .ToList();
                    var purchasedItemsString = string.Join("<br>", purchasedItems);

                    // Tạo thông báo chi tiết cho popup
                    TempData["PopupMessage"] = $"Đặt hàng thành công với COD!<br>" +
                                              $"Tên người mua: {order.FullName}<br>" +
                                              $"Mã đơn hàng: {order.OrderId}<br>" +
                                               $"Số tiền: {(int)order.Ammount} VND <br>" +
                                              $"Sản phẩm đã mua:<br>{purchasedItemsString}";
                    TempData["PopupType"] = "success";


                    return Redirect("/");

                case "VNPAY":
                    // Tạo đơn hàng trước khi chuyển hướng đến VNPAY
                    var orderVnpay = new Order
                    {
                        FullName = model.FullName,
                        Address = model.Address,
                        Phone = model.Phone,
                        Note = model.Note,
                        CreateTime = DateTime.Now,
                        Ammount = cart.Sum(p => p.Ammount),
                        PaymentMethod = "VNPAY",
                        Status = "Pending", // Trạng thái ban đầu là Pending
                        AccountId = accountId,
                        TransactionId = DateTime.Now.Ticks.ToString() // Tạo TransactionId tạm thời
                    };

                    orderVnpay.OrderDetails = cart.Select(item => new OrderDetail
                    {
                        ProductId = item.bookId,
                        Quantity = item.Quantity,
                        Order = orderVnpay
                    }).ToList();

                    // Lưu đơn hàng vào database
                    _context.Orders.Add(orderVnpay);
                    _context.SaveChanges();

                    // Tạo model cho VNPAY với OrderId duy nhất từ đơn hàng vừa lưu
                    var vnPayModel = new VnPaymentRequestModel
                    {
                        Ammount = cart.Sum(p => p.Ammount),
                        CreatedDate = DateTime.Now,
                        Description = model.Note,
                        FullName = model.FullName,
                        OrderId = orderVnpay.OrderId // Sử dụng OrderId từ đơn hàng vừa tạo
                    };

                    //// Chuyển hướng đến URL thanh toán VNPAY
                    //return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
                    string paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayModel);
                    orderVnpay.TransactionId = paymentUrl.Split("vnp_TxnRef=")[1].Split('&')[0]; // Lấy vnp_TxnRef từ URL
                    _context.SaveChanges();
                    //clear cart
                    HttpContext.Session.Remove(MySettings.CART_KEY);
                    return Redirect(paymentUrl);


                case "VietQR":
                    var orderVietQR = new Order
                    {
                        FullName = model.FullName,
                        Address = model.Address,
                        Phone = model.Phone,
                        Note = model.Note,
                        CreateTime = DateTime.Now,
                        Ammount = cart.Sum(p => p.Ammount),
                        PaymentMethod = "VietQR",
                        Status = "Pending",
                        AccountId = accountId,
                        TransactionId = DateTime.Now.Ticks.ToString()
                    };

                    orderVietQR.OrderDetails = cart.Select(item => new OrderDetail
                    {
                        ProductId = item.bookId,
                        Quantity = item.Quantity,
                        Order = orderVietQR
                    }).ToList();

                    _context.Orders.Add(orderVietQR);
                    _context.SaveChanges();

                    ReduceProduct(cart);
                    HttpContext.Session.Remove(MySettings.CART_KEY);

                    return RedirectToAction("CheckoutWithVietQR", new { orderId = orderVietQR.OrderId });
                default:
                    TempData["Message"] = "Invalid payment method.";
                    return RedirectToAction("Checkout");
            }
        }


        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null)
            {
                TempData["PopupMessage"] = "Không nhận được phản hồi từ VNPAY.";
                TempData["PopupType"] = "error";
                return RedirectToAction("Index", "Product");
            }

            // Debug: In giá trị response.OrderId
            TempData["DebugOrderId"] = $"Giá trị OrderId từ VNPAY: {response.OrderId}";

            if (response.VnPayResponseCode != "00")
            {
                string errorMessage;
                switch (response.VnPayResponseCode)
                {
                    case "24":
                        errorMessage = "Giao dịch đã bị hủy bởi người dùng.";
                        break;
                    case "99":
                        errorMessage = "Lỗi không xác định từ VNPAY.";
                        break;
                    default:
                        errorMessage = $"Giao dịch thất bại. Mã lỗi: {response.VnPayResponseCode}";
                        break;
                }
                TempData["PopupMessage"] = errorMessage;
                TempData["PopupType"] = "error";
                return RedirectToAction("Index", "Product");
            }

            if (response.Success)
            {
                // Tìm đơn hàng dựa trên TransactionId (vnp_TxnRef)
                var order = _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product) // Include sản phẩm để lấy tên sản phẩm
                    .SingleOrDefault(o => o.TransactionId == response.OrderId);

                if (order != null && order.Status == "Pending")
                {
                    order.Status = "Paid";
                    _context.Orders.Update(order);

                    var cart = order.OrderDetails.Select(od => new CartDTO
                    {
                        bookId = od.ProductId,
                        Quantity = od.Quantity,
                        BookName = _context.Products.FirstOrDefault(p => p.BookId == od.ProductId)?.BookName ?? "Unknown"
                    }).ToList();
                    ReduceProduct(cart);

                    _context.SaveChanges();

                    // Lấy danh sách sản phẩm đã mua
                    var purchasedItems = order.OrderDetails
                        .Select(od => $"{od.Product.BookName} (x{od.Quantity})")
                        .ToList();
                    var purchasedItemsString = string.Join("<br>", purchasedItems);

                    // Tạo thông báo chi tiết
                    TempData["PopupMessage"] = $"Thanh toán thành công qua VNPAY! <br>" +
                          $"Tên người mua: {order.FullName} <br>" +
                          $"Mã đơn hàng: {order.OrderId} <br>" +
                          $"Số tiền: {(int)order.Ammount} VND <br>" +
                          $"Sản phẩm đã mua:<br>{purchasedItemsString}";
                    TempData["PopupType"] = "success";

                    return RedirectToAction("Index", "Product");
                }
                else
                {
                    TempData["PopupMessage"] = "Đơn hàng không tồn tại hoặc đã được xử lý.";
                    TempData["PopupType"] = "error";
                    return RedirectToAction("Index", "Product");
                }
            }

            TempData["PopupMessage"] = "Giao dịch không thành công. Vui lòng thử lại.";
            TempData["PopupType"] = "error";
            return RedirectToAction("Index", "Product");
        }


        [HttpGet]
        public IActionResult PaymentSuccess()
        {
            // Không thay đổi logic của action này
            return View();
        }
        [HttpGet]
        public IActionResult PaymentFailed()
        {
            // Không thay đổi logic của action này
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CheckoutWithVietQR(int orderId)
        {
            // Log entry for debugging (optional, if ILogger is available)
            // _logger.LogInformation("Entering CheckoutWithVietQR for orderId: {OrderId}", orderId);

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .SingleOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Message"] = "Order not found.";
                return RedirectToAction("Index", "Cart"); // Explicitly specify controller
            }

            try
            {
                // Handle nullable Ammount
                if (!order.Ammount.HasValue)
                {
                    TempData["Message"] = "Order amount is missing.";
                    return RedirectToAction("Checkout", "Cart");
                }

                var qrCodeUrl = await _vietQRService.GenerateQRCodeAsync(order.Ammount.Value, order.OrderId.ToString(), $"Thanh toan don hang {order.OrderId}");
                if (string.IsNullOrEmpty(qrCodeUrl))
                {
                    TempData["Message"] = "Failed to generate QR code: No QR code URL returned.";
                    return RedirectToAction("Checkout", "Cart");
                }

                ViewBag.QrCodeUrl = qrCodeUrl;
                ViewBag.Order = order;
                return View(order); // Pass the order as the model explicitly
            }
            catch (Exception ex)
            {
                // Log the exception (replace Console.WriteLine with ILogger if available)
                Console.WriteLine($"Error generating QR code for orderId {orderId}: {ex.Message}");
                TempData["Message"] = $"Failed to generate QR code: {ex.Message}";
                return RedirectToAction("Checkout", "Cart");
            }
        }
    }
}
