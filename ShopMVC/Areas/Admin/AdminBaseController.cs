using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShopMVC.Areas.Admin
{
    [Area("Admin")]
    [Authorize(Roles = "QuanTri")]
    public abstract class AdminBaseController : Controller { }
}
