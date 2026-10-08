using System.ComponentModel.DataAnnotations;
using ToyBoxApi.DTOs.Toys;

namespace ToyBoxApi.DTOs.Profile;

public class ChildDto
{
    public int ChildId { get; set; }
    public int ParentUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public string? Interests { get; set; }
    public bool IsVisibleToContacts { get; set; }

    public int Age { get; set; }
    public DateOnly NextBirthday { get; set; }
    public int DaysUntilBirthday { get; set; }

    /// <summary>Age the child turns on <see cref="NextBirthday"/>.</summary>
    public int TurningAge { get; set; }

    /// <summary>Filled for other people's children (e.g. upcoming birthdays).</summary>
    public ToyOwnerDto? Parent { get; set; }
}

public class SaveChildRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly BirthDate { get; set; }

    [MaxLength(200)]
    public string? Interests { get; set; }

    public bool IsVisibleToContacts { get; set; } = true;
}
