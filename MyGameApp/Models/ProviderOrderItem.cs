using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class ProviderOrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual ProviderOrder Order { get; set; } = null!;
}
