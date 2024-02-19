namespace MvcHost.Controllers;
using System.Diagnostics;

using BusinessDomain;

using LvStreamStore.Messaging;

using Microsoft.AspNetCore.Mvc;

using MvcHost.Models;

public class HomeController : Controller {
    private readonly AsyncDispatcher _dispatcher;

    public HomeController(AsyncDispatcher dispatcher) {
        _dispatcher = dispatcher;
    }

    [HttpGet("")]
    public IActionResult Index() {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddItems() {
        try {
            await _dispatcher.SendAsync(new ItemMsgs.AddItems(5));
        }
        catch (Exception) {
            return BadRequest();
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> AddSingleItem() {
        try {
            await _dispatcher.SendAsync(new ItemMsgs.AddItems(1));
        }
        catch (Exception) {
            return BadRequest();
        }
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
