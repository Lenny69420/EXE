using System;
using System.Collections.Generic;

namespace QuatBook.Models;

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Email { get; set; }
    public string? Image { get; set; }

    public string? Address { get; set; }

    public int RoleId { get; set; }

    public bool Gender { get; set; }

    public DateOnly? Birth { get; set; }
    public string? Phone { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;
}
