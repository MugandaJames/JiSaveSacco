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

        public int GetUserId()
        {
            return int.Parse(
                _http.HttpContext!.User.FindFirst("uid")!.Value
            );
        }

        public int? GetMemberId()
        {
            var value = _http.HttpContext!.User.FindFirst("mid")?.Value;

            return value == null ? null : int.Parse(value);
        }

        public string GetRole()
        {
            return _http.HttpContext!.User.FindFirst("role")!.Value;
        }
    }
}