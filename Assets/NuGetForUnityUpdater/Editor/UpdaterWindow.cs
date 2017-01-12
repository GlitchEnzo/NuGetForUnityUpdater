namespace NuGetForUnityUpdater
{
    using NugetForUnity;
    using System.Net;
    using UnityEditor;
    using UnityEngine;

    public class UpdaterWindow
    {
        /// <summary>
        /// The filepath to the releases.xml file specifing the NuGetForUnity releases.
        /// </summary>
        public static string ReleasesFilepath = "releases.xml";

        /// <summary>
        /// Checks for a newer version of NuGetForUnity.
        /// </summary>
        [MenuItem("NuGet/Check for Updates", false, 20)]
        protected static void CheckForUpdates()
        {
            //Debug.LogFormat("{0} is currently installed", NugetPreferences.NuGetForUnityVersion);

            // read the releases.xml file
            ReleasesFile releasesFile = ReleasesFile.Load(ReleasesFilepath);

            // determine if there is a version listed that is newer than the installed version
            Release newerVersion = new Release(NugetPreferences.NuGetForUnityVersion, "junk");
            foreach (var release in releasesFile.Releases)
            {
                //Debug.LogFormat("{0} at {1}", release.Version, release.URL);
                if (CompareVersions(NugetPreferences.NuGetForUnityVersion, release.Version) < 0)
                {
                    if (CompareVersions(newerVersion.Version, release.Version) < 0)
                    {
                        newerVersion = release;
                    }
                }
            }

            if (newerVersion.Version != NugetPreferences.NuGetForUnityVersion)
            {
                // prompt user to install
                if (EditorUtility.DisplayDialog("Install Newer Version?", string.Format("Found {0}\n{1} is currently installed", newerVersion.Version, NugetPreferences.NuGetForUnityVersion), "Upgrade", "Keep Current Version"))
                {
                    //Debug.LogFormat("Installing {0}", newerVersion.Version);

                    string downloadedFile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "\\NuGet\\Cache\\NuGetForUnity." + newerVersion.Version + ".unitypackage";

                    // Mono doesn't have a Certificate Authority, so we have to provide all validation manually.  Currently just accept anything.
                    // See here: http://stackoverflow.com/questions/4926676/mono-webrequest-fails-with-https

                    // remove all handlers
                    //if (ServicePointManager.ServerCertificateValidationCallback != null)
                    //    foreach (var d in ServicePointManager.ServerCertificateValidationCallback.GetInvocationList())
                    //        ServicePointManager.ServerCertificateValidationCallback -= (d as System.Net.Security.RemoteCertificateValidationCallback);
                    ServicePointManager.ServerCertificateValidationCallback = null;

                    // add anonymous handler
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policyErrors) => true;

                    using (var client = new WebClient())
                    {
                        client.DownloadFile(newerVersion.URL, downloadedFile);
                    }

                    AssetDatabase.ImportPackage(downloadedFile, true);
                }
            }
            else
            {
                // alternatively inform the user if there is no update available
                EditorUtility.DisplayDialog("No Update Found", string.Format("You are currently using the latest version found"), "OK");
            }
        }

        /// <summary>
        /// Compares two version numbers in the form "1.2". Also supports an optional 3rd and 4th number as well as a prerelease tag, such as "1.3.0.1-alpha2".
        /// Returns:
        /// -1 if versionA is less than versionB
        ///  0 if versionA is equal to versionB
        /// +1 if versionA is greater than versionB
        /// </summary>
        /// <remarks>
        /// This method was copied from NuGetForUnity.NuGetIdentifier.  Perhaps this class should be updated to compare two IDs instead.</remarks>
        /// <param name="versionA">The first version number to compare.</param>
        /// <param name="versionB">The second version number to compare.</param>
        /// <returns>-1 if versionA is less than versionB. 0 if versionA is equal to versionB. +1 if versionA is greater than versionB</returns>
        private static int CompareVersions(string versionA, string versionB)
        {
            try
            {
                string[] splitStringsA = versionA.Split('-');
                versionA = splitStringsA[0];
                string prereleaseA = string.Empty;

                if (splitStringsA.Length > 1)
                {
                    prereleaseA = splitStringsA[1];
                    for (int i = 2; i < splitStringsA.Length; i++)
                    {
                        prereleaseA += "-" + splitStringsA[i];
                    }
                }

                string[] splitA = versionA.Split('.');
                int majorA = int.Parse(splitA[0]);
                int minorA = int.Parse(splitA[1]);
                int patchA = 0;
                if (splitA.Length >= 3)
                {
                    patchA = int.Parse(splitA[2]);
                }
                int buildA = 0;
                if (splitA.Length >= 4)
                {
                    buildA = int.Parse(splitA[3]);
                }

                string[] splitStringsB = versionB.Split('-');
                versionB = splitStringsB[0];
                string prereleaseB = string.Empty;

                if (splitStringsB.Length > 1)
                {
                    prereleaseB = splitStringsB[1];
                    for (int i = 2; i < splitStringsB.Length; i++)
                    {
                        prereleaseB += "-" + splitStringsB[i];
                    }
                }

                string[] splitB = versionB.Split('.');
                int majorB = int.Parse(splitB[0]);
                int minorB = int.Parse(splitB[1]);
                int patchB = 0;
                if (splitB.Length >= 3)
                {
                    patchB = int.Parse(splitB[2]);
                }
                int buildB = 0;
                if (splitB.Length >= 4)
                {
                    buildB = int.Parse(splitB[3]);
                }

                int major = majorA < majorB ? -1 : majorA > majorB ? 1 : 0;
                int minor = minorA < minorB ? -1 : minorA > minorB ? 1 : 0;
                int patch = patchA < patchB ? -1 : patchA > patchB ? 1 : 0;
                int build = buildA < buildB ? -1 : buildA > buildB ? 1 : 0;
                int prerelease = string.Compare(prereleaseA, prereleaseB);

                if (major == 0)
                {
                    // if major versions are equal, compare minor versions
                    if (minor == 0)
                    {
                        if (patch == 0)
                        {
                            // if patch versions are equal, compare build versions
                            if (build == 0)
                            {
                                // if the build versions are equal, just return the prerelease version comparison
                                return prerelease;
                            }

                            // the build versions are different, so use them
                            return build;
                        }

                        // the patch versions are different, so use them
                        return patch;
                    }

                    // the minor versions are different, so use them
                    return minor;
                }

                // the major versions are different, so use them
                return major;
            }
            catch (System.Exception)
            {
                Debug.LogErrorFormat("Compare Error: {0} {1}", versionA, versionB);
                return 0;
            }
        }
    }
}