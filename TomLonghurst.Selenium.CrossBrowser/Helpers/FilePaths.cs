using System.IO;
using System.Reflection;

namespace TomLonghurst.Selenium.CrossBrowser.Helpers
{
    internal class FilePaths
    {
        public static string RawOutputDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string Screenshots => Path.Combine(RawOutputDirectory, "CrossBrowserScreenshots");
    }
}