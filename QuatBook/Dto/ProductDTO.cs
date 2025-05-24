using QuatBook.Models;

namespace QuatBook.Dto
{
    public class ProductDTO
    {
        public int BookId { get; set; }

        public string BookName { get; set; } = null!;

        public string? Image { get; set; }

        public double? Price { get; set; }

        public string CategoryName { get; set; }
    }

    public class ProductDetailDTO
    {
        public int BookId { get; set; }

        public string BookName { get; set; } = null!;

        public string? Image { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public double? Price { get; set; }

        public string CategoryName { get; set; }
        public string AuthorName { get; set; }
    }
    public class ProductViewModel
    {
        public int BookId { get; set; }
        public string BookName { get; set; } = null!;
        public string? Image { get; set; }
        public IFormFile? ImageFile { get; set; }
        public int Quantity { get; set; }
        public double? Price { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int? AuthorId { get; set; }
        public DateTime? Created { get; set; }
    }

}
