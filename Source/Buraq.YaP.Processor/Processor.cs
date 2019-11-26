using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Buraq.YaP.Helper;
using Ionic.Zip;
using Microsoft.Web.Administration;
using Ionic.Zlib;
using Buraq.YaP.Model;
using CsvHelper;
using SimpleImpersonation;

namespace Buraq.YaP.Processor
{

    public class YaPProcessor
    {
        //private string _connection = ConfigurationManager.ConnectionStrings["idm"].ConnectionString;
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(YaPProcessor));
        string _connection = "";
        private List<string> _applicatinList = new List<string>() { };
        private string _tempPath = Path.GetTempPath();
        private string _zipPath = "Package";
        private string _unZipPath = "UnPack";
        public string BinaryComponentPath = "HexaComponent";
        private string _msBuildExePath = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe";
        private Dictionary<string, string> AppSettings { get; } = new Dictionary<string, string>();
        private List<ApplicationModel> ApplicationModels { get; } = new List<ApplicationModel>();
        private string _passwordZip = "";
        private readonly string _registryPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
        public event EventHandler<SolutionBuildArgs> BuildCompleted;
        private static readonly List<DeploymentRegistry> RegistryInformation = new List<DeploymentRegistry>();

        protected virtual void OnBuildCompleted(Dictionary<string, string> buildStatus)
        {

            var args = new SolutionBuildArgs
            {
                NameOfSolutionOrProject = buildStatus.Keys.ToString(),
                Status = buildStatus.Values.ToString()
            };
            BuildCompleted?.Invoke(this, args);
        }
        private Dictionary<string, ConfigurationModel> Configuration { get; } =
            new Dictionary<string, ConfigurationModel>();

        public YaPProcessor(Dictionary<string, string> appSettings, List<ApplicationModel> applicationModels,
            Dictionary<string, ConfigurationModel> configuration, string passwordZip)
        {
            AppSettings = appSettings;
            ApplicationModels = applicationModels;
            Configuration = configuration;
            _connection = "";
            _msBuildExePath = "";
            _passwordZip = passwordZip;
            _tempPath = Path.Combine(Path.GetTempPath(), AppSettings["ProjectName"]);
            YaPConfigureLog4Net.ConfigureLog4Net();
        }

        public YaPProcessor(Dictionary<string, string> appSettings)
        {
            YaPConfigureLog4Net.ConfigureLog4Net();
            AppSettings = appSettings;
            _tempPath = Path.Combine(Path.GetTempPath(), AppSettings["ProjectName"]);

        }

        public YaPProcessor()
        {

        }

        public void StoreDllToDatabase(string componentPath, string applicationName)
        {

            DirectoryInfo d = new DirectoryInfo(componentPath);
            FileInfo[] Files = d.GetFiles();

            foreach (var filePath in Files)
            {
                if (filePath.Extension.ToLower() == ".pdb") continue;
                byte[] file;
                var versionNo = "";
                var createDate = new DateTime();
                var modifiedDate = new DateTime();
                using (var stream = new FileStream(filePath.FullName, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        file = reader.ReadBytes((int)stream.Length);
                        versionNo = FileVersionInfo.GetVersionInfo(filePath.FullName).FileVersion;
                        if (string.IsNullOrEmpty(versionNo))
                            versionNo = "0";
                        createDate = File.GetCreationTime(filePath.FullName);
                        modifiedDate = File.GetLastWriteTime(filePath.FullName);

                    }
                }

                using (var varConnection = new SqlConnection(_connection))
                {
                    var checkComponentExit =
                        $"SELECT COUNT(1) FROM {applicationName} where componentName=\'{filePath.Name}\'";
                    using (var sqlWrite =
                        new SqlCommand(
                            $"INSERT INTO {applicationName} (ComponentName,Component,VersionNo,CreateDate,ModifiedDate) Values(@FileName,@File,@VersionNo,@CreateDate,@ModifiedDate)",
                            varConnection))
                    {
                        var sqlRead = new SqlCommand(checkComponentExit, varConnection);
                        varConnection.Open();
                        if (Convert.ToInt16(sqlRead.ExecuteScalar()) > 0) continue;
                        sqlWrite.Parameters.Add("@FileName", SqlDbType.VarChar, filePath.Name.Length).Value =
                            filePath.Name;
                        sqlWrite.Parameters.Add("@File", SqlDbType.VarBinary, file.Length).Value = file;
                        sqlWrite.Parameters.Add("@VersionNo", SqlDbType.VarChar, 50).Value = versionNo;
                        sqlWrite.Parameters.Add("@CreateDate", SqlDbType.DateTime, 50).Value = createDate;
                        sqlWrite.Parameters.Add("@ModifiedDate", SqlDbType.DateTime, 50).Value = modifiedDate;

                        sqlWrite.ExecuteNonQuery();
                        varConnection.Close();
                    }
                }
            }
        }

