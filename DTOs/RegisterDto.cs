using System.ComponentModel.DataAnnotations;

namespace JiSaveSacco.API.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(30, MinimumLength = 4, ErrorMessage = "Username must be between 4 and 30 characters.")]
        public required string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters long.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#!^()_\-+=]).{12,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character."
        )]
        public required string Password { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        [RegularExpression(
            @"^[A-Za-z]+(?:[ '-][A-Za-z]+)*$",
            ErrorMessage = "First name contains invalid characters."
        )]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        [RegularExpression(
            @"^[A-Za-z]+(?:[ '-][A-Za-z]+)*$",
            ErrorMessage = "Last name contains invalid characters."
        )]
        public required string LastName { get; set; }

        [Required]
        [RegularExpression(
            @"^\d{7,10}$",
            ErrorMessage = "National ID must contain 7 to 10 digits."
        )]
        public required string NationalId { get; set; }

        [Required]
        [RegularExpression(
            @"^(07\d{8}|01\d{8}|2547\d{8}|2541\d{8})$",
            ErrorMessage = "Enter a valid Kenyan phone number."
        )]
        public required string Phone { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        public required string Email { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 5)]
        public required string Address { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public required string Occupation { get; set; }
    }
}