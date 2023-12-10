namespace MvcHost.Controllers {
    using BusinessDomain;

    using LvStreamStore.ApplicationToolkit;

    using Microsoft.AspNetCore.Mvc;

    using MvcHost.Models;

    public class ItemsController : Controller {
        private readonly ICommandPublisher _cmdPublisher;

        public ItemsController(ICommandPublisher cmdPublisher) {
            _cmdPublisher = cmdPublisher;
        }

        public IActionResult Index() => View();


        [HttpPost, Route("", Order = 2)]
        public async Task<IActionResult> PostAsync(ItemForms.AddItem form) {
            var result = await _cmdPublisher.SendAsync(new ItemMsgs.CreateItem(form.ItemId, form.Name));
            return ProcessResult(result);
        }


        [HttpPost, Route("{itemid:guid}/rename", Order = 1)]
        public async Task<IActionResult> RenameAsync(Guid itemId, ItemForms.Rename form) {
            var result = await _cmdPublisher.SendAsync(new ItemMsgs.ChangeName(itemId, form.Name));
            return ProcessResult(result);
        }


        private IActionResult ProcessResult(CommandResult result) {
            try {
                _ = result.IsType<CommandCompleted>();
                return RedirectToAction("Index");
            }
            catch {
                return BadRequest();
            }
        }
    }
}
