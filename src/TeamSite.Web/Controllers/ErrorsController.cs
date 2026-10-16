using Microsoft.AspNetCore.Mvc;

namespace TeamSite.Web.Controllers;

/// <summary>The pages shown when something goes wrong. Program.cs sends visitors here.</summary>
public class ErrorsController : Controller
{
    [Route("not-found")]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = 404;
        return View();
    }

    [Route("error")]
    public IActionResult ServerError()
    {
        Response.StatusCode = 500;
        return View();
    }
}
