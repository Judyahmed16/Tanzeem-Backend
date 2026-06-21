using System.ComponentModel.DataAnnotations;

namespace Tanzeem.Shared.Dtos.Users {
    public class UserProfileUpdateDto {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
