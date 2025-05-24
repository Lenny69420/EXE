using System;
using System.Collections.Generic;

namespace QuatBook.Models;

public partial class OrderDetail
{
    public int OderDetailId { get; set; }

    public int Quantity { get; set; }

    public int ProductId { get; set; }

    public int OrderId { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
