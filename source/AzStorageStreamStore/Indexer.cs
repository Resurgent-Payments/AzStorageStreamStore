namespace AzStorageStreamStore;

using Microsoft.Extensions.Options;

public interface IStreamIndexer {
    void Persist();
}

public class NoIndexer {

}

public class MemoryStreamIndexer : IStreamIndexer {
    public MemoryStreamIndexer(IOptions<IndexConfigurationOptions> options) {

    }

    public void Persist() {
        throw new NotImplementedException();
    }
}

public class IndexConfigurationOptions {
    /// <summary>
    /// The maximum number of categories to build indexes for (e.g. {tenant}/{cat1}/{cat2}/{cat3}/{objId} with a max cat index of 2 would create indexes for:
    /// {tenant}/{cat1}
    /// {tenant}/{cat2}
    /// </summary>
    public int MaximumCategoryDepth { get; set; }
}