using Chrysalis.Enums;
using Chrysalis.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Chrysalis.Controllers
{
    [Controller]
    public abstract class BaseController : Controller
    {
        protected int _companyId => User.Identity!.GetCompanyId();
        protected string _userId => User.Identity!.GetUserId();
    }
}