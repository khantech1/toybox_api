using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ToyBoxApi.DTOs.Auth;

namespace ToyBoxApi.DTOs.Profile;

public class UpdateProfileRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }
}

public class ProfileResponse
{
    public UserDto User { get; set; } = null!;
}

public class ContactDto
{
    public int UserId { get; set; }
    public int ContactId { get; set; }
    public UserDto? ContactUser { get; set; }
}

public class AddContactRequest
{
    [Required]
    public int ContactId { get; set; }
}

public class SyncContactsRequest
{
    [JsonPropertyName("phoneNumbers")]
    public List<string> PhoneNumbers { get; set; } = new();
}
