namespace MvcHost.Controllers {
    using BusinessDomain;

    using LvStreamStore.Messaging;

    using Microsoft.AspNetCore.Mvc;

    using MvcHost.Models;

    public class ItemsController : Controller {
        private readonly AsyncDispatcher _dispatcher;

        public ItemsController(AsyncDispatcher dispatcher) {
            _dispatcher = dispatcher;
        }

        public IActionResult Index() => View();


        [HttpPost, Route("", Order = 2)]
        public async Task<IActionResult> PostAsync(ItemForms.AddItem form) {
            try {
                await _dispatcher.SendAsync(new ItemMsgs.CreateItem(form.ItemId, form.Name));
                return RedirectToAction("Index");
            }
            catch (Exception) {
                return BadRequest();
            }
        }


        [HttpPost, Route("{itemid:guid}/rename", Order = 1)]
        public async Task<IActionResult> RenameAsync(Guid itemId, ItemForms.Rename form) {
            try {
                await _dispatcher.SendAsync(new ItemMsgs.ChangeName(itemId, form.Name));
                return RedirectToAction("Index");
            }
            catch (Exception) {
                return BadRequest();
            }
        }
    }
}
