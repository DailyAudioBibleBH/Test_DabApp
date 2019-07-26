using System;
using System.Collections.Generic;
using System.Text;

namespace DABApp
{
    public class VersionInfo
    {
        public string versionName { get; set; }
        public string platform { get; set; }
        public VersionInfo(string versionName, string platform)
        {
            this.versionName = versionName;
            this.platform = platform;
        }
    }
}
