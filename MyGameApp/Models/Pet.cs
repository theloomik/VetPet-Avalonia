using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class Pet
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public int PetTypeId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Client Client { get; set; } = null!;

    public virtual PetType PetType { get; set; } = null!;
}
