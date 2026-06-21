namespace JiSaveSacco.API.DTOs.Members
{
    public class MemberResponseDto
    {
        public int MemberId { get; set; }
        public string MemberNo { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}