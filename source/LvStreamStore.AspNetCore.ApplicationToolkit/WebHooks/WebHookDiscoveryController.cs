namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using Microsoft.AspNetCore.Mvc;

    [Route(".well-known/webhooks")]
    public class WebHookDiscoveryController : ControllerBase {
        private readonly WebHookRm _webHooks;

        public WebHookDiscoveryController(WebHookRm webHooks) {
            _webHooks = webHooks;
        }

        public IActionResult Index() => Ok(new { _webHooks.WebHooks });
    }
}
