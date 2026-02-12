using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Treatment
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    public string Description { get; set; } = null!;

    public int? MedicineId { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Medicine? Medicine { get; set; }
}
