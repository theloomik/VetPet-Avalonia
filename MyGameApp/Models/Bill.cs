using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Bill
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? Date { get; set; }

    public string? Paid { get; set; }

    public string? PaymentMethod { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
}
