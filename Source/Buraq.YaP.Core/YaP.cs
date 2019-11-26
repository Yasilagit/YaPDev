using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Buraq.YaP.Core.Properties;
using Buraq.YaP.Helper;
using Buraq.YaP.Model;
using Buraq.YaP.Processor;
using YaP;


namespace Buraq.YaP.Core
{
    public partial class ApplicationSetUp : Form
    {

        public ApplicationSetUp()
        {
            InitializeComponent();
            YaPConfigureLog4Net.ConfigureLog4Net();
        }
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(Processor.YaPProcessor));
        private bool _isButtonClick;
        private List<ApplicationModel> _applicationModels;
        private Dictionary<string, ConfigurationModel> saveConfigurations = new Dictionary<string, ConfigurationModel>();
        private Dictionary<string, ConfigurationModel> tmpSaveConfigurations = new Dictionary<string, ConfigurationModel>();
        private FinalConfiguration.YaPConfigurationDataTable _dtConfiguration = new FinalConfiguration.YaPConfigurationDataTable();

        void Tabs_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == tabStep1 && _isButtonClick)
            {
                e.TabPage.Enabled = false;
            }
            else if (e.TabPage == tabStep1 && !_isButtonClick)
            {
                e.TabPage.Enabled = true;
            }

            //_isButtonClick = false;
        }
        private void IDMApplication_Load(object sender, System.EventArgs e)
        {
            lstChooseComponent.DrawMode = DrawMode.OwnerDrawFixed;
            lstChooseComponent.DrawItem += lstComponent_DrawItem;
            OnFormLoad();
        }

        private void lstMessage_DrawItem(object sender, DrawItemEventArgs e)
        {

        }
        private void lstComponent_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.Items.Count < 0) return;
            e.DrawBackground();
            var outPutItem = "";
            var itemColor = new SolidBrush(Color.Black);

            Graphics g = e.Graphics;
            g.FillRectangle(new SolidBrush(Color.White), e.Bounds);

            if (e.Index == -1) return;

            if (lb.Items[e.Index].ToString().StartsWith("Web~"))
            {
                outPutItem = lb.Items[e.Index].ToString().Replace("Web~", "");
                itemColor = new SolidBrush(Color.Red);
            }
            else if (lb.Items[e.Index].ToString().StartsWith("WindowService~"))
            {
                outPutItem = lb.Items[e.Index].ToString().Replace("WindowService~", "");
                itemColor = new SolidBrush(Color.Blue);
            }
            else if (lb.Items[e.Index].ToString().StartsWith("FileCopy~"))
            {
                outPutItem = lb.Items[e.Index].ToString().Replace("FileCopy~", "");
                itemColor = new SolidBrush(Color.DarkCyan);
            }

            g.DrawString(outPutItem, e.Font, itemColor, new PointF(e.Bounds.X, e.Bounds.Y));
            e.DrawFocusRectangle();

            //upon selected 
            bool isItemSelected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);
            if (isItemSelected)
            {
                int itemIndex = e.Index;
                if (itemIndex >= 0 && itemIndex < lstChooseComponent.Items.Count)
                {
                    Graphics graphics = e.Graphics;

                    // Background Color
                    string colour = "#21AA47";
                    var backColor = Color.FromArgb(Convert.ToByte(colour.Substring(1, 2), 16), Convert.ToByte(colour.Substring(3, 2), 16), Convert.ToByte(colour.Substring(5, 2), 16));

                    SolidBrush backgroundColorBrush = new SolidBrush((isItemSelected) ? backColor : Color.White);
                    graphics.FillRectangle(backgroundColorBrush, e.Bounds);

                    // Set text color
                    string itemText = outPutItem;

                    SolidBrush itemTextColorBrush = (isItemSelected) ? new SolidBrush(Color.White) : new SolidBrush(Color.Black);
                    g.DrawString(itemText, e.Font, itemTextColorBrush, lstChooseComponent.GetItemRectangle(itemIndex).Location);

                    // Clean up
                    backgroundColorBrush.Dispose();

                }

                e.DrawFocusRectangle();

            }

        }
        private void lstDisplayConfiguration_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            if (lb.Items.Count < 0) return;
            e.DrawBackground();
            var outPutItem = "";
            var itemColor = new SolidBrush(Color.Red);

            Graphics g = e.Graphics;
            g.FillRectangle(new SolidBrush(Color.White), e.Bounds);

            if (e.Index == -1) return;
            var tickMark = "✓";
            var tickMarckColor = new SolidBrush(Color.Green);
            outPutItem = lb.Items[e.Index].ToString();
            //g.DrawString(tickMark, e.Font, tickMarckColor, new PointF(e.Bounds.X, e.Bounds.Y));
            g.DrawString(tickMark + outPutItem, e.Font, itemColor, new PointF(e.Bounds.X, e.Bounds.Y));
            e.DrawFocusRectangle();
        }

        private void SetReadonlyControls(Control.ControlCollection controlCollection, bool status)
        {
            if (controlCollection == null)
            {
                return;
            }

            foreach (var c in controlCollection.OfType<TextBoxBase>())
            {
                c.Enabled = status;
            }

            foreach (var c in controlCollection.OfType<RadioButton>())
            {
                c.Enabled = status;
            }

            foreach (var c in controlCollection.OfType<CheckBox>())
            {
                c.Enabled = status;
            }
        }

        private void ClearControls(Control.ControlCollection controlCollection)
        {
            if (controlCollection == null)
            {
                return;
            }

            foreach (var c in controlCollection.OfType<TextBoxBase>())
            {
                c.Text = "";
            }

            //foreach (var c in controlCollection.OfType<RadioButton>())
            //{
            //    c.sel = status;
            //}
        }

        private void OnFormLoad()
        {
            log.Info("Application started...");
            try
            {
                _applicationModels = Utility.GetApplicationModel();
                cmbApplicationType.SelectedIndex = cmbApplicationType.Items.IndexOf("Select");
                cmbCredentialsType.SelectedIndex = cmbCredentialsType.Items.IndexOf("Select");
                cmbCredentialsType.Enabled = false;
                var projectName = Utility.GetAppSettingByKey("ProjectName");
                var versionNo = "";
                var versionNoConfig = Utility.GetAppSettingByKey("Version");
                if (!string.IsNullOrEmpty(versionNo))
                {
                    versionNo = versionNoConfig;
                }
                else
                {
                    versionNo = "5.0.0.72";
                }
                Text = !string.IsNullOrEmpty(projectName) ? projectName + " " + versionNo : Assembly.GetExecutingAssembly().GetName().Name + " " + versionNo;
                lnkCreatePackage.Visible = !string.IsNullOrEmpty(Utility.GetAppSettingByKey("EnablePackaging")) && Utility.GetAppSettingByKey("EnablePackaging").Equals("1", StringComparison.CurrentCultureIgnoreCase);

                EnableOrDisableCredentialGrpBox();
                SetReadonlyControls(grpInstallation.Controls, true);
                SetReadonlyControls(grpApplicationType.Controls, false);
                SetReadonlyControls(grpChooseComponent.Controls, false);
                btnStep2Cancel.Enabled = false;
                lnkConfigAppAdd.Enabled = false;
                lnkConfigAppClear.Enabled = false;
                btnStep2Next.Enabled = false;
                lblMessage.Visible = false;

                tabYaP.TabPages[1].Enabled = false;
                tabYaP.TabPages[2].Enabled = false;
                tabYaP.TabPages[3].Enabled = false;

                var isGenerateKeyEnabled = Utility.GetAppSettingByKey("ProductKeyGenerate");
                lnkCreateProductKey.Visible = !string.IsNullOrEmpty(isGenerateKeyEnabled) && isGenerateKeyEnabled == "1";
            }
            catch (Exception ex)
            {
                tabYaP.Enabled = false;
                log.Error("While Loading Application", ex);
                lblMessage.Visible = true;
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Error while Loading";
            }

        }

        private Tuple<string, string, string> GetHostAndCurrentUser()
        {
            var fullyQualifiedName = "";
            var currentUserName = "";
            var ipv4Addresses = "";
            var machineName = Environment.MachineName;
            fullyQualifiedName = System.Net.Dns.GetHostEntry(machineName).HostName;
            currentUserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            var hostName = Dns.GetHostName();
            //TODO: Need to revisit to get ipv4 address.
            ipv4Addresses = Array.FindAll(Dns.GetHostEntry(hostName).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork)[0].ToString();
            return new Tuple<string, string, string>(fullyQualifiedName, currentUserName, ipv4Addresses);
        }

        private void EnableOrDisableCredentialGrpBox(bool status = false)
        {
            txtAppsIISDomainName.Enabled = status;
            txtAppsIISUserName.Enabled = status;
            txtAppsIISPassword.Enabled = status;
            chkAppsWSApplicable.Enabled = status;

            txtAppsWSDomainName.Enabled = status;
            txtAppsWSUserName.Enabled = status;
            txtAppsWSPassword.Enabled = status;

            txtConfigDBDomainName.Enabled = status;
            txtConfigDBserverOrIp.Enabled = status;
            txtConfigDBUserName.Enabled = status;
            if (status)
            {
                txtConfigDBPortNo.Text = "1433";
            }
            else
            {
                txtConfigDBPortNo.Text = "";
            }
            txtConfigDBPortNo.Enabled = status;
            txtConfigDBPassword.Enabled = status;
            cmbApplicationType.Enabled = status;


        }

        private void DeploymentConfiguration_Click(object sender, System.EventArgs e)
        {

        }

        private void btnNext_Click(object sender, System.EventArgs e)
        {
            this.tabYaP.SelectedTab = this.tabYaP.TabPages[2];
            DisplaySelectedOption();
        }

        private void DisplaySelectedOption()
        {

        }

        private void lnkInstall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lnkCreatePackage.Visible = false;
            lnkCreateProductKey.Visible = false;
            lnkPackageCancel.Visible = false;
            lnkCancelInstall.Visible = true;
            pnlProductKey.Visible = true;
            txtProductkey.Visible = true;
            txtInstallPath.Visible = true;
            btnOKInstall.Visible = true;
            btnOk.Visible = false;
            pnlInstalationPath.Visible = true;
            lnkInstall.Visible = true;
            pnlProductKey.Location = new Point(285, 160);
            pnlInstalationPath.Location = new Point(221, 200);
            lnkUnInstall.Visible = false;
            txtInstallPath.Text = Utility.GetAppSettingByKey("InstallationFolder");
            tabYaP.TabPages[1].Enabled = true;

        }

        private void btnFinish_Click(object sender, System.EventArgs e)
        {
            this.tabYaP.SelectedTab = this.tabYaP.TabPages[3];
        }

        private void btnStep3Previous_Click(object sender, EventArgs e)
        {
            this.tabYaP.SelectedTab = this.tabYaP.TabPages[1];
            saveConfigurations.Clear();
            lstDisplayConfiguration.Items.Clear();

            if (rdbComplete.Checked)
            {
                LoadComponentList("All");
                cmbCredentialsType.Enabled = false;
                cmbCredentialsType.SelectedIndex = cmbCredentialsType.Items.IndexOf("Select");
                cmbApplicationType.Enabled = true;
                ClearControls(grpConfigDBCredentials.Controls);
                btnStep2Next.Enabled = false;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lnkCreatePackage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lnkCreateProductKey.Visible = false;
            lnkInstall.Visible = false;
            lnkUnInstall.Visible = false;

            txtProductkey.Visible = true;
            btnOk.Visible = true;
            pnlProductKey.Visible = true;
            pnlProductKey.Location = new Point(285, 160);
            txtProductkey.ReadOnly = false;
            lnkCreatePackage.Visible = false;

        }

        private Dictionary<string, string> GetAppSettings()
        {
            var appSettings = new Dictionary<string, string>();
            var filePath = Utility.GetConfigurationFilePath();
            var doc = new XmlDocument();
            doc.Load(filePath);

            //Load appSettings configuration
            var appSettingsNodes = doc.SelectNodes("/Configuration/AppSettings/add");
            if (appSettingsNodes == null) return appSettings;
            foreach (XmlNode node in appSettingsNodes)
            {
                if (node.Attributes != null)
                    appSettings.Add(node.Attributes["key"].Value, node.Attributes["value"].Value);
            }
            return appSettings;

        }


        private async void lnkUnInstall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            var appSettings = GetAppSettings();
            var applicationModel = Utility.GetApplicationModel();
            var installer = new YaPProcessor(appSettings, applicationModel, null, txtProductkey.Text);
            var result = await installer.InvokeUnInstallationAsync(txtProductkey.Text);
            //Un-Install
        }

        private void cmbApplicationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadComponentList(cmbApplicationType.Text);
            if (lstChooseComponent.Items.Count == 0)
            {
                cmbCredentialsType.Enabled = false;
                cmbCredentialsType.SelectedIndex = cmbCredentialsType.Items.IndexOf("Select");
            }
        }

        private void lstChooseComponent_SelectedIndexChanged(object sender, EventArgs e)
        {

            cmbCredentialsType.Enabled = false;
            if (lstChooseComponent.SelectedItems.Count == 0)
            {
                SetReadonlyControls(grpInstallation.Controls, true);
                cmbApplicationType.Enabled = true;
                return;
            }
            cmbCredentialsType.Enabled = true;
            lblMessage.Text = "";
            lblMessage.Visible = false;
            lblMessage.Text = "";

            SetReadonlyControls(grpInstallation.Controls, false);
            cmbApplicationType.Enabled = false;
            if (rdbPatch.Checked)
            {
                btnStep2Next.Enabled = true;
                cmbCredentialsType.Enabled = false;
            }
            ListBox lb = (ListBox)sender;
            if (lstChooseComponent.SelectedItems.Count > 1)
            {
                var listOfSelectedItems = lstChooseComponent.SelectedItems.Cast<String>().ToList();
                bool fileCopyAny = listOfSelectedItems.Any(l => l.Contains("FileCopy~"));
                bool webAny = listOfSelectedItems.Any(l => l.Contains("Web~"));
                bool windowServiceAny = listOfSelectedItems.Any(l => l.Contains("WindowService~"));

                if (fileCopyAny && (webAny || windowServiceAny))
                {
                    lblMessage.Visible = true;
                    lblMessage.Text = $"Unable to select FileCopy && Other project type together!";
                    lstChooseComponent.SelectedIndex = -1;
                    return;
                }
            }
            if (lb.Text.StartsWith("FileCopy"))
            {
                SetReadonlyControls(grpConfigDBCredentials.Controls, false);
                SetReadonlyControls(grpConfigIISCredentials.Controls, false);
                SetReadonlyControls(grpConfigWSCredentials.Controls, false);
                cmbCredentialsType.Enabled = false;
                lnkConfigAppClear.Enabled = false;
                lnkConfigAppAdd.Enabled = false;
            }
            else
            {
                lnkConfigAppClear.Enabled = false;
                lnkConfigAppAdd.Enabled = false;
            }


        }

        private void chkAppsWSApplicable_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAppsWSApplicable.Checked)
            {
                txtAppsWSDomainName.Text = txtAppsIISDomainName.Text;
                txtAppsWSUserName.Text = txtAppsIISUserName.Text;
                txtAppsWSPassword.Text = txtAppsIISPassword.Text;

                txtAppsWSDomainName.Enabled = false;
                txtAppsWSUserName.Enabled = false;
                txtAppsWSPassword.Enabled = false;
                txtBoxControl_Leave(sender, e);


            }
            else
            {
                txtAppsWSPassword.Text = "";
                txtAppsWSDomainName.Enabled = true;
                txtAppsWSUserName.Enabled = true;
                txtAppsWSPassword.Enabled = true;
            }
        }

        private void chkConfigApplicableAll_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void SetReadOnlyDBConfig(bool status = false)
        {

        }

        private void LoadComponentList(string applicationTye)
        {
            if (applicationTye?.ToLower() == "select")
            {
                lstChooseComponent.Items.Clear();
                EnableOrDisableCredentialGrpBox();
            }
            else
            {
                lstChooseComponent.Items.Clear();
                List<ApplicationModel> listOfApplications;
                if (cmbApplicationType.Text != "All")
                {
                    var listofApplicationByType = (from apps in _applicationModels
                                                   where (!string.IsNullOrEmpty(apps.Name) && apps.Type == cmbApplicationType.Text)
                                                   select apps).ToList();
                    listOfApplications = listofApplicationByType;
                }
                else
                {
                    var listOfAllpplication = (from apps in _applicationModels
                                               where (!string.IsNullOrEmpty(apps.Name))
                                               select apps).ToList();
                    listOfApplications = listOfAllpplication;

                }

                foreach (var application in listOfApplications)
                {
                    if (application.Type.Equals("Web", StringComparison.OrdinalIgnoreCase))
                    {
                        application.Name = "Web~" + application.Name;
                    }
                    else if (application.Type.Equals("WindowService", StringComparison.OrdinalIgnoreCase))
                    {
                        application.Name = "WindowService~" + application.Name;
                    }
                    else if (application.Type.Equals("FileCopy", StringComparison.OrdinalIgnoreCase))
                    {
                        application.Name = "FileCopy~" + application.Name;
                    }
                    else if (application.Type.Equals("Database", StringComparison.OrdinalIgnoreCase))
                    {
                        application.Name = "Database~" + application.Name;
                    }
                    lstChooseComponent.Items.Add(application.Name);
                }


            }

        }

        private void btnExitDeployment_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cmbCredentialsType_SelectedIndexChanged(object sender, EventArgs e)
        {

            chkAppsWSApplicable.Enabled = false;
            if (cmbCredentialsType.SelectedIndex == 0)
            {
                SetReadonlyControls(grpConfigIISCredentials.Controls, false);
                SetReadonlyControls(grpConfigDBCredentials.Controls, false);
                SetReadonlyControls(grpConfigWSCredentials.Controls, false);

            }
            else if (cmbCredentialsType.SelectedIndex == 1) //Database
            {
                SetReadonlyControls(grpConfigIISCredentials.Controls, false);
                SetReadonlyControls(grpConfigWSCredentials.Controls, false);
                SetReadonlyControls(grpConfigDBCredentials.Controls, true);
                LoadDefaultConfiguration();
                ClearControls(grpConfigIISCredentials.Controls);
                ClearControls(grpConfigWSCredentials.Controls);
                tabCredentialInfo.SelectedTab = tabCredentialInfo.TabPages[0];

            }
            else if (cmbCredentialsType.SelectedIndex == 2) //IIS
            {
                SetReadonlyControls(grpConfigIISCredentials.Controls, true);
                SetReadonlyControls(grpConfigDBCredentials.Controls, false);
                SetReadonlyControls(grpConfigWSCredentials.Controls, false);
                LoadDefaultConfiguration("iis");
                ClearControls(grpConfigDBCredentials.Controls);
                ClearControls(grpConfigWSCredentials.Controls);
                tabCredentialInfo.SelectedTab = tabCredentialInfo.TabPages[1];

            }
            else if (cmbCredentialsType.SelectedIndex == 3) //WindowService
            {
                SetReadonlyControls(grpConfigIISCredentials.Controls, false);
                SetReadonlyControls(grpConfigDBCredentials.Controls, false);
                SetReadonlyControls(grpConfigWSCredentials.Controls, true);
                LoadDefaultConfiguration("ws");
                ClearControls(grpConfigDBCredentials.Controls);
                ClearControls(grpConfigIISCredentials.Controls);
                tabCredentialInfo.SelectedTab = tabCredentialInfo.TabPages[1];

            }
            else if (cmbCredentialsType.SelectedIndex == 4) // IIS & Database
            {
                SetReadonlyControls(grpConfigIISCredentials.Controls, true);
                SetReadonlyControls(grpConfigDBCredentials.Controls, true);
                SetReadonlyControls(grpConfigWSCredentials.Controls, false);
                LoadDefaultConfiguration();
                LoadDefaultConfiguration("iis");
                ClearControls(grpConfigWSCredentials.Controls);
                tabCredentialInfo.SelectedTab = tabCredentialInfo.TabPages[0];

            }
            else if (cmbCredentialsType.SelectedIndex == 5) // Database & WindowService
            {
                SetReadonlyControls(grpConfigIISCredentials.Controls, false);
                SetReadonlyControls(grpConfigDBCredentials.Controls, true);
                SetReadonlyControls(grpConfigWSCredentials.Controls, true);
                LoadDefaultConfiguration();
                LoadDefaultConfiguration("ws");
                ClearControls(grpConfigIISCredentials.Controls);
                tabCredentialInfo.SelectedTab = tabCredentialInfo.TabPages[0];
            }
            else if (cmbCredentialsType.SelectedIndex == 6) // IIS & WindowService
            {
                SetReadonlyControls(grpConfigIISCredentials.Controls, true);
                SetReadonlyControls(grpConfigDBCredentials.Controls, false);
                SetReadonlyControls(grpConfigWSCredentials.Controls, true);
                chkAppsWSApplicable.Enabled = true;
                LoadDefaultConfiguration("ws");
                LoadDefaultConfiguration("iis");
                ClearControls(grpConfigDBCredentials.Controls);
                tabCredentialInfo.SelectedTab = tabCredentialInfo.TabPages[1];

            }
            else if (cmbCredentialsType.SelectedIndex == 7) // All
            {
                ClearControls(grpConfigDBCredentials.Controls);
                ClearControls(grpConfigIISCredentials.Controls);
                ClearControls(grpConfigWSCredentials.Controls);

                SetReadonlyControls(grpConfigIISCredentials.Controls, true);
                SetReadonlyControls(grpConfigDBCredentials.Controls, true);
                SetReadonlyControls(grpConfigWSCredentials.Controls, true);
                chkAppsWSApplicable.Enabled = true;
                LoadDefaultConfiguration();
                LoadDefaultConfiguration("iis");
                LoadDefaultConfiguration("ws");
                tabCredentialInfo.SelectedTab = tabCredentialInfo.TabPages[0];
            }
        }

        private void LoadDefaultConfiguration(string configType = "db")
        {
            var returnConfigInfo = GetHostAndCurrentUser();
            var fullyQualifiedName = returnConfigInfo.Item1;
            var currentUserName = returnConfigInfo.Item2;
            var ipAddress = returnConfigInfo.Item3;
            if (configType.Equals("db", StringComparison.CurrentCultureIgnoreCase))
            {
                txtConfigDBDomainName.Text = fullyQualifiedName;
                txtConfigDBUserName.Text = currentUserName;
                txtConfigDBPortNo.Text = "1433";
                txtConfigDBserverOrIp.Text = ipAddress;
            }
            else if (configType.Equals("iis", StringComparison.CurrentCultureIgnoreCase))
            {
                txtAppsIISDomainName.Text = fullyQualifiedName;
                txtAppsIISUserName.Text = currentUserName;
            }
            else if (configType.Equals("ws", StringComparison.CurrentCultureIgnoreCase))
            {
                txtAppsWSDomainName.Text = fullyQualifiedName;
                txtAppsWSUserName.Text = currentUserName;
            }

        }
        private void rdbComplete_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbComplete.Checked)
            {
                SetReadonlyControls(grpChooseComponent.Controls, true);
                btnStep2Cancel.Enabled = true;
                //SetReadonlyControls(grpInstallation.Controls, false);
                cmbApplicationType.SelectedIndex = cmbApplicationType.Items.IndexOf("All");
                cmbApplicationType.Enabled = true;
            }


        }

        private void rdbPatch_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbPatch.Checked)
            {
                SetReadonlyControls(grpApplicationType.Controls, true);
                SetReadonlyControls(grpChooseComponent.Controls, true);
                btnStep2Cancel.Enabled = true;
                //SetReadonlyControls(grpInstallation.Controls, false);
                cmbApplicationType.SelectedIndex = cmbApplicationType.Items.IndexOf("Web");
                cmbApplicationType.Enabled = true;

            }
        }

        private void btnCancelDeployment_Click(object sender, EventArgs e)
        {

        }

        private void btnCompleteInstallation_Click(object sender, EventArgs e)
        {

        }

        private void lnkConfigAppAdd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RequiredFieldValidation();
            saveConfigurations.Clear();
            var removeItems = new List<string>();
            if (lstChooseComponent.SelectedItems.Count > 0)
            {
                foreach (var item in lstChooseComponent.SelectedItems)
                {
                    if (item.ToString().Split('~')[0]
                        .StartsWith("filecopy", StringComparison.CurrentCultureIgnoreCase)) continue;
                    var selectedConfiguration = new ConfigurationModel();
                    selectedConfiguration.DBDomainName = txtConfigDBDomainName.Text;
                    selectedConfiguration.DBServerOrIp = txtConfigDBserverOrIp.Text;
                    selectedConfiguration.DBUserName = txtConfigDBUserName.Text;
                    selectedConfiguration.DBPassword = txtConfigDBPassword.Text;
                    selectedConfiguration.DBPortNo = txtConfigDBPortNo.Text;
                    selectedConfiguration.IISDomainName = txtAppsIISDomainName.Text;
                    selectedConfiguration.IISUserName = txtAppsIISUserName.Text;
                    selectedConfiguration.IISPassword = txtAppsIISPassword.Text;
                    selectedConfiguration.WSDomainName = txtAppsWSDomainName.Text;
                    selectedConfiguration.WSUserName = txtAppsWSUserName.Text;
                    selectedConfiguration.WSPassword = txtAppsWSPassword.Text;
                    var appKey = item.ToString().Split('~')[0];
                    var appName = item.ToString().Split('~')[1];
                    selectedConfiguration.InstallableAppName = appName;
                    if (saveConfigurations.ContainsKey(appKey)) continue;
                    saveConfigurations.Add(appKey, selectedConfiguration);
                    removeItems.Add(item.ToString());
                }
                foreach (var item in removeItems)
                {
                    lstChooseComponent.Items.Remove(item);
                }
            }
            cmbCredentialsType.Enabled = false;
            lnkConfigAppClear.Enabled = false;
            lnkConfigAppAdd.Enabled = false;
            btnStep2Next.Enabled = true;
            SetReadonlyControls(grpInstallation.Controls, false);

        }

        private void btnStep2Next_Click(object sender, EventArgs e)
        {
            foreach (var configuration in saveConfigurations)
            {

                var existingConfiguration = (from config in _dtConfiguration.AsEnumerable()
                                             where config.ApplicationName.Equals(configuration.Key)
                                             select config).FirstOrDefault();

                if (existingConfiguration != null)
                {
                    existingConfiguration.ApplicationName = configuration.Key;
                    existingConfiguration.InstallableAppName = configuration.Value.InstallableAppName;
                    existingConfiguration.IISDomainName = configuration.Value.IISDomainName;
                    existingConfiguration.IISUserName = configuration.Value.IISUserName;
                    existingConfiguration.IISPassword = configuration.Value.IISPassword;
                    existingConfiguration.WebPortNo = "";
                    existingConfiguration.Url = "";
                    existingConfiguration.DBServerOrIP = configuration.Value.DBServerOrIp;
                    existingConfiguration.DBPortNo = configuration.Value.DBPortNo;
                    existingConfiguration.DBDomainName = configuration.Value.DBDomainName;
                    existingConfiguration.DBUserName = configuration.Value.DBUserName;
                    existingConfiguration.DBPassword = configuration.Value.DBPassword;
                    existingConfiguration.WSDomainName = configuration.Value.WSDomainName;
                    existingConfiguration.WSUserName = configuration.Value.WSUserName;
                    existingConfiguration.WSPassword = configuration.Value.WSPassword;
                }
                else
                {
                    var drConfiguration = _dtConfiguration.NewYaPConfigurationRow();
                    drConfiguration.ApplicationName = configuration.Key;
                    drConfiguration.InstallableAppName = configuration.Value.InstallableAppName;
                    drConfiguration.IISDomainName = configuration.Value.IISDomainName;
                    drConfiguration.IISUserName = configuration.Value.IISUserName;
                    drConfiguration.IISPassword = configuration.Value.IISPassword;
                    drConfiguration.WebPortNo = "";
                    drConfiguration.Url = "";
                    drConfiguration.DBServerOrIP = configuration.Value.DBServerOrIp;
                    drConfiguration.DBPortNo = configuration.Value.DBPortNo;
                    drConfiguration.DBDomainName = configuration.Value.DBDomainName;
                    drConfiguration.DBUserName = configuration.Value.DBUserName;
                    drConfiguration.DBPassword = configuration.Value.DBPassword;
                    drConfiguration.WSDomainName = configuration.Value.WSDomainName;
                    drConfiguration.WSUserName = configuration.Value.WSUserName;
                    drConfiguration.WSPassword = configuration.Value.WSPassword;
                    _dtConfiguration.Rows.Add(drConfiguration);
                }

                var drFinalConfigurationRow = (from config in _dtConfiguration.AsEnumerable()
                                               where config.ApplicationName.Equals(configuration.Key)
                                               select config).First();


                if (configuration.Key.Equals("web", StringComparison.CurrentCultureIgnoreCase))
                {
                    var iisConfiguration = new StringBuilder();
                    lstDisplayConfiguration.Items.Add(configuration.Key);
                    lstDisplayConfiguration.Items.Add("----------------------------------------------------------------------------------");
                    iisConfiguration.Append("IISDomainName = " + drFinalConfigurationRow.IISDomainName + "; ");
                    iisConfiguration.Append("IISUserName = " + drFinalConfigurationRow.IISUserName + "; ");
                    iisConfiguration.Append("IISPassword = " + ReplaceAll(drFinalConfigurationRow.DBPassword, '*') + ";");
                    lstDisplayConfiguration.Items.Add(iisConfiguration);
                    lstDisplayConfiguration.Items.Add("\n");
                }
                else if (configuration.Key.Equals("windowservice", StringComparison.CurrentCultureIgnoreCase))
                {
                    var wsConfiguration = new StringBuilder();
                    wsConfiguration.Append("WSDomainName = " + drFinalConfigurationRow.WSDomainName + "; ");
                    wsConfiguration.Append("WSUserName = " + drFinalConfigurationRow.WSUserName + "; ");
                    wsConfiguration.Append("WSPassword = " + ReplaceAll(drFinalConfigurationRow.DBPassword, '*') + ";");
                    lstDisplayConfiguration.Items.Add(wsConfiguration);
                    lstDisplayConfiguration.Items.Add("\n");
                }

                var applicationModel = _applicationModels.FirstOrDefault(a => a.Name.Split('~')[0].Equals(configuration.Key, StringComparison.CurrentCultureIgnoreCase));

                if (applicationModel == null || applicationModel.Database.Count <= 0) continue;
                var dbConfiguration = new StringBuilder();
                dbConfiguration.Append("DBServerOrIp = " + drFinalConfigurationRow.DBServerOrIP + "; ");
                dbConfiguration.Append("DBPortNo = " + drFinalConfigurationRow.DBPortNo + "; ");
                dbConfiguration.Append("DBDomainName = " + drFinalConfigurationRow.DBDomainName + "; ");
                dbConfiguration.Append("DBUserName = " + drFinalConfigurationRow.DBUserName + "; ");
                dbConfiguration.Append("DBPassword = " + ReplaceAll(drFinalConfigurationRow.DBPassword, '*') + ";");
                lstDisplayConfiguration.Items.Add(dbConfiguration);
                lstDisplayConfiguration.Items.Add("\n");

            }

            lstDisplayConfiguration.Visible = true;
            tabYaP.SelectedTab = tabYaP.TabPages[2];
            tabYaP.TabPages[2].Enabled = true;
            saveConfigurations.Clear();
        }

        private DataTable GenerateConfigurationDataTable()
        {
            var dtConfiguration = new DataTable("FinalConfiguration");
            var dcApplicationName = new DataColumn("ApplicationName", typeof(string));
            var dcIISDomainName = new DataColumn("IISDomainName", typeof(string));
            var dcIISUserName = new DataColumn("IISUserName", typeof(string));
            var dcIISPassword = new DataColumn("IISPassword", typeof(string));
            var dcIISWebPortNo = new DataColumn("IISWebPortNo", typeof(string));
            var dcUrl = new DataColumn("Url", typeof(string));
            var dcWSDomainName = new DataColumn("WSDomainName", typeof(string));
            var dcWSUserName = new DataColumn("WSUserName", typeof(string));
            var dcWSPassword = new DataColumn("WSPassword", typeof(string));
            var dcDBServerOrIp = new DataColumn("DBServerOrIp", typeof(string));
            var dcDBPortNo = new DataColumn("DBPortNo", typeof(string));
            var dcDBDomainName = new DataColumn("DBDomainName", typeof(string));
            var dcDBUserName = new DataColumn("DBUserName", typeof(string));
            var dcDBPassword = new DataColumn("WSPassword", typeof(string));

            dtConfiguration.Columns.Add(dcApplicationName);
            dtConfiguration.Columns.Add(dcIISDomainName);
            dtConfiguration.Columns.Add(dcIISUserName);
            dtConfiguration.Columns.Add(dcIISPassword);
            dtConfiguration.Columns.Add(dcIISWebPortNo);
            dtConfiguration.Columns.Add(dcUrl);
            dtConfiguration.Columns.Add(dcWSDomainName);
            dtConfiguration.Columns.Add(dcWSUserName);
            dtConfiguration.Columns.Add(dcWSPassword);
            dtConfiguration.Columns.Add(dcDBDomainName);
            dtConfiguration.Columns.Add(dcDBUserName);
            dtConfiguration.Columns.Add(dcDBPassword);

            return dtConfiguration;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        string ReplaceAll(string input, char target)
        {
            StringBuilder sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                sb.Append(target);
            }

            return sb.ToString();
        }
        private async void btnInstall_Click(object sender, EventArgs e)
        {

            string installationPath = txtInstallPath.Text;
            string productKey = txtProductkey.Text;
            if (string.IsNullOrEmpty(installationPath))
            {
                installationPath = Utility.GetAppSettingByKey("InstallationFolder");
                txtInstallPath.Text = "";
            }

            if (string.IsNullOrEmpty(installationPath))
            {
                lblMessage.Visible = true;
                lblMessage.Text = "Installation Cancelled";
                return;
            }
            lblMessage.Visible = false;
            lblMessage.Text = "";
            //Load AppSetting values
            var appSettings = GetAppSettings();
            var applicationModel = Utility.GetApplicationModel();
            if (appSettings == null || applicationModel == null) return;


            List<bool> isSuccess = new List<bool>();
            List<string> listOfInstallationType = new List<string> { "Database", "Web", "WindowService", "FileCopy" };
            var installableAppNames = (from config in _dtConfiguration.AsEnumerable()
                                       select config.InstallableAppName).ToList();


            foreach (var type in listOfInstallationType)
            {
                var existingConfiguration = (from config in _dtConfiguration.AsEnumerable()
                                             where config.ApplicationName.Equals(type, StringComparison.CurrentCultureIgnoreCase)
                                             select config).FirstOrDefault();

                if (existingConfiguration == null) continue;

                var configurationModel = new ConfigurationModel
                {
                    IISDomainName = existingConfiguration.IISDomainName,
                    IISUserName = existingConfiguration.IISUserName,
                    IISPassword = existingConfiguration.IISPassword,
                    DBServerOrIp = existingConfiguration.DBServerOrIP,
                    DBDomainName = existingConfiguration.DBDomainName,
                    DBPortNo = existingConfiguration.DBPortNo,
                    DBUserName = existingConfiguration.DBUserName,
                    DBPassword = existingConfiguration.DBPassword,
                    WSDomainName = existingConfiguration.WSDomainName,
                    WSUserName = existingConfiguration.WSUserName,
                    WSPassword = existingConfiguration.WSPassword,
                    InstallableAppName = existingConfiguration.InstallableAppName
                };

                var configuration = new Dictionary<string, ConfigurationModel> { { type, configurationModel } };
                var installer = new YaPProcessor(appSettings, applicationModel, configuration, txtProductkey.Text);
                tabYaP.SelectedTab = tabYaP.TabPages[3];
                tabYaP.TabPages[0].Enabled = false;
                tabYaP.TabPages[1].Enabled = false;
                tabYaP.TabPages[2].Enabled = false;
                _isButtonClick = true;
                btnLaunch.Visible = false;
                this.tabYaP.SelectedTab = this.tabYaP.TabPages[3];
                this.tabYaP.TabPages[3].Enabled = true;
                lblMessage.Text = "Installation InProgress";
                lblMessage.ForeColor = Color.Blue;
                pcbMessage.Image = Resources.Packaging;
                pcbMessage.Visible = true;
                lblMessage.Visible = true;
                var isPatchDeployment = rdbPatch.Checked;
                var installationResult = await installer.InvokeInstallationAsync(installationPath, productKey, isPatchDeployment, installableAppNames);
                isSuccess.Add(installationResult.Item2);
                foreach (var item in installationResult.Item1)
                {
                    lstMessage.Items.Add(item);
                }


            }

            if (isSuccess.Contains(false))
            {
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Failed Installation";
                pcbMessage.Image = Resources.Failed;
                return;


            }
            if (!isSuccess.Contains(false))
            {
                lblMessage.Text = "Installation completed Successfully";
                lblMessage.ForeColor = Color.Green;
                pcbMessage.Image = Resources.Completed;
            }

        }

        private void RequiredFieldValidation()
        {
            bool canAdd = true;
            TextBox[] validaTextBoxs = new TextBox[] { };

            TextBox[] textBoxesIIS =
            {
                txtAppsIISDomainName,txtAppsIISUserName,txtAppsIISPassword,
            };
            TextBox[] textBoxesDB =
            {
                //txtConfigDBDomainName, txtConfigDBserverOrIp, txtConfigDBPortNo, txtConfigDBUserName, txtConfigDBPassword,

                txtConfigDBDomainName, txtConfigDBserverOrIp, txtConfigDBUserName, txtConfigDBPassword,
            };
            TextBox[] textBoxesWS =
            {
               txtAppsWSDomainName,txtAppsWSUserName,txtAppsWSPassword
            };

            //Database
            //IIS
            //WindowService
            //Database & IIS
            //Database & WindowService
            //IIS & WindowService
            //All

            if (cmbCredentialsType.SelectedIndex == 1) //Database check
            {
                validaTextBoxs = textBoxesDB;
            }
            else if (cmbCredentialsType.SelectedIndex == 2) //IIS Check
            {
                validaTextBoxs = textBoxesIIS;
            }
            else if (cmbCredentialsType.SelectedIndex == 3) //WS Check
            {
                validaTextBoxs = textBoxesWS;
            }
            else if (cmbCredentialsType.SelectedIndex == 4) //Database & IIS
            {
                validaTextBoxs = textBoxesDB.Concat(textBoxesIIS).ToArray();
            }
            else if (cmbCredentialsType.SelectedIndex == 5) //Database & WS
            {
                validaTextBoxs = textBoxesDB.Concat(textBoxesWS).ToArray();
            }

            else if (cmbCredentialsType.SelectedIndex == 6) //IIS & WS
            {
                validaTextBoxs = textBoxesIIS.Concat(textBoxesWS).ToArray();

            }
            else if (cmbCredentialsType.SelectedIndex == 7) // All
            {
                validaTextBoxs = textBoxesIIS.Concat(textBoxesWS).Concat(textBoxesDB).ToArray();

            }

            foreach (TextBox textBox in validaTextBoxs)
            {
                if (textBox.Text == string.Empty)
                {
                    lblMandatoryCheckMsg.Text = $"Pls Enter the required field {textBox.Name.Replace("txt", " ")} ";
                    lnkConfigAppAdd.Enabled = false;
                    lnkConfigAppClear.Enabled = false;
                    return;
                }

            }

            lblMandatoryCheckMsg.Text = "";
            lnkConfigAppAdd.Enabled = true;
            lnkConfigAppClear.Enabled = true;

        }

        private void txtBoxControl_Leave(object sender, EventArgs e)
        {
            var credentialType = cmbCredentialsType.SelectedIndex;
            if (credentialType == 1)
            {
                if (string.IsNullOrEmpty(txtConfigDBserverOrIp.Text))
                {
                    return;
                }
            }

            if (!ControlValidation.IsValidIP(txtConfigDBserverOrIp.Text))
            {
                lblMessage.Visible = true;
                lblMessage.Text = "Invalid IP Address";
                SetReadonlyControls(grpConfigDBCredentials.Controls, false);
                txtConfigDBserverOrIp.Enabled = true;
                return;
            }
            SetReadonlyControls(grpConfigDBCredentials.Controls, true);
            lblMessage.Visible = false;
            lblMessage.Text = "";
            RequiredFieldValidation();
        }

        private void btnStep2Cancel_Click(object sender, EventArgs e)
        {
            rdbComplete.Checked = false;
            rdbPatch.Checked = false;

            SetReadonlyControls(grpInstallation.Controls, true);
            lstChooseComponent.Items.Clear();
            cmbApplicationType.SelectedIndex = cmbApplicationType.Items.IndexOf("Select");
            cmbCredentialsType.SelectedIndex = cmbApplicationType.Items.IndexOf("Select");
            cmbApplicationType.Enabled = false;
            ClearControls(grpConfigDBCredentials.Controls);
            ClearControls(grpConfigIISCredentials.Controls);
            ClearControls(grpConfigWSCredentials.Controls);

            SetReadonlyControls(grpConfigDBCredentials.Controls, false);
            SetReadonlyControls(grpConfigIISCredentials.Controls, false);
            SetReadonlyControls(grpConfigWSCredentials.Controls, false);
            btnStep2Next.Enabled = false;
            saveConfigurations.Clear();
        }

        private void grpInstallation_Enter(object sender, EventArgs e)
        {

        }

        private void txtConfigDBserverOrIp_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtConfigDBserverOrIp_KeyPress(object sender, KeyPressEventArgs e)
        {
            //e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '.';
        }

        private void txtAppsIISDomainName_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '.';
        }

        private void txtAppsWSDomainName_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '.';
        }

        private void lstDisplayConfiguration_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lnkCreateProductKey_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var emailId = Microsoft.VisualBasic.Interaction.InputBox("Email ID", "Enter EmailId");
            if (string.IsNullOrEmpty(emailId))
            {
                lblMessage.Visible = true;
                lblMessage.Text = "Please enter valid email Id";
                return;
            }
            else if (!IsValidEmailId(emailId))
            {
                lblMessage.Visible = true;
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Please enter valid email Id";
                return;
            }
            lblMessage.Visible = false;
            lblMessage.Text = "";
            var projectName = Utility.GetAppSettingByKey("ProjectName");
            if (string.IsNullOrEmpty(projectName))
            {
                lblMessage.Visible = true;
                lblMessage.ForeColor = Color.Red;
                lblMessage.Text = "Project Name doesn't exists !";
                return;
            }

            var appSettings = GetAppSettings();
            var productKey = new YaPProcessor(appSettings).CreateProductKey(projectName, emailId);
            lblMessage.Visible = false;
            lblMessage.Text = "";
            //txtProductkey.Visible = true;
            //txtProductkey.Text = productKey;
            txtProductKeyDisplay.Visible = true;
            txtProductKeyDisplay.Text = productKey;

        }

        private bool IsValidEmailId(string emailId)
        {
            try
            {
                var emailAddress = new System.Net.Mail.MailAddress(emailId);
                return emailAddress.Address == emailId;
            }
            catch
            {
                return false;
            }
        }

        private async void btnOk_Click(object sender, EventArgs e)
        {
            txtProductkey.Visible = true;
            txtProductkey.ReadOnly = false;
            btnOk.Visible = true;
            var productKey = txtProductkey.Text;
            if (string.IsNullOrEmpty(productKey))
            {
                txtProductkey.Focus();
                lblMessage.Visible = true;
                lblMessage.Text = "Enter ProductKey";
                return;
            }
            var projectName = Utility.GetAppSettingByKey("ProjectName");
            if (string.IsNullOrEmpty(projectName))
            {
                lblMessage.Visible = true;
                lblMessage.Text = "Project Name doesn't exists !";
                return;
            }

            Dictionary<string, string> appSettings;
            List<ApplicationModel> applicationModel;
            if (!IsValidProductKey(projectName, productKey, out appSettings, out applicationModel)) return;

            tabYaP.TabPages[1].Enabled = false;
            tabYaP.TabPages[2].Enabled = false;
            tabYaP.TabPages[3].Enabled = true;

            tabYaP.SelectedTab = tabYaP.TabPages[3];
            pcbMessage.Image = Resources.Packaging;
            lblMessage.Visible = true;
            lblMessage.ForeColor = Color.Blue;
            lblMessage.Text = "Package creation InProgress...";
            var installer = new YaPProcessor(appSettings, applicationModel, null, txtProductkey.Text);

            tabYaP.TabPages[0].Enabled = false;
            tabYaP.TabPages[1].Enabled = false;
            tabYaP.TabPages[2].Enabled = false;
            _isButtonClick = true;
            btnLaunch.Visible = false;
            pcbMessage.Visible = true;
            var returnMsg = await installer.InvokeCreatePackageAsync(productKey);
            tabYaP.TabPages[0].Enabled = true;
            tabYaP.TabPages[1].Enabled = true;
            tabYaP.TabPages[2].Enabled = true;
            if (!string.IsNullOrEmpty(returnMsg.Item1) && returnMsg.Item1.Equals("success", StringComparison.CurrentCultureIgnoreCase))
            {
                pcbMessage.Image = Resources.Completed;
                lblMessage.ForeColor = Color.Green;
                lblMessage.Text = "Package Created successfully!";
                lnkCreatePackage.Visible = false;
                foreach (var totalTimeStamp in returnMsg.Item2)
                {
                    lstMessage.Items.Add($"{totalTimeStamp.Key}=>{totalTimeStamp.Value}");
                }

                lstMessage.Visible = true;
                //tabYaP.SelectedTab = tabYaP.TabPages[0];
            }
            else
            {
                pcbMessage.Image = Resources.Failed;
                lblMessage.Text = "Error while creating package!";
                lnkCreatePackage.Visible = true;
                //tabYaP.SelectedTab = tabYaP.TabPages[0];
                lblMessage.Text = "Package Creation failed";
                lblMessage.Visible = true;
                lblMessage.ForeColor = Color.Red;

            }

            //lblMessage.Visible = false;
            //lblMessage.Text = "";
            txtProductkey.Visible = false;
            txtProductkey.ReadOnly = true;
            btnOk.Visible = false;
        }

        private bool IsValidProductKey(string projectName, string productKey, out Dictionary<string, string> appSettings,
            out List<ApplicationModel> applicationModel)
        {
            string productKeyInfo = Resources.ResourceManager.GetString(projectName);
            var emailId = productKeyInfo.Split(':')[1];
            var secretPhrase = emailId;
            //Load AppSetting values
            appSettings = GetAppSettings();
            applicationModel = Utility.GetApplicationModel();
            if (appSettings == null || applicationModel == null) return false;
            if (!new YaPProcessor(appSettings).IsValidProductKey(productKey, secretPhrase))
            {
                txtProductkey.Focus();
                lblMessage.Visible = true;
                lblMessage.Text = "Invalid Product Key!";
                return false;
            }

            return true;
        }

        private void btnOKInstall_Click(object sender, EventArgs e)
        {
            var productKey = txtProductkey.Text;
            if (string.IsNullOrEmpty(productKey))
            {
                txtProductkey.Focus();
                lblMessage.Visible = true;
                lblMessage.Text = "Enter ProductKey";
                return;
            }
            else
            {
                lblMessage.Visible = false;
                lblMessage.Text = "";
            }
            var installationPath = txtInstallPath.Text;
            if (string.IsNullOrEmpty(installationPath))
            {
                txtInstallPath.Focus();
                lblMessage.Visible = true;
                lblMessage.Text = "Enter InstallationPath";
                return;
            }
            else
            {
                lblMessage.Visible = false;
                lblMessage.Text = "";
            }
            var projectName = Utility.GetAppSettingByKey("ProjectName");
            if (string.IsNullOrEmpty(projectName))
            {
                lblMessage.Visible = true;
                lblMessage.Text = "Project Name doesn't exists !";
                return;
            }

            Dictionary<string, string> appSettings;
            List<ApplicationModel> applicationModel;
            if (!IsValidProductKey(projectName, productKey, out appSettings, out applicationModel)) return;

            txtInstallPath.Visible = false;
            btnOKInstall.Visible = false;
            txtProductkey.Visible = false;
            _isButtonClick = true;
            tabYaP.SelectedTab = tabYaP.TabPages[1];

        }

        private void btnStep2Previous_Click(object sender, EventArgs e)
        {
            _isButtonClick = false;
            lnkInstall.Visible = true;
            btnOKInstall.Visible = false;
            lnkCancelInstall.Visible = false;
            lnkCreatePackage.Visible = true;
            lnkCreateProductKey.Visible = true;
            lnkUnInstall.Visible = true;
            txtProductkey.Visible = false;
            txtInstallPath.Visible = false;
            pnlProductKey.Visible = false;
            pnlInstalationPath.Visible = false;
            tabYaP.SelectedTab = tabYaP.TabPages[0];

        }

        private void btnStep4Cancel_Click(object sender, EventArgs e)
        {

            var dialogResult = MessageBox.Show("Want to Cancel Package creation ?", "Cancel Process", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Application.Exit();
            }
            else if (dialogResult == DialogResult.No)
            {
                return;
            }

        }

        private void lnkCancelInstall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lnkCreatePackage.Visible = true;
            lnkPackageCancel.Visible = true;
            pnlProductKey.Visible = false;
            pnlInstalationPath.Visible = false;
            lnkInstall.Visible = true;
            lnkUnInstall.Visible = true;
            lnkCreateProductKey.Visible = true;

            txtProductkey.Text = "";
            txtInstallPath.Text = "";
            lnkCancelInstall.Visible = false;
        }

        private void lnkPackageCancel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            pnlProductKey.Visible = false;
            txtProductkey.Text = "";
            lnkCreatePackage.Visible = true;
            lnkInstall.Visible = true;
            lnkUnInstall.Visible = true;
            lnkCreateProductKey.Visible = true;
        }


    }
    public class PackageService
    {
        public void OnBuildCompleted(object sender, SolutionBuildArgs args)
        {

            var solutionName = args.NameOfSolutionOrProject;
            var status = args.Status;
        }
    }
}
