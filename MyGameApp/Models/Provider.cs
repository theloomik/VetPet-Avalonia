using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Provider
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<ProviderOrder> ProviderOrders { get; set; } = new List<ProviderOrder>();
}
