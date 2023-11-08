namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using System.Net;

    using Microsoft.AspNetCore.Mvc;

    [Route("webhooks")]
    public class WebHookSubscriptionController : ControllerBase {
        ICommandPublisher _cmdPublisher;

        public WebHookSubscriptionController(ICommandPublisher cmdPublisher) {
            _cmdPublisher = cmdPublisher;
        }

        [Route("{subscriptionId:guid}")]
        public ActionResult Index(Guid subscriptionId) {
            return Ok();
        }

        [Route("subscribe"), HttpPost]
        public async Task<ActionResult> Subscribe([FromBody] SubscriptionForm form, CancellationToken token) {
            var cmd = new WebHookSubscriptionMsgs.Subscribe(form.WebHookId, form.SubscriptionId, form.Description, form.PostUrl, token);

            try {
                var result = await _cmdPublisher.SendAsync(cmd);
                _ = result.IsType<CommandCompleted>();
            }
            catch (InvalidResultException exc) {
                //todo: investigate exception for more information.  this could be a successful failure.
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            return Created(Url.Action("Index", "WebHookSubscription", new { subscriptionId = cmd.SubscriptionId }), new { });
        }

        [Route("enable/{subscriptionId:guid}"), HttpPost]
        public async Task<ActionResult> Enable(Guid subscriptionId, CancellationToken token) {
            var cmd = new WebHookSubscriptionMsgs.Enable(subscriptionId, token);

            try {
                var result = await _cmdPublisher.SendAsync(cmd);
                _ = result.IsType<CommandCompleted>();
            }
            catch (InvalidResultException exc) {
                //todo: investigate exception for more information.  this could be a successful failure.
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            return Ok();
        }

        [Route("disable/{subscriptionId:guid}"), HttpPost]
        public async Task<ActionResult> Disable(Guid subscriptionId, CancellationToken token) {
            var cmd = new WebHookSubscriptionMsgs.Disable(subscriptionId, token);

            try {
                var result = await _cmdPublisher.SendAsync(cmd);
                _ = result.IsType<CommandCompleted>();
            }
            catch (InvalidResultException exc) {
                //todo: investigate exception for more information.  this could be a successful failure.
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            return Ok();
        }

        [Route("remove/{subscriptionId:guid}"), HttpPost]
        public async Task<ActionResult> Remove(Guid subscriptionId, CancellationToken token) {
            var cmd = new WebHookSubscriptionMsgs.Remove(subscriptionId, token);

            try {
                var result = await _cmdPublisher.SendAsync(cmd);
                _ = result.IsType<CommandCompleted>();
            }
            catch (InvalidResultException exc) {
                //todo: investigate exception for more information.  this could be a successful failure.
                return StatusCode((int)HttpStatusCode.BadRequest);
            }

            return Ok();
        }
    }


    public class SubscriptionForm {
        public Guid SubscriptionId { get; set; }
        public Guid WebHookId { get; set; }
        public string PostUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
