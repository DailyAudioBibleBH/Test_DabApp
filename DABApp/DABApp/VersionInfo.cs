using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp
{
    public class VersionInfo
    {
        public System.Version versionName { get; set; }
        public string platform { get; set; }
        public VersionInfo(string versionName, string platform)
        {
            this.versionName = new System.Version(versionName);
            this.platform = platform;
        }
    }
}
