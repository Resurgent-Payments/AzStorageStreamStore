namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using System.Net;

    using Microsoft.AspNetCore.Mvc;

    [Route("webhooks/subscriptions")]
    public class SubscriptionController : ControllerBase {
        private readonly ICommandPublisher _cmdPublisher;
        private readonly WebHookRm _readModel;

        public SubscriptionController(ICommandPublisher cmdPublisher, WebHookRm readModel) {
            _cmdPublisher = cmdPublisher;
            _readModel = readModel;
        }

        [Route("", Order = 0), Produces("application/json")]
        public ActionResult Index() {
            return Ok(new { _readModel.Subscriptions });
        }

        [Route("{subscriptionId:guid}", Order = 1)]
        public ActionResult Subscription(Guid subscriptionId) {
            return Ok();
        }

        [Route("subscribe"), HttpPost]
        public async Task<ActionResult> Subscribe([FromBody] SubscriptionForm form, CancellationToken token) {
            var cmd = new SubscriptionMsgs.Subscribe(form.WebHookId, Guid.NewGuid(), form.Description, form.PostUrl, token);

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

        [Route("{subscriptionId:guid}/enable"), HttpPost]
        public async Task<ActionResult> Enable(Guid subscriptionId, CancellationToken token) {
            var cmd = new SubscriptionMsgs.Enable(subscriptionId, token);

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

        [Route("{subscriptionId:guid}/disable"), HttpPost]
        public async Task<ActionResult> Disable(Guid subscriptionId, CancellationToken token) {
            var cmd = new SubscriptionMsgs.Disable(subscriptionId, token);

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

        [Route("{subscriptionId:guid}/remove"), HttpPost]
        public async Task<ActionResult> Remove(Guid subscriptionId, CancellationToken token) {
            var cmd = new SubscriptionMsgs.Remove(subscriptionId, token);

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
        public Guid WebHookId { get; set; }
        public string PostUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
