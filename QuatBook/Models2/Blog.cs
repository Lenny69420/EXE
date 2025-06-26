using System;
using System.Collections.Generic;

namespace QuatBook.Models2;

public partial class Blog
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public byte[]? Image { get; set; }
}
