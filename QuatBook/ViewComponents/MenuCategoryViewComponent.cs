using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuatBook.Dto;
using QuatBook.Models;

namespace QuatBook.ViewComponents
{
    public class MenuCategoryViewComponent : ViewComponent
    {
        private readonly QuatBookContext _context;

        public MenuCategoryViewComponent(QuatBookContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            Console.WriteLine("MenuCategory ViewComponent is invoked.");
            var categories = _context.Categories.Select(cate => new MenuCategoryDTO
            {
                CategoryId = cate.CategoryId,
                CategoryName = cate.CategoryName,
                CategoryCount = cate.Products.Count
            }).OrderBy(ca=> ca.CategoryName).ToList();
            return View(categories);
        }
    }
}
