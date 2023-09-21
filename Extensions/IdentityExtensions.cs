using System.Security.Claims;
using System.Security.Principal;

namespace Chrysalis.Extensions
{
    public static class IdentityExtensions
    {
        public static int GetCompanyId(this IIdentity identity)
        {
            Claim claim = ((ClaimsIdentity)identity).FindFirst("CompanyId")!;
            return int.Parse(claim.Value);
        }

        public static string GetUserId(this IIdentity identity)
        {
            Claim claim = ((ClaimsIdentity)identity).FindFirst("UserId")!;
            return claim.Value;
        }
    }
}
