namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using Microsoft.AspNetCore.Mvc;

    [Route(".well-known/webhooks")]
    public class DiscoveryController : ControllerBase {
        private readonly WebHookRm _webHooks;

        public DiscoveryController(WebHookRm webHooks) {
            _webHooks = webHooks;
        }

        public IActionResult Index() => Ok(new { _webHooks.WebHooks });
    }
}
