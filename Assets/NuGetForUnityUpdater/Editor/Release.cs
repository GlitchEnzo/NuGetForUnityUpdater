namespace NuGetForUnityUpdater
{
    public class Release
    {
        public string Version { get; private set; }

        public string URL { get; private set; }

        public Release(string version, string url)
        {
            Version = version;
            URL = url;
        }
    }
}