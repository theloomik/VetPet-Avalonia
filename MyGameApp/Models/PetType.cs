using System;
using System.Collections.Generic;

namespace MyGameApp.Models;

public partial class PetType
{
    public int Id { get; set; }

    public string Species { get; set; } = null!;

    public string? Breed { get; set; }

    public virtual ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
