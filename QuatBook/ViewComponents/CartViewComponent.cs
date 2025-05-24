using Microsoft.AspNetCore.Mvc;
using QuatBook.Dto;
using QuatBook.Helpers;
using QuatBook.Models;

namespace QuatBook.ViewComponents
{
    public class CartViewComponent : ViewComponent
    {
        private readonly QuatBookContext _context;

        public CartViewComponent(QuatBookContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var cart = HttpContext.Session.Get<List<CartDTO>>(MySettings.CART_KEY) ?? new List<CartDTO>();
            return View(new CartModel
            {
                Quantity = cart.Sum(p => p.Quantity),//theo cot
                Total = cart.Sum(p => p.Ammount),
                Items = cart
            });

        }
    }
}
