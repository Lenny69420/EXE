using System;
using System.Collections.Generic;

namespace QuatBook.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? CategoryDescription { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
