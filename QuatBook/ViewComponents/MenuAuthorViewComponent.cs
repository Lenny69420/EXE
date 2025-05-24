using Microsoft.AspNetCore.Mvc;
using QuatBook.Dto;
using QuatBook.Models;

namespace QuatBook.ViewComponents
{
    public class MenuAuthorViewComponent : ViewComponent
    {
        private readonly QuatBookContext _context;

        public MenuAuthorViewComponent(QuatBookContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            Console.WriteLine("MenuAuthor ViewComponent is invoked.");
            var authors = _context.Authors.Select(au => new MenuAuthorDTO
            {
                AuthorId = au.AuthorId,
                AuthorName = au.AuthorName
            }).OrderBy(au => au.AuthorName).ToList();
            return View(authors);
        }
    }
}
