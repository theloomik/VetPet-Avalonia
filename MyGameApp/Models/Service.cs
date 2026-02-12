using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Service
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
}
