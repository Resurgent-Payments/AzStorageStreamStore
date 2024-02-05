namespace MvcHost.Models;

public static class ItemForms {
    public class AddItem {
        public Guid ItemId { get; set; }
        public string Name { get; set; } = string.Empty;    
    }

    public class Rename {
        public string Name { get; set; } = string.Empty;
    }
}

