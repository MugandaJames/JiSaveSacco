namespace JiSaveSacco.API.DTOs
{
    public class RegisterDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }

        public required string MemberNo { get; set; }

        public required string FirstName { get; set; }
        public required string LastName { get; set; }

        public required string NationalId { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }

        public required string Address { get; set; }
        public required string Occupation { get; set; }
    }
}