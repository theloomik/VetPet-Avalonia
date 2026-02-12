using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Appointment
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public int PetId { get; set; }

    public int StaffId { get; set; }

    public DateTime Date { get; set; }

    public int? ServiceId { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual Client Client { get; set; } = null!;

    public virtual Pet Pet { get; set; } = null!;

    public virtual Service? Service { get; set; }

    public virtual Staff Staff { get; set; } = null!;

    public virtual ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
}
