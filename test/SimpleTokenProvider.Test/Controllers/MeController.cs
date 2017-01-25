using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleTokenProvider.Test.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class MeController : Controller
    {
        public string Get()
        {
            // The JWT "sub" claim is automatically mapped to ClaimTypes.NameIdentifier
            // by the UseJwtBearerAuthentication middleware
            var username = HttpContext.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;

            return $"Hello {username}!";
        }
    }
}
