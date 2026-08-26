using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.WebUI.Models;

namespace Symplify.BackOffice.WebUI.Controllers;

[AllowAnonymous]
[Route("error")]
public sealed class ErrorController : Controller
{
    [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH")]
    public IActionResult Index()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;

        return View("~/Views/Shared/Error.cshtml", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
