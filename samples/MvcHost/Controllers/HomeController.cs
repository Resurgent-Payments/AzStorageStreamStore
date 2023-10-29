namespace MvcHost.Controllers;
using System.Diagnostics;

using LvStreamStore;
using LvStreamStore.ApplicationToolkit;

using Microsoft.AspNetCore.Mvc;

using MvcHost.Models;

public class HomeController : Controller {
    private readonly ICommandPublisher _cmdPublisher;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ICommandPublisher cmdPublisher, ILogger<HomeController> logger) {
        _cmdPublisher = cmdPublisher;
        _logger = logger;
    }

    public IActionResult Index() {
        return View();
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
