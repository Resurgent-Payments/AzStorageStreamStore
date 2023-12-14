namespace MvcHost.Controllers;
using System.Diagnostics;

using BusinessDomain;

using LvStreamStore.ApplicationToolkit;

using Microsoft.AspNetCore.Mvc;

using MvcHost.Models;

public class HomeController : Controller {
    private readonly ICommandPublisher _cmdPublisher;

    public HomeController(ICommandPublisher cmdPublisher) {
        _cmdPublisher = cmdPublisher;
    }

    [HttpGet("")]
    public IActionResult Index() {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddItems() {
        var x = await _cmdPublisher.SendAsync(new ItemMsgs.AddItems(5));
        try {
            _ = x.IsType<CommandCompleted>();
            return RedirectToAction("Index");
        }
        catch (Exception ex) {
            return BadRequest();
        }
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
