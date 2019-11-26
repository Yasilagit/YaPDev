using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buraq.YaP.Model
{
    public class ConfigurationModel
    {
        //Database
        public string DBDomainName { get; set; }
        public string DBServerOrIp { get; set; }
        public string DBUserName { get; set; }
        public string DBPassword { get; set; }
        public string DBPortNo { get; set; }
        
        //IIS Configuration
        public string IISDomainName { get; set; }
        public string IISUserName { get; set; }
        public string IISPassword { get; set; }
        public string Url { get; set; }
        public string WebPortNo { get; set; }

        
        //Window Service Configuration
        public string WSDomainName { get; set; }
        public string WSUserName { get; set; }
        public string WSPassword { get; set; }
        public string InstallableAppName { get; set; }
    }
}
