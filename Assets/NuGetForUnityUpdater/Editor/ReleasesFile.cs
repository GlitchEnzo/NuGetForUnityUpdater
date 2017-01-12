namespace NuGetForUnityUpdater
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    public class ReleasesFile
    {
        public List<Release> Releases { get; set; }

        //public void Save(string filepath)
        //{

        //}

        public static ReleasesFile Load(string filepath)
        {
            ReleasesFile file = new ReleasesFile();
            file.Releases = new List<Release>();

            XDocument xmlFile = XDocument.Load(filepath);
            var releaseElements = xmlFile.Root.Elements("release");
            foreach (var releaseElement in releaseElements)
            {
                string version = releaseElement.Attribute("version").Value;
                string url = releaseElement.Attribute("url").Value;

                file.Releases.Add(new Release(version, url));
            }

            return file;
        }
    }
}