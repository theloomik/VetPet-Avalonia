using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Stock
{
    public int Id { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;
}
