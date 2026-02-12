using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Medicine
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();

    public virtual ICollection<ProviderOrderItem> ProviderOrderItems { get; set; } = new List<ProviderOrderItem>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
}
