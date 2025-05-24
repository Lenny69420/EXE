using System;
using System.Collections.Generic;

namespace QuatBook.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public double? Ammount { get; set; }

    public int AccountId { get; set; }

    public DateTime? CreateTime { get; set; }

    public string? FullName { get; set; }

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Note { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
    public string? TransactionId { get; set; }
    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
