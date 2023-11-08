namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class WebHookRm {
        private readonly List<ListItem> _webHooks = new();

        public IReadOnlyList<ListItem> WebHooks => _webHooks.AsReadOnly();


        public void RegisterMessage<T>() {
            var attr = typeof(T).GetCustomAttribute<WebHookMessageAttribute>();
            if (attr == null) { return; }
            _webHooks.Add(new ListItem { Id = attr.WebHookId, Name = attr.Name, Description = attr.Description });
        }

        public class ListItem {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
