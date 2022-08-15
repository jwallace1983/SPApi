namespace SPApi
{
    public class Settings
    {
        public string Endpoint { get; set; } = "/api/data";

        public bool EnableHelp { get; set; } = false;

        public string HelpKey { get; set; }

        public bool RequireHttps { get; set; } = true;
    }
}
