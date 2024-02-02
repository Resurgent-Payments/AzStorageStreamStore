namespace LvStreamStore.ApplicationToolkit.Authorization;

internal class ResolveTenantFromHostnameServiceOptions {
    /// <summary>
    /// When building the internal collection, if we just are given [account_name], and not [account_name].com, we're going to use this value to append
    /// to the "subdomain" so we get a full hostname for resolution.
    /// </summary>
    public string DefaultDomain { get; set; } = string.Empty;
}
