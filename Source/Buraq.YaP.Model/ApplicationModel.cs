using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Buraq.YaP.Model
{
    public class ApplicationModel
    {
        public string Name { get; set; }
        public string Ignore { get; set; }
        public string SourcePath { get; set; }
        public string DeployOrder { get; set; }
        public string ReferenceUrl { get; set; }
        public List<string> Database { get; set; }
        public string Type { get; set; }
        public string FolderName { get; set; }
        public string FileName { get; set; }
        public PatchDeploymentModel PatchDeployments { get; set; }
        public List<string> ServiceParameters { get; set; }
        public List<string> FilesToCopy { get; set; }
        public bool IsFolder { get; set; }
        public string CopyToFolder { get; set; }
        public bool Enable32BitMode { get; set; }
        public string PublishProfile { get; set; }
        public string ProjectName { get; set; }
        public int PortNo { get; set; }
        public bool IsLaunch { get; set; }
        
    }
}
