using Microsoft.AspNetCore.Mvc;
using TeamSite.Web.Data;

namespace TeamSite.Web.Controllers;

/// <summary>
/// The team site's pages. Each action picks the data for one page and hands it to the
/// view with the same name in Views/Home/ (Index() uses Views/Home/Index.cshtml).
/// To add a page: add a method here with its own [Route("...")], a matching view,
/// and a link in Nav in Data/TeamInfo.cs.
/// </summary>
public class HomeController : Controller
{
    [Route("")]
    public IActionResult Index() => View(TeamInfo.Team);
}
