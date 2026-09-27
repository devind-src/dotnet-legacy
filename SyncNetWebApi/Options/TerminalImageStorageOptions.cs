namespace SyncNetApi.Options
{
    /// <summary>Where PosBase &gt; Base Config &gt; Image (sw_terminal_image) banner/logo files
    /// are stored on disk. Legacy Blazor Server read these straight off local disk
    /// (C:\SwitchNet\Images\Upload) since its pages run server-side; the WASM client can't do
    /// that, so the API serves them back over HTTP from this folder instead (see
    /// Program.cs UseStaticFiles mapping to UrlPrefix).</summary>
    public class TerminalImageStorageOptions
    {
        public const string SectionName = "TerminalImageStorage";

        /// <summary>Absolute or content-root-relative path. Defaults to an App_Data folder
        /// under the API's own content root so it works out of the box in dev without
        /// requiring the legacy C:\SwitchNet\Images\Upload folder to exist.</summary>
        public string UploadDir { get; set; } = "App_Data/terminal-images";

        /// <summary>Public URL path prefix the files are served under.</summary>
        public string UrlPrefix { get; set; } = "/terminal-images";
    }
}
