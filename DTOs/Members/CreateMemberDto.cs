namespace JiSaveSacco.API.DTOs.Members
{
    public class CreateMemberDto
    {
        public string Username { get; set; } = string.Empty;   // login account
        public string Password { get; set; } = string.Empty;

        public string MemberNo { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
    }
}