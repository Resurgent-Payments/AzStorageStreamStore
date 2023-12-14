namespace MvcHost.Models;

public static class ItemForms {
    public class AddItem {
        public Guid ItemId { get; set; }
        public string Name { get; set; }
    }

    public class Rename {
        public string Name { get; set; }
    }
}

