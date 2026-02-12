using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class BillItem
{
    public int Id { get; set; }

    public int BillId { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int? ServiceId { get; set; }

    public int? MedicineId { get; set; }

    public virtual Bill Bill { get; set; } = null!;

    public virtual Medicine? Medicine { get; set; }

    public virtual Service? Service { get; set; }
}
