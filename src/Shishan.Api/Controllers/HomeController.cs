using Microsoft.AspNetCore.Mvc;

namespace Shishan.Api.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Topic(string code)
    {
        ViewBag.TopicCode = code;
        return View();
    }
}