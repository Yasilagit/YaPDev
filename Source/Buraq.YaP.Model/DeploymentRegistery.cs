using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buraq.YaP.Model
{
    public class DeploymentRegistry
    {
        public string ApplicationType { get; set; }
        public string ApplicationName { get; set; }
        public string DBConfiguration { get; set; }
        public string IISConfiguration { get; set; }
        public string WSConfiguration { get; set; }
        public string InstallationPath { get; set; }
    }
}