        public void ConvertAssemblyToByteArray(string sourceComponentPath, string applicationName)
        {
            DirectoryInfo d = new DirectoryInfo(sourceComponentPath);
            List<string> files = new List<string>();

            foreach (string file in Directory.EnumerateFiles(sourceComponentPath, "*.*", SearchOption.AllDirectories))
            {
                files.Add(file);
            }

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var destinationComponentPath = _tempPath + @"AssemblyToByteFile\";
                if (filePath != null && Path.GetExtension(filePath).Equals(".pdb", StringComparison.CurrentCultureIgnoreCase)) continue;
                byte[] file = File.ReadAllBytes(filePath);
                GenerateComponentFile(destinationComponentPath, $"{applicationName}hashidm{fileName}", file);
            }
        }

        public void DeployFromDatabase(string deployPath, string applicationName)
        {
            using (var varConnection = new SqlConnection(_connection))
            {
                using (var sqlQuery = new SqlCommand($@"SELECT ComponentName,Component FROM [dbo].[{applicationName}]",
                    varConnection))
                {
                    varConnection.Open();
                    using (var sqlQueryResult = sqlQuery.ExecuteReader())
                        if (sqlQueryResult != null)
                        {
                            while (sqlQueryResult.Read())
                            {
                                var fileName = sqlQueryResult["ComponentName"].ToString();
                                var blob = new Byte[(sqlQueryResult.GetBytes(1, 0, null, 0, int.MaxValue))];
                                sqlQueryResult.GetBytes(1, 0, blob, 0, blob.Length);
                                var exists = Directory.Exists(deployPath);
                                if (!exists)
                                    Directory.CreateDirectory(deployPath);
                                using (var fs = new FileStream(Path.Combine(deployPath, fileName), FileMode.Create,
                                    FileAccess.Write))
                                    fs.Write(blob, 0, blob.Length);
                            }
                        }
                }
            }

            //CleanUpDeploymentProcess();
        }

        public void DeployFromFile(string deployPath, string applicationName)
        {
            deployPath = @"D:\temp\";
            applicationName = "ProvisioningService";
            _unZipPath = _tempPath + _unZipPath;

            DirectoryInfo directory = new DirectoryInfo(_unZipPath);
            FileInfo[] Files = directory.GetFiles();

            foreach (var filePath in Files)
            {
                var fileContent = File.ReadAllText(filePath.FullName);
                var appName = filePath.Name.Split(new[] { "hashidm" }, StringSplitOptions.None)[0];
                var fileName = filePath.Name.Split(new[] { "hashidm" }, StringSplitOptions.None)[1]
                    .Replace("idmhash", ".");
                if (appName != applicationName) continue;
                var exists = Directory.Exists(deployPath);
                if (!exists)
                    Directory.CreateDirectory(deployPath);
                var componentByte = StringToByteArray(fileContent);
                File.WriteAllBytes(Path.Combine(deployPath, fileName), componentByte);
            }

        }

        private bool CleanUpDeploymentProcess()
        {
            using (var varConnection = new SqlConnection(_connection))
            {
                using (var sqlDrop = new SqlCommand(@"DROP DATABASE INSTALLER", varConnection))
                {
                    varConnection.Open();
                    return (sqlDrop.ExecuteNonQuery() != -1);
                }
            }
        }

        public void GenerateSQLScript(string sqlFilePath)
        {

            sqlFilePath = @"D:\temp\";
            var applicationName = "ProvisioningService";
            var componentName = "";
            var component = "";
            var versionNo = "";
            var createdDate = "";
            var createBy = "";
            var modifiedDate = "";

            using (var varConnection = new SqlConnection(_connection))
            {
                using (var sqlQuery =
                    new SqlCommand(
                        $@"SELECT ComponentName,Component,VersionNo,CreateDate,CreateBy,ModifiedDate FROM [dbo].[{applicationName}]",
                        varConnection))
                {
                    varConnection.Open();
                    using (var sqlQueryResult = sqlQuery.ExecuteReader())
                        if (sqlQueryResult != null)
                        {
                            while (sqlQueryResult.Read())
                            {
                                componentName = sqlQueryResult["ComponentName"].ToString();
                                var componentHexString = BitConverter.ToString(sqlQueryResult["Component"] as byte[])
                                    .Replace("-", "");
                                versionNo = sqlQueryResult["VersionNo"].ToString();
                                createdDate = sqlQueryResult["CreateDate"].ToString();
                                createBy = sqlQueryResult["CreateBy"].ToString();
                                modifiedDate = sqlQueryResult["ModifiedDate"].ToString();

                                var filePath = $"{sqlFilePath}{applicationName}_{componentName}.sql";

                                var insertScript =
                                    "Insert into ProvisioningService (ComponentName,Component,VersionNo,CreateDate,CreateBy,ModifiedDate)" +
                                    $"Values ('{componentName}',0x{componentHexString},'{versionNo}','{createdDate}','{createBy}','{modifiedDate}')";

                                var exists = Directory.Exists(sqlFilePath);
                                if (!exists)
                                    Directory.CreateDirectory(sqlFilePath);
                                File.WriteAllText(filePath, insertScript);
                            }
                        }
                }
            }

        }

        private void GenerateComponentFile(string writePath, string componentName, byte[] component)
        {

            var filePath = Path.Combine(writePath, componentName);
            var extension = Path.GetExtension(filePath);
            filePath = filePath.Replace(extension, $"idmhash{extension.Replace(".", "")}");
            var exists = Directory.Exists(writePath);
            if (!exists)
                Directory.CreateDirectory(writePath);

            var componentHexString = BitConverter.ToString(component).Replace("-", "");
            File.WriteAllText($"{filePath}.txt", $"{componentHexString}");

        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        private bool ExecuteSqlScripts(string sqlQueryFilePath, string connectionString)
        {
            string script = File.ReadAllText($@"{sqlQueryFilePath}");
            // split script on GO command
            IEnumerable<string> commandStrings = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    foreach (var commandString in commandStrings)
                    {
                        if (commandString.Trim() != "")
                        {
                            using (var command = new SqlCommand(commandString, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    connection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private bool CreateOrRemoveDatabase(string databaseName, string connectionString, ref bool isDbAlreadyCreated, bool create = true)
        {
            var dbScript = "";

            if (create)
            {
                dbScript = $"IF NOT EXISTS(SELECT * FROM SYS.DATABASES WHERE NAME = \'{databaseName}\') \n\r CREATE DATABASE {databaseName}";
            }
            else
            {
                dbScript = $"IF  EXISTS(SELECT * FROM SYS.DATABASES WHERE NAME = \'{databaseName}\') \n\r DROP DATABASE {databaseName}";
            }

            var sqlDbExists = $"SELECT database_id FROM sys.databases WHERE Name = '{databaseName}'";

            var commandString = dbScript;
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    //Check database exists
                    using (var command = new SqlCommand(sqlDbExists, connection))
                    {
                        object resultObj = command.ExecuteScalar();
                        int databaseId = 0;
                        if (resultObj != null)
                        {
                            int.TryParse(resultObj.ToString(), out databaseId);
                            connection.Close();
                        }
                        isDbAlreadyCreated = databaseId > 0;
                    }

                    if (!isDbAlreadyCreated)
                    {
                        //Create Database
                        if (!string.IsNullOrEmpty(commandString))
                        {
                            using (var command = new SqlCommand(commandString, connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        connection.Close();
                        return true;
                    }
                    return true;

                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public void Compress(string originalFilePath, string zipFilePath)
        {
            originalFilePath = @"D:\temp2\Original\";
            zipFilePath = @"D:\temp2\Zip\";
            DirectoryInfo d = new DirectoryInfo(originalFilePath);
            FileInfo[] Files = d.GetFiles();

            foreach (var fileToBeCompressed in Files)
            {
                var extension = Path.GetExtension(fileToBeCompressed.FullName);
                string zipFilename = $"{Path.Combine(zipFilePath, fileToBeCompressed.Name).Replace(extension, ".zip")}";

                using (FileStream target = new FileStream(zipFilename, FileMode.Create, FileAccess.Write))
                using (GZipStream alg = new GZipStream(target, CompressionMode.Compress))
                {
                    byte[] data = File.ReadAllBytes(fileToBeCompressed.FullName);
                    alg.Write(data, 0, data.Length);
                    alg.Flush();
                }
            }

        }

        public void DeCompress(string fileToDeCompress)
        {
            var extension = Path.GetExtension(fileToDeCompress);
            string originalFileName = fileToDeCompress.Replace(extension, ".txt");
            string compressedFile = fileToDeCompress;

            using (FileStream zipFile =
                new FileStream(compressedFile,
                    FileMode.Open, FileAccess.Read))
            using (FileStream originalFile =
                new FileStream(originalFileName,
                    FileMode.Create, FileAccess.Write))
            using (GZipStream alg =
                new GZipStream(zipFile, CompressionMode.Decompress))
            {
                while (true)
                {
                    // Reading 100bytes by 100bytes
                    byte[] buffer = new byte[100];
                    // The Read() method returns the number of bytes read
                    int bytesRead = alg.Read(buffer, 0, buffer.Length);

                    originalFile.Write(buffer, 0, bytesRead);

                    if (bytesRead != buffer.Length)
                        break;
                }
            }
        }

        private bool UnpackResourceFiles(out string unPackPath)
        {
            try
            {
                unPackPath = "";
                string destinationPath = "";
                //var processingPath = Path.GetTempPath();
                var processingPath = Path.Combine(_tempPath, _unZipPath);
                var executingAssembly = Assembly.GetExecutingAssembly();
                var folderName = $"{executingAssembly.GetName().Name}";
                var files = executingAssembly.GetManifestResourceNames().Where(r => r.StartsWith(folderName) && r.EndsWith(".zip")).ToArray();

                var exists = Directory.Exists(processingPath);
                if (!exists)
                    Directory.CreateDirectory(processingPath);
                foreach (var file in files)
                {
                    using (var resourceStream = executingAssembly.GetManifestResourceStream(file))
                    {
                        var destinationFileName = file.Replace(folderName, "").Split('.')[2];
                        if (resourceStream == null)
                        {
                            throw new Exception("Cannot find embedded resource '" + file + "'");
                        }

                        var buffer = new byte[resourceStream.Length];
                        resourceStream.Read(buffer, 0, buffer.Length);
                        destinationPath = Path.Combine(processingPath, destinationFileName) + ".zip";
                        using (var sw = new BinaryWriter(File.Open(Path.Combine(destinationPath), FileMode.Create)))
                        {
                            sw.Write(buffer);
                        }
                    }
                }
                log.Info("Unpacked Deployment package successfully!");
                unPackPath = destinationPath;
                return true;

            }
            catch (Exception ex)
            {
                log.Error("Failed while unpacking deployment package", ex);
                unPackPath = "";
                return false;
            }
        }

        private bool UnZip(string zipFilePath, string destinationPath, string productKey, List<string> includeAppDirectories)
        {
            //zipFilePath = Path.Combine(zipFilePath, "DeploymentPackage.zip");
            if (!File.Exists(zipFilePath))
                log.Info("Package not Found");

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);
            try
            {
                foreach (var includeAppDirectory in includeAppDirectories)
                {
                    using (ZipFile zip = ZipFile.Read(zipFilePath))
                    {
                        foreach (ZipEntry e in zip.Where(x => x.FileName.StartsWith(includeAppDirectory)))
                        {
                            zip.Password = productKey;
                            zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                            e.Extract(destinationPath, ExtractExistingFileAction.OverwriteSilently);
                        }
                    }
                }
                return true;

            }
            catch (Exception ex)
            {
                log.Error("During UnZipping process", ex);
                return false;
            }
        }

        private string CreateZip(string productKey)
        {
            var sourcePath = Path.Combine(_tempPath, @"SourcePackage");
            return Zip(sourcePath, productKey);
        }

        private Tuple<string, Dictionary<string, string>> CreatePackage(string productKey, out Dictionary<string, string> totalTimeElapsed)
        {
            totalTimeElapsed = new Dictionary<string, string>();
            try
            {
                //Convert Web Assembly to Byte array
                var msBuildExePath = AppSettings["MsBuildPath"];
                var mainSolutionBatchFileName = AppSettings["MainSolutionBatchFileName"];
                var yapSetupBatchFileName = AppSettings["YaPSetupBatchFileName"];
                var solutionBuildPath = AppSettings["SolutionPath"]?.Replace(@"\", @"\\");
                Dictionary<string, string> timeElapsedSolution;
                Dictionary<string, string> timeElapsedPublishing;


                if (!BuildSolution(msBuildExePath, solutionBuildPath, mainSolutionBatchFileName, out timeElapsedSolution))
                {
                    return new Tuple<string, Dictionary<string, string>>("Failed", totalTimeElapsed);
                }

                //OnBuildCompleted(timeElapsedSolution); //notify to client on successful built;

                if (!PublishWebSite(msBuildExePath, solutionBuildPath, out timeElapsedPublishing))
                {
                    return new Tuple<string, Dictionary<string, string>>("Failed", totalTimeElapsed);
                }


                totalTimeElapsed = timeElapsedSolution.Union(timeElapsedPublishing).ToDictionary(k => k.Key, v => v.Value);
                //Copy components from source to destination
                foreach (var applicationModel in ApplicationModels)
                {
                    if (applicationModel.Type.Equals("database", StringComparison.CurrentCultureIgnoreCase)) continue;
                    //if (applicationModel.Type.Equals("windowservice", StringComparison.CurrentCultureIgnoreCase))
                    //{
                    //    applicationModel.Name = Path.Combine("Services", applicationModel.Name);
                    //}
                    ExtractComponents(applicationModel.SourcePath, applicationModel.Name, applicationModel);

                }

                var packagePath = CreateZip(productKey);
                if (!string.IsNullOrEmpty(packagePath))
                {
                    //var executingAssemblyPath = AppDomain.CurrentDomain.BaseDirectory;
                    var deploymentPackagepath = AppSettings["DeploymentPackage"];
                    var destinationPath = Path.Combine(deploymentPackagepath, Path.GetFileName(packagePath));
                    File.Copy(packagePath, destinationPath, true);
                    if (!CleanUpPackaginProcessFolders())
                        return new Tuple<string, Dictionary<string, string>>("Failed", totalTimeElapsed);
                }

                //Rebuild YaP Project to include deployemntPackage.zip 
                var yapProjectPath = AppSettings["YaPSetUpProjectPath"]?.Replace(@"\", @"\\");
                solutionBuildPath = yapProjectPath;
                if (!BuildSolution(msBuildExePath, solutionBuildPath, yapSetupBatchFileName, out timeElapsedSolution))
                {
                    return new Tuple<string, Dictionary<string, string>>("Failed", totalTimeElapsed);
                }
            }
            catch (Exception ex)
            {
                log.Error("Error while creating pacakge", ex);
            }
            return new Tuple<string, Dictionary<string, string>>("success", totalTimeElapsed);
        }

        private bool CleanUpPackaginProcessFolders()
        {
            try
            {
                string[] subdirectoryEntries = Directory.GetDirectories(_tempPath);

                foreach (var path in subdirectoryEntries)
                {
                    var directoryName = new DirectoryInfo(@path).Name;
                    var excludeFromCleanUp = AppSettings["ExcludeFromCleanUp"].Split(',').ToList();
                    if (!excludeFromCleanUp.Contains(directoryName, StringComparer.OrdinalIgnoreCase))
                    {
                        Directory.Delete(path, true);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }

        private bool CleanUpDirectories(string destinationPath, List<string> cleanUpPaths)
        {
            try
            {
                foreach (var cleanUpPath in cleanUpPaths)
                {
                    string[] subdirectoryEntries = Directory.GetDirectories(cleanUpPath);
                    foreach (var path in subdirectoryEntries)
                    {
                        Directory.Delete(path, true);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }


        private bool UnZipAssembly(string destinationPath, string productKey, List<string> installableAppNames)
        {
            var unPackPath = "";
            if (UnpackResourceFiles(out unPackPath))
                return UnZip(unPackPath, destinationPath, productKey, installableAppNames);
            return false;

        }

        private string Zip(string sourcePath, string productKey)
        {
            var destinationDirectory = Path.Combine(_tempPath, _zipPath);
            var exists = Directory.Exists(destinationDirectory);
            if (!exists)
                Directory.CreateDirectory(Path.Combine(destinationDirectory));

            var destinationPath = Path.Combine(destinationDirectory, "DeploymentPackage.zip");
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    zip.Password = productKey;
                    zip.AddDirectory(sourcePath);
                    zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    zip.Save(destinationPath);
                }
                return destinationPath;
            }
            catch (Exception ex)
            {
                log.Error("During package zipping process", ex);
                return "";
            }
        }

        public Tuple<string, Dictionary<string, string>> InvokeCreatePackage(string productKey, out Dictionary<string, string> totalTimeStamp)
        {
            return CreatePackage(productKey, out totalTimeStamp);

        }

        public async Task<Tuple<string, Dictionary<string, string>>> InvokeCreatePackageAsync(string productKey)
        {
            Dictionary<string, string> totalTimeStamp;
            return await Task.Run(() => InvokeCreatePackage(productKey, out totalTimeStamp));
        }

        private void GetAllDeployableComponentPath()
        {

        }

        public bool UnInstallApplication()
        {

            DeleteWebApplication();
            UnInstallWindowService("", "");
            DeleteInstallationFolder();
            DeleteDatabase();
            return true;

        }

        private void DeleteDatabase()
        {
            throw new NotImplementedException();
        }

        private void DeleteInstallationFolder()
        {
            throw new NotImplementedException();
        }

        private void DeleteWebApplication()
        {
            throw new NotImplementedException();
        }

        private List<string> InstallApplication(string installPath, out bool isInstallationSuccessful, List<string> installableAppNames)
        {
            var listOfUniqDatabaseName = new List<string>();
            List<string> returnMessage;
            returnMessage = new List<string>();
            isInstallationSuccessful = true;
            List<string> registery = new List<string>();
            //registeryInformation = new List<List<string>>();

            foreach (var application in ApplicationModels)
            {
                if (application.Ignore.Equals("1")) continue;
                foreach (var database in application.Database)
                {
                    if (!listOfUniqDatabaseName.Contains(database))
                    {
                        listOfUniqDatabaseName.Add(database);
                    }
                }

            }

            //Execute query files of Database
            foreach (var database in listOfUniqDatabaseName)
            {
                var dbConfiguration = Configuration.Where(x => !string.IsNullOrEmpty(x.Value.DBServerOrIp)).Select(x => x.Value).FirstOrDefault();
                if (dbConfiguration == null) continue;
                var sqlDataFileName = Path.Combine($@"{_tempPath}\Database", $"{database}.sql");
                string connectionString = GenerateConnectionString(dbConfiguration, true, database, false);
                bool isDbAlreadyCreated = false;
                if (CreateDatabase(database, connectionString, ref isDbAlreadyCreated))
                {
                    if (isDbAlreadyCreated)
                    {
                        CreateDatabaeRegistry(installPath, database, dbConfiguration);
                        continue;
                    }
                    connectionString = GenerateConnectionString(dbConfiguration, false, database, false);
                    if (!ExecuteSqlScripts(sqlDataFileName, connectionString)) continue;
                    returnMessage.Add($"Success: Created {database}");
                    CreateDatabaeRegistry(installPath, database, dbConfiguration);
                }
                else
                {
                    returnMessage.Add($"Failed:Database Created for : {database}");
                    isInstallationSuccessful = false;
                    return returnMessage;
                }
            }

            //Create installation path
            //if (CreateInstallationFolder(installPath))
            //{
            foreach (var app in ApplicationModels.OrderBy(x => x.Name))
            {
                if (app.Ignore.Equals("1")) continue;
                var applicationType = app.Type;
                if (applicationType.Equals("web", StringComparison.OrdinalIgnoreCase))
                {
                    if (!installableAppNames.Contains(app.Name, StringComparer.OrdinalIgnoreCase)) continue;
                    var bindingProtocal = AppSettings["BindingProtocal"];
                    var webSiteName = app.Name;
                    var webSitePortNo = GetOpenPort();
                    var physicalFolder = Path.Combine(installPath, app.Name);
                    var appPoolUserName = Configuration["Web"].IISUserName;
                    var appPoolPassword = Configuration["Web"].IISPassword;
                    ;
                    var enable32Bit = app.Enable32BitMode;
                    var domainNameOrIP = "";

                    if (CreateWebSite(webSiteName, bindingProtocal, webSitePortNo, physicalFolder, appPoolUserName,
                        appPoolPassword, enable32Bit))
                    {
                        returnMessage.Add($"Success: Created Web Application {app.Name}");
                        CreateWebRegistery(installPath, app, domainNameOrIP, appPoolUserName, appPoolPassword, bindingProtocal, webSitePortNo,
                            webSiteName);
                    }
                    else
                    {
                        returnMessage.Add($"Failed: Created WebSite {app.Name}");
                        isInstallationSuccessful = false;
                    }

                }
                else if (app.Type.Equals("windowservice", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!installableAppNames.Contains(app.Name, StringComparer.OrdinalIgnoreCase)) continue;
                    var serviceName = "";
                    var domainNameOrIP = Configuration["WindowService"].WSDomainName;
                    var userName = Configuration["WindowService"].WSUserName;
                    var password = Configuration["WindowService"].WSPassword;
                    var InstallServiceName = app.ServiceParameters.First(p => p.StartsWith("ServiceName"))
                        ?.Split(':')[1];
                    var InstallDisplayName = app.ServiceParameters.First(p => p.StartsWith("DisplayName"))
                        ?.Split(':')[1];
                    var startOption = app.ServiceParameters.First(p => p.StartsWith("StartOption"))
                        ?.Split(':')[1];

                    installPath = Path.Combine(installPath, app.Name);
                    if (InstallWindowService(InstallServiceName, InstallDisplayName, startOption, installPath))
                    {
                        SetWindowsServiceCredentials(InstallServiceName, userName, password);
                        returnMessage.Add($"Success: Created Window Service {app.Name}");
                        CreateWindowServiceRegistery(installPath, app, domainNameOrIP, userName, password);

                    }
                    else
                    {
                        returnMessage.Add($"Failed: Created Window Service {app.Name}");
                        isInstallationSuccessful = false;
                    }
                }
                else if (app.Type.Equals("filecopy", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!installableAppNames.Contains(app.Name, StringComparer.OrdinalIgnoreCase)) continue;
                    var sourcePath = app.SourcePath;
                    var destinationPath = $@"{installPath}\{app.CopyToFolder}";
                    if (CopyFiles(sourcePath, destinationPath))
                    {
                        returnMessage.Add($"Success: Copied Files Or Folder {app.Name}");
                        CreateFileCopyRegistery(installPath, app);
                    }
                    else
                    {
                        returnMessage.Add($"Failed: Copied Files Or Folder {app.Name}");
                        isInstallationSuccessful = false;
                    }
                }
            }
            //}
            //else
            //{
            //    returnMessage.Add("Failed: Creation of Installation Folder Path");
            //    return returnMessage;
            //}

            return returnMessage;

        }

        private string GenerateConnectionString(ConfigurationModel dbConfigurationModel, bool isCreateDatabase, string databaseName, bool isDomainCredential)
        {
            StringBuilder connectionBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(dbConfigurationModel.DBPortNo))
            {
                connectionBuilder.Append($"Data Source={dbConfigurationModel.DBServerOrIp} , {dbConfigurationModel.DBPortNo}; ");
            }
            else
            {
                connectionBuilder.Append($"Data Source={dbConfigurationModel.DBServerOrIp}; ");
            }

            if (isCreateDatabase)
            {
                connectionBuilder.Append($"Initial Catalog=Master; ");
            }
            else
            {
                connectionBuilder.Append($"Initial Catalog={databaseName}; ");
            }
            connectionBuilder.Append($"Persist Security Info= true; ");
            if (isDomainCredential)
            {
                connectionBuilder.Append($@"User Id={dbConfigurationModel.DBDomainName}\{dbConfigurationModel.DBUserName}; ");
            }
            else
            {
                connectionBuilder.Append($"User Id={dbConfigurationModel.DBUserName}; ");
            }

            connectionBuilder.Append($"Password={dbConfigurationModel.DBPassword}; ");

            return connectionBuilder.ToString();
        }
        private void CreateFileCopyRegistery(string installPath, ApplicationModel application)
        {

            DeploymentRegistry registry = new DeploymentRegistry();
            //applicationType
            registry.ApplicationType = "FileCopy";
            //applicationName
            registry.ApplicationName = application.Name;
            //InstallationPath
            registry.InstallationPath = installPath;
            RegistryInformation.Add(registry);

        }

        private static void CreateWindowServiceRegistery(string installPath, ApplicationModel application, string domainNameOrIP, string userName, string password)
        {

            DeploymentRegistry registry = new DeploymentRegistry();
            //applicationType
            registry.ApplicationType = "WindowService";
            //applicationName
            registry.ApplicationName = application.Name;
            //WSConfiguration
            var wsConfigurationBuilder = new StringBuilder();
            wsConfigurationBuilder.Append($"DomainNameOrIP= {domainNameOrIP};");
            wsConfigurationBuilder.Append($"UserName= {userName};");
            wsConfigurationBuilder.Append($"Password= {password};");
            wsConfigurationBuilder.Append("#");
            registry.WSConfiguration = wsConfigurationBuilder.ToString();
            //InstallationPath
            registry.InstallationPath = $@"{installPath}\{application.Name}";
            RegistryInformation.Add(registry);
        }

        private static void CreateWebRegistery(string installPath, ApplicationModel application, string domainNameOrIP, string appPoolUserName,
            string appPoolPassword, string bindingProtocal, int webSitePortNo, string webSiteName)
        {

            DeploymentRegistry registry = new DeploymentRegistry();
            //applicationType
            registry.ApplicationType = "Web";
            //applicationName
            registry.ApplicationName = application.Name;
            //IISConfiguration
            var iisConfigurationBuilder = new StringBuilder();
            iisConfigurationBuilder.Append($"DomainNameOrIP= {domainNameOrIP};");
            iisConfigurationBuilder.Append($"UserName= {appPoolUserName};");
            iisConfigurationBuilder.Append($"Password= {appPoolPassword};");
            iisConfigurationBuilder.Append($"{bindingProtocal}//{domainNameOrIP}:{webSitePortNo}//{webSiteName};");
            registry.IISConfiguration = iisConfigurationBuilder.ToString();
            //InstallationPath
            registry.InstallationPath = installPath;
            RegistryInformation.Add(registry);

        }

        public static void CreateDatabaeRegistry(string installPath, string database, ConfigurationModel dbConfiguration)
        {

            DeploymentRegistry registry = new DeploymentRegistry();
            //applicationType
            registry.ApplicationType = "Database";
            //applicationName
            registry.ApplicationName = dbConfiguration.InstallableAppName;
            //DBConfiguration
            var dbConfigurationBuilder = new StringBuilder();
            dbConfigurationBuilder.Append($"ServerNameOrIP= {dbConfiguration.DBServerOrIp};");
            dbConfigurationBuilder.Append($"PortNo= {dbConfiguration.DBPortNo};");
            dbConfigurationBuilder.Append($"DomainName= {dbConfiguration.DBDomainName};");
            dbConfigurationBuilder.Append($"Database= {database};");
            dbConfigurationBuilder.Append($"UserName= {dbConfiguration.DBUserName};");
            dbConfigurationBuilder.Append($"Password= {dbConfiguration.DBPassword};");
            registry.DBConfiguration = dbConfigurationBuilder.ToString();
            //InstallationPath
            registry.InstallationPath = installPath;
            RegistryInformation.Add(registry);

        }

        private bool CopyFiles(string sourcePath, string destinationPath)
        {
            return false;
        }

        public void LaunchApplication()
        {
            //Start WebSite (User facing)
            LaunchWebSite("");

        }

        private bool CreateInstallationFolder(string installationPath)
        {
            return false;
        }

        private bool CreateDatabase(string dataBaseName, string connectionString, ref bool isDbAlreadyCreated)
        {
            return CreateOrRemoveDatabase(dataBaseName, connectionString, ref isDbAlreadyCreated);
        }

        private Tuple<List<string>, bool> InvokeInstallation(string installationPath, string productKey, bool isPatchDeployment, List<string> installableAppNames)
        {
            var isInstallationSuccessful = false;
            var installationResults = new List<string>();
            if (!UnZipAssembly(installationPath, productKey, installableAppNames))
            {
                return new Tuple<List<string>, bool>(installationResults, isInstallationSuccessful);

            }

            if (!isPatchDeployment)
            {
                installationResults = InstallApplication(installationPath, out isInstallationSuccessful, installableAppNames);
                if (isInstallationSuccessful)
                {
                    isInstallationSuccessful = CreateDeploymentRegistry();
                }
                return new Tuple<List<string>, bool>(installationResults, isInstallationSuccessful);
            }
            installationResults = InvokePatchDeployment(installationPath, out isInstallationSuccessful);
            return new Tuple<List<string>, bool>(installationResults, isInstallationSuccessful);
        }

        private Tuple<List<string>, bool> InvokeUnInstallation(string productKey)
        {
            //Load Deployment Registry file
            //UnInstall Window Service
            //UnInstall Web Application
            //Remove All Databases
            //Delete Installation path
            //Delete Deployement Registry file.
            return new Tuple<List<string>, bool>(null, false);
        }

        public async Task<Tuple<List<string>, bool>> InvokeUnInstallationAsync(string productKey)
        {
            return await Task.Run(() => InvokeUnInstallation(productKey));
        }

        private List<string> InvokePatchDeployment(string installationPath, out bool isInstallationSuccessful)
        {
            isInstallationSuccessful = true;
            var installationResult = new List<string>();
            var sourcePath = Path.Combine(_tempPath, _unZipPath);
            foreach (var applicationModel in ApplicationModels)
            {
                if (applicationModel.Ignore.Equals("1")) continue;
                sourcePath = Path.Combine(sourcePath, applicationModel.Name);
                var destinationPath = Path.Combine(installationPath, applicationModel.Name);
                isInstallationSuccessful = CopyComponents(sourcePath, destinationPath, applicationModel);
                installationResult.Add(isInstallationSuccessful
                    ? $"success:{applicationModel.Name} is deployed successfully"
                    : $"Failed:{applicationModel.Name} is failed to deploy");
            }

            return installationResult;
        }

        public async Task<Tuple<List<string>, bool>> InvokeInstallationAsync(string installationPath, string productKey, bool isPatchDeployment, List<string> installableAppNames)
        {
            return await Task.Run(() => InvokeInstallation(installationPath, productKey, isPatchDeployment, installableAppNames));
        }

        public bool CreateWebSite(string webSiteName, string bindingProtocal, int portNo, string physicalFolder,string appPoolUserName, string appPoolPassword, bool enable32bitOn64 = false)
        {
            //webSiteName = "TestWeb";
            //bindingProtocal = "http";
            //portNo = 7076;
            //physicalFolder = @"D:\inetpub\Deepidentity\Web";
            //appPoolUserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            //appPoolPassword = "Monday20";

            using (var iisManager = new ServerManager())
            {
                //check if website name already exists in IIS
                if (!IsWebsiteExists(webSiteName))
                {
                    var appPool = $"{webSiteName}Pool";
                    if (!isAppPoolExits(appPool))
                    {
                        if (!CreateAppPool(appPool, enable32bitOn64)) return false;
                    }

                    var site = iisManager.Sites.Add(webSiteName, physicalFolder, portNo);
                    site.ApplicationDefaults.EnabledProtocols = bindingProtocal;
                    iisManager.Sites[webSiteName].ApplicationDefaults.ApplicationPoolName = appPool;
                    foreach (var item in iisManager.Sites[webSiteName].Applications)
                    {
                        item.ApplicationPoolName = appPool;
                    }

                    iisManager.CommitChanges();
                    SetAppPoolIdentity(appPool, appPoolUserName, appPoolPassword);
                    return true;
                }
            }

            return false;

        }

        private bool CreateAppPool(string poolname, bool enable32bitOn64, string runtimeVersion = "v4.0")
        {
            using (ServerManager serverManager = new ServerManager())
            {
                ApplicationPool newPool = serverManager.ApplicationPools.Add(poolname);
                newPool.ManagedRuntimeVersion = runtimeVersion;
                newPool.Enable32BitAppOnWin64 = enable32bitOn64;
                newPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                serverManager.CommitChanges();
                return true;
            }
        }

        private bool isAppPoolExits(string appPoolName)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                return serverManager.ApplicationPools.Any(p => p.Name == appPoolName);
            }

        }

        private void SetAppPoolIdentity(string appPoolName, string appPoolUser, string appPoolPassword)
        {
            //Initialize the metabase path
            var metabasePath = "IIS://localhost/W3SVC/AppPools";
            DirectoryEntry appPools = new DirectoryEntry(metabasePath);
            var appPoolDirectoryEntry = appPools.Children.Find(appPoolName, "IIsApplicationPool");
            /*Change Application Pool Identity*/
            appPoolDirectoryEntry.InvokeSet("AppPoolIdentityType", 3);
            appPoolDirectoryEntry.InvokeSet("WAMUserName", appPoolUser);
            appPoolDirectoryEntry.InvokeSet("WAMUserPass", appPoolPassword);
            /*Commit changes*/
            appPoolDirectoryEntry.CommitChanges();

        }

        private bool IsWebsiteExists(string strWebsitename)
        {
            var serverMgr = new ServerManager();
            var flagset = false;
            var sitecollection = serverMgr.Sites;
            foreach (Site site in sitecollection)
            {
                if (site.Name != strWebsitename) continue;
                flagset = true;
                break;
            }

            return flagset;
        }

        private bool InstallWindowService(string serviceName, string displayName, string startOption, string installationPath)
        {
            try
            {
                var serviceBatchFileName = GenerateServiceBatFile(serviceName, displayName, startOption, installationPath);
                if (string.IsNullOrEmpty(serviceBatchFileName)) return false;
                if (IsServiceInstalled(serviceName)) return true;
                var processService = new Process { StartInfo = { FileName = serviceBatchFileName } };
                processService.Start();
                processService.WaitForExit();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string GenerateServiceBatFile(string serviceName, string displayName, string startOption, string installationPath, bool create = true)
        {
            var servicePath = installationPath;
            var projectName = Utility.GetAppSettingByKey("ProjectName");
            var serviceBatchFileContent = "";
            var serviceBatchFileName = Path.Combine(Path.GetTempPath(), projectName);
            if (create)
            {
                serviceBatchFileContent = $"SC CREATE \"{serviceName}\" binpath= \"{servicePath}\"  displayname= \"{displayName}\" depend= Tcpip start= {startOption} ";
                serviceBatchFileName = Path.Combine(serviceBatchFileName, $"Install_{serviceName}.bat");
            }
            else
            {
                serviceBatchFileContent = $"SC DELETE {serviceName} ";
                serviceBatchFileName = Path.Combine(serviceBatchFileName, $"UnInstall_{serviceName}.bat");
            }

            using (var stream = new StreamWriter(serviceBatchFileName))
            {

                stream.WriteLine(serviceBatchFileContent);
            }

            return serviceBatchFileName;
        }

        public void UnInstallWindowService(string batchFileNamePath, string serviceName)
        {
            if (IsServiceInstalled(serviceName)) return;
            Process.Start(batchFileNamePath);
        }

        public bool IsServiceInstalled(string serviceName)
        {
            return GetService(serviceName) != null;
        }

        private ServiceController GetService(string serviceName)
        {
            var services = ServiceController.GetServices();
            return services.FirstOrDefault(s => s.ServiceName == serviceName);
        }

        private void StartService(string serviceName)
        {
            ServiceController controller = GetService(serviceName);
            if (controller == null)
            {
                return;
            }

            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running);
        }

        public void StopService(string serviceName)
        {
            ServiceController controller = GetService(serviceName);
            if (controller == null)
            {
                return;
            }

            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        public void SetWindowsServiceCredentials(string serviceName, string username, string password)
        {
            if (string.IsNullOrEmpty(serviceName) || string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(password)) return;
            var objPath = string.Format("Win32_Service.Name='{0}'", serviceName);
            using (ManagementObject service = new ManagementObject(new ManagementPath(objPath)))
            {
                object[] wmiParams = new object[10];
                wmiParams[6] = username;
                wmiParams[7] = password;
                service.InvokeMethod("Change", wmiParams);
            }
        }

        public void LaunchWebSite(string url)
        {
            Process.Start(url);
        }

        public bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }

            return isAdmin;
        }

        public Dictionary<string, string> BuildSolution()
        {
            Dictionary<string, string> configurations = new Dictionary<string, string>();
            //Get solution path
            if (!configurations.ContainsKey("SolutionPath")) return null;
            var solutionPath = configurations["SolutionPath"];

            //Get MsBuild path
            if (!configurations.ContainsKey("MsBuildPath")) return null;
            var msBuildPath = configurations["MsBuildPath"];

            _msBuildExePath = msBuildPath;
            var msBuildExePath = _msBuildExePath;
            Dictionary<string, string> timeElapsed = new Dictionary<string, string>();
            //BuildSolution(msBuildExePath, solutionPath, out timeElapsed);
            return timeElapsed;
        }

        private bool BuildSolution(string msBuildExePath, string solutionPath, string solutionBatchFileName, out Dictionary<string, string> timeElapsed)
        {
            timeElapsed = new Dictionary<string, string>();
            try
            {
                //Construct Builder batch file
                var batchFilePathCreate = $@"{_tempPath}\BatchFiles";
                var batchFilePath = Path.Combine(batchFilePathCreate, $"{solutionBatchFileName}.bat");
                var exists = Directory.Exists(batchFilePathCreate);
                if (!exists)
                    Directory.CreateDirectory(batchFilePathCreate);

                var builderBatchFile =
                    $"@echo off\r\n\"{msBuildExePath}\" \"{solutionPath}\" /t:Rebuild /p:Configuration=Release\r\nset BUILD_STATUS=%ERRORLEVEL%\r\nif %BUILD_STATUS%==0 echo buildsuccess \r\nif not %BUILD_STATUS%==0  echo buildfailed";

                using (var streamWriter = new StreamWriter(batchFilePath))
                {
                    streamWriter.WriteLine(builderBatchFile);
                }

                //Build solution using MSBuildTool
                var outPut = RunProcessGrabOutput(batchFilePath);
                if (outPut.Any(p => p.Contains("buildsuccess")))
                {
                    timeElapsed.Add("Solution", outPut.FirstOrDefault(p => p.Contains("Time Elapsed")));
                    log.Info("Solution build successful");
                    return true;
                }

                log.Error($"Solution build Failed :{outPut}");
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        private bool PublishWebSite(string msBuildExePath, string solutionPath, out Dictionary<string, string> timeElapsed)
        {
            timeElapsed = new Dictionary<string, string>();
            try
            {
                var batchFilePathCreate = $@"{_tempPath}\BatchFiles";
                var batchFilePath = batchFilePathCreate;
                var exists = Directory.Exists(batchFilePathCreate);
                if (!exists)
                    Directory.CreateDirectory(batchFilePathCreate);


                foreach (var applicationModel in ApplicationModels)
                {
                    if (!applicationModel.Type.Equals("Web", StringComparison.CurrentCultureIgnoreCase)) continue;
                    //Construct Builder batch file
                    batchFilePath = $@"{batchFilePathCreate}\PublishWebSite_{applicationModel.Name}.bat";

                    var builderBatchFile =
                        $"@echo off \r\n\"{msBuildExePath}\" \"{applicationModel.ProjectName}\" /P:DeployOnBuild=true /P:PublishProfile=\"{applicationModel.PublishProfile}\" /P:WebPublishProfileFile=\"{applicationModel.PublishProfile}\" \r\nset BUILD_STATUS=%ERRORLEVEL%\r\nif %BUILD_STATUS%==0 echo publishsuccess \r\nif not %BUILD_STATUS%==0  echo publishfailed";

                    using (var streamWriter = new StreamWriter(batchFilePath))
                    {
                        streamWriter.WriteLine(builderBatchFile);
                    }

                    //Build solution using MSBuildTool
                    var outPut = RunProcessGrabOutput(batchFilePath);
                    if (!outPut.Any(p => p.Contains("publishsuccess")))
                    {
                        log.Error($"Published {applicationModel.Name}:{outPut}");
                        return false;
                    }
                    if (!timeElapsed.ContainsKey(applicationModel.Name))
                    {
                        log.Info($"Published : {applicationModel.Name}:{outPut.FirstOrDefault(p => p.Contains("Time Elapsed"))}");
                        timeElapsed.Add(applicationModel.Name, outPut.FirstOrDefault(p => p.Contains("Time Elapsed")));
                    }

                }
                return true;
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                return false;
            }
        }

        private Process _process;
        private List<string> _processOutput;

        public List<string> RunProcessGrabOutput(string batchFilePath)
        {
            try
            {
                _process = new Process();
                _processOutput = new List<string>();
                _process.StartInfo.FileName = batchFilePath;
                _process.StartInfo.UseShellExecute = false;
                //process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_tempPath);
                _process.StartInfo.RedirectStandardInput = true;
                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;
                _process.StartInfo.CreateNoWindow = true;
                _process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                _process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

                //if (!string.IsNullOrEmpty(Arguments))
                //    process.StartInfo.Arguments = Arguments;

                _process.EnableRaisingEvents = true;
                _process.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputHandler);
                _process.ErrorDataReceived += new DataReceivedEventHandler(ProcessOutputHandler);
                _process.Start();

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                // You can set the priority only AFTER the you started the process.
                _process.PriorityClass = ProcessPriorityClass.BelowNormal;
                _process.WaitForExit();
            }
            catch
            {
                // This is how we indicate that something went wrong.
                _processOutput = null;
            }

            return _processOutput;
        }

        private void ProcessOutputHandler(object SendingProcess, DataReceivedEventArgs OutLine)
        {
            _processOutput.Add(OutLine.Data + Environment.NewLine);
        }

        private bool CreateDeploymentRegistry()
        {
            try
            {
                FileInfo fi = new FileInfo(_registryPath);
                string drive = Path.GetPathRoot(fi.FullName);
                var registryPath = Path.Combine(drive, AppSettings["ProjectName"]);
                if (!Directory.Exists(registryPath))
                {
                    Directory.CreateDirectory(registryPath);
                }
                var deploymentRegistryFile = Path.Combine(registryPath, "DeploymentRegistry.csv");

                using (var writer = new StreamWriter(deploymentRegistryFile))
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(RegistryInformation);
                }
                RegistryInformation.Clear();

            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        private int GetOpenPort()
        {
            int portStartIndex = Convert.ToInt32(AppSettings["PortStartIndex"]);
            int portEndIndex = Convert.ToInt32(AppSettings["PortEndIndex"]);
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();

            List<int> usedPorts = tcpEndPoints.Select(p => p.Port).ToList<int>();
            int unUsedPort = 0;

            for (int port = portStartIndex; port < portEndIndex; port++)
            {
                if (!usedPorts.Contains(port))
                {
                    unUsedPort = port;
                    break;
                }
            }

            return unUsedPort;
        }

        public string CreateProductKey(string projectName, string emailId)
        {

            var secretPassPharase = emailId;
            var createSKGLKey = new SKGL.Generate();
            createSKGLKey.secretPhase = secretPassPharase;
            var key = createSKGLKey.doKey(999);

            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(assemblyPath, "Productkey.txt");
            using (StreamWriter writter = new StreamWriter(filePath, true))
            {
                writter.WriteLine($"{projectName}:{key}:{secretPassPharase}");
            }

            return key;
        }

        public bool IsValidProductKey(string productKey, string secretPhrase)
        {
            var validateKey = new SKGL.Validate { secretPhase = secretPhrase, Key = productKey };
            return validateKey.IsValid;
        }

        private string ExtractComponents(string sourcePath, string applicationFolder, ApplicationModel applicationModel)
        {
            try
            {
                string destinationPath = Path.Combine(_tempPath, "SourcePackage");
                var exists = Directory.Exists(Path.Combine(destinationPath, applicationFolder));
                if (!exists)
                    Directory.CreateDirectory(Path.Combine(destinationPath, applicationFolder));

                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, Path.Combine(destinationPath, applicationFolder)));

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    if (newPath != null && !Path.GetExtension(newPath).Equals(".pdb", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (applicationModel.Type.Equals("filecopy", StringComparison.CurrentCultureIgnoreCase) && !applicationModel.FilesToCopy.Contains("*"))
                        {
                            foreach (var fileToCopy in applicationModel.FilesToCopy)
                            {
                                if (fileToCopy.Equals(Path.GetFileName(newPath), StringComparison.CurrentCultureIgnoreCase))
                                {
                                    File.Copy(newPath, newPath.Replace(sourcePath, Path.Combine(destinationPath, applicationFolder)), true);
                                }
                            }
                        }
                        else
                        {
                            File.Copy(newPath, newPath.Replace(sourcePath, Path.Combine(destinationPath, applicationFolder)), true);
                        }
                    }

                }
                return destinationPath;
            }
            catch (Exception ex)
            {
                log.Error($"Error while extracting component & save into source package", ex);
                return "";
            }

        }

        private bool CopyComponents(string sourcePath, string destinationPath, ApplicationModel applicatinModel)
        {

            try
            {
                var exists = Directory.Exists(Path.Combine(destinationPath));
                if (!exists)
                    Directory.CreateDirectory(Path.Combine(destinationPath));

                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, Path.Combine(destinationPath)));

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(newPath);
                    if (applicatinModel.PatchDeployments.Option.Equals("exclude", StringComparison.CurrentCultureIgnoreCase) &&
                        !applicatinModel.PatchDeployments.Items.Any(t => t.Equals(fileName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
                    }
                    else if (applicatinModel.PatchDeployments.Option.Equals("Include", StringComparison.CurrentCultureIgnoreCase) &&
                             applicatinModel.PatchDeployments.Items.Any(t => t.Equals(fileName, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
                    }

                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        //public void UnpackResourceFiles(string applicationName, string destinationPath)
        //{
        //    try
        //    {
        //        applicationName = "ProvisioningService";
        //        //var processingPath = Path.GetTempPath();
        //        var processingPath = AppDomain.CurrentDomain.BaseDirectory;
        //        var executingAssembly = Assembly.GetExecutingAssembly();
        //        var folderName = $"{executingAssembly.GetName().Name}.{applicationName}";
        //        var files = executingAssembly.GetManifestResourceNames().Where(r => r.StartsWith(folderName) && r.EndsWith(".zip")).ToArray();
        //        //processingPath = processingPath + "Extract";
        //        processingPath = @"d:\te\";
        //        var exists = Directory.Exists(processingPath);
        //        if (!exists)
        //            Directory.CreateDirectory(processingPath);
        //        foreach (var file in files)
        //        {
        //            using (var resourceStream = executingAssembly.GetManifestResourceStream(file))
        //            {
        //                var destinationFileName = file.Replace(folderName, "").Split('.')[1];
        //                if (resourceStream == null) { throw new Exception("Cannot find embedded resource '" + file + "'"); }
        //                var buffer = new byte[resourceStream.Length];
        //                resourceStream.Read(buffer, 0, buffer.Length);
        //                destinationPath = Path.Combine(processingPath, destinationFileName) + ".zip";
        //                using (var sw = new BinaryWriter(File.Open(Path.Combine(destinationPath), FileMode.Create)))
        //                {
        //                    sw.Write(buffer);
        //                }
        //            }
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        throw;
        //    }

        //}
    }
    public class SolutionBuildArgs : EventArgs
    {
        public string NameOfSolutionOrProject { get; set; }
        public string Status { get; set; }
    }

    public static class StringContains
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }

}