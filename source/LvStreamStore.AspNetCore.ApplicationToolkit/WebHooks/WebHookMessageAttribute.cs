namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class WebHookMessageAttribute : Attribute {
        public readonly Guid WebHookId;
        public readonly string Name;
        public readonly string Description;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="webHookId">This should be a string representation of a Guid value.</param>
        /// <param name="name">The name of the webhook message</param>
        /// <param name="description">A description of the webhook message</param>
        public WebHookMessageAttribute(string webHookId, string name, string description) {
            WebHookId = Guid.Parse(webHookId);
            Name = name;
            Description = description;
        }
    }
}
