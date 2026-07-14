using System.Security.Claims;

namespace JiSaveSacco.API.Services
{
    public class IdentityService
    {
        private readonly IHttpContextAccessor _http;

        public IdentityService(IHttpContextAccessor http)
        {
            _http = http;
        }

        public int? GetUserId()
        {
            var value = _http.HttpContext?
                .User
                .FindFirst("uid")
                ?.Value;

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value, out var id)
                ? id
                : null;
        }

        public int? GetMemberId()
        {
            var value = _http.HttpContext?
                .User
                .FindFirst("mid")
                ?.Value;

            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value, out var id)
                ? id
                : null;
        }

        public string? GetRole()
        {
            return _http.HttpContext?
                .User
                .FindFirst("role")
                ?.Value;
        }
    }
}