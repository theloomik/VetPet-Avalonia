using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class StaffPosition
{
    public int Id { get; set; }

    public string Position { get; set; } = null!;

    public decimal Salary { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
