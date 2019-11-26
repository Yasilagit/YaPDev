using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Buraq.YaP.Model;

namespace Buraq.YaP.Helper
{
    public static class Utility
    {
        public static List<ApplicationModel> GetApplicationModel()
        {
            var applicationModels = new List<ApplicationModel>();
            var filePath = GetConfigurationFilePath();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            XmlNodeList xmlNodeLists = xmlDoc.SelectNodes("/Configuration/Applications/Application");
            if (xmlNodeLists != null)
                foreach (XmlNode xmlNode in xmlNodeLists)
                {
                    var applicationModel = new ApplicationModel
                    {
                        Type = xmlNode["Type"]?.InnerText,
                        Name = xmlNode["Name"]?.InnerText,
                        SourcePath = xmlNode["SourcePath"]?.InnerText,
                        DeployOrder = xmlNode["DeployOrder"]?.InnerText,
                        ReferenceUrl = xmlNode["ReferenceUrl"]?.InnerText,
                        FolderName = xmlNode["FolderName"]?.InnerText,
                        FileName = xmlNode["FileName"]?.InnerText,
                        ProjectName = xmlNode["ProjectName"]?.InnerText,
                        PublishProfile = xmlNode["PublishProfile"]?.InnerText,
                        Ignore = xmlNode["Ignore"]?.InnerText
                    };
                    applicationModel.PublishProfile = xmlNode["PublishProfile"]?.InnerText;
                    applicationModel.IsFolder = !string.IsNullOrEmpty(xmlNode["IsFolder"]?.InnerText) &&
                                                Convert.ToBoolean(xmlNode["IsFolder"]?.InnerText.Equals("1"));
                    applicationModel.CopyToFolder = xmlNode["CopyToFolder"]?.InnerText;
                    applicationModel.Enable32BitMode = !string.IsNullOrEmpty(xmlNode["Enable32BitMode"]?.InnerText) &&
                                                       Convert.ToBoolean(xmlNode["Enable32BitMode"]?.InnerText
                                                           .Equals("1"));
                    applicationModel.IsLaunch = !string.IsNullOrEmpty(xmlNode["IsLaunch"]?.InnerText) &&
                                                Convert.ToBoolean(xmlNode["IsLaunch"]?.InnerText.Equals("1"));

                    applicationModel.PortNo = !string.IsNullOrEmpty(xmlNode["PortNo"]?.InnerText) ? Convert.ToInt16(xmlNode["IsLaunch"]?.InnerText) : 0;

                    //Load databases
                    var xmlDatabaseNodes = xmlNode["Databases"]?.ChildNodes;
                    applicationModel.Database = new List<string>();
                    if (xmlDatabaseNodes != null)
                        foreach (XmlNode xmlDatabaseNode in xmlDatabaseNodes)
                        {
                            var dataBaseName = xmlDatabaseNode?.InnerText;
                            applicationModel.Database.Add(dataBaseName);
                        }

                    //Load patchDeployment Items
                    var xmlPatchDeploymentItemNodes = xmlNode["PatchDeployment"]?.ChildNodes;
                    var patchDeployment = new PatchDeploymentModel { Items = new List<string>() };
                    if (xmlPatchDeploymentItemNodes != null)
                        foreach (XmlNode xmlPatchDeploymentItemNode in xmlPatchDeploymentItemNodes)
                        {
                            var nodeName = xmlPatchDeploymentItemNode.Name;
                            if (!string.IsNullOrEmpty(nodeName) &&
                                nodeName.Equals("option", StringComparison.CurrentCultureIgnoreCase))
                            {
                                patchDeployment.Option = xmlPatchDeploymentItemNode.InnerXml;
                                break;
                            }
                        }

                    var xmlPatchNodes =
                        xmlDoc.SelectNodes("/Configuration/Applications/Application/PatchDeployment/Item");
                    if (xmlPatchNodes != null)
                        foreach (XmlNode xmlPatchChild in xmlPatchNodes)
                        {
                            var item = xmlPatchChild.InnerXml;
                            patchDeployment.Items.Add(item);
                        }

                    applicationModel.PatchDeployments = patchDeployment;

                    //Load Service parameters 
                    var xmlServiceParameterItemNodes = xmlNode["ServiceParameters"]?.ChildNodes;
                    applicationModel.ServiceParameters = new List<string>();
                    if (xmlServiceParameterItemNodes != null)
                        foreach (XmlNode xmlServiceParameterItemNode in xmlServiceParameterItemNodes)
                        {
                            var parameter = xmlServiceParameterItemNode?.InnerText;
                            applicationModel.ServiceParameters.Add(parameter);
                        }

                    //Load FilesToCopy parameters 
                    var xmlFilesToCopyItemNodes = xmlNode["FilesToCopy"]?.ChildNodes;
                    applicationModel.FilesToCopy = new List<string>();
                    if (xmlFilesToCopyItemNodes != null)
                        foreach (XmlNode xmlFilesToCopyItemNode in xmlFilesToCopyItemNodes)
                        {
                            var parameter = xmlFilesToCopyItemNode?.InnerText;
                            applicationModel.FilesToCopy.Add(parameter);
                        }

                    applicationModels.Add(applicationModel);
                }
            return applicationModels;
        }

        public static string GetAppSettingByKey(string keyName)
        {
            var filePath = GetConfigurationFilePath();
            var doc = new XmlDocument();
            doc.Load(filePath);

            //Load appSettings configuration
            var appSettingsNodes = doc.SelectNodes("/Configuration/AppSettings/add");
            if (appSettingsNodes != null)
                foreach (XmlNode node in appSettingsNodes)
                {
                    if (node.Attributes == null) continue;
                    if (string.Equals(node.Attributes["key"].Value, keyName,
                        StringComparison.CurrentCultureIgnoreCase))
                    {
                        return node.Attributes["value"].Value;
                    }
                }

            return "";

        }

        public static string GetConfigurationFilePath()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(assemblyPath, "AppSetting.xml");
            return filePath;
        }

        public static string GetConnectionString()
        {
            string filePath = GetConfigurationFilePath();
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            XmlNodeList connectionString = doc.SelectNodes("/Configuration/connectionStrings/add");
            if (connectionString != null)
                foreach (XmlNode node in connectionString)
                {
                    if (node.Attributes != null)
                        return node.Attributes["connectionString"].Value;
                }

            return "";
        }
    }
}
