using System;
using System.Collections.Generic;

namespace QuatBook.Models;

public partial class Product
{
    public int BookId { get; set; }

    public string BookName { get; set; } = null!;

    public string? Image { get; set; }

    public int? Quantity { get; set; }
    public bool? active { get; set; }
    public double? Price { get; set; }

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int? AuthorId { get; set; }

    public DateTime? Created { get; set; }

    public virtual Author? Author { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
