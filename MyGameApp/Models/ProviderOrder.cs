using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class ProviderOrder
{
    public int Id { get; set; }

    public int ProviderId { get; set; }

    public DateTime Date { get; set; }

    public decimal? TotalCost { get; set; }

    public string? Status { get; set; }

    public virtual Provider Provider { get; set; } = null!;

    public virtual ICollection<ProviderOrderItem> ProviderOrderItems { get; set; } = new List<ProviderOrderItem>();
}
