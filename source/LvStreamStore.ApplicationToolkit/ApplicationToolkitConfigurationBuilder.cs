namespace LvStreamStore;

using System.Runtime.CompilerServices;

using LvStreamStore.ApplicationToolkit;

public class ApplicationToolkitConfigurationBuilder : LvStreamStoreConfigurationBuilder {
    public ApplicationToolkitConfigurationBuilder(LvStreamStoreConfigurationBuilder builder) : base(builder.Builder) {

    }
}
