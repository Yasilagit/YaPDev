using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Buraq.YaP.Helper;

namespace Buraq.YaP.Processor
{
    public class YaPConfigureLog4Net
    {
        public static void ConfigureLog4Net()
        {

            ///// CONTENT IN THE APP.CONFIG THAT WILL BE TRANSFORMED TO FLUENT CONFIGURATION:
            /////
            /////    <appender name="LogFileAppender" type="log4net.Appender.RollingFileAppender">
            /////      <file value="logfile.txt" />
            /////      <appendToFile value="true" />
            /////      <rollingStyle value="Size" />
            /////      <maxSizeRollBackups value="5" />
            /////      <maximumFileSize value="1024KB" />
            /////      <staticLogFileName value="true" />
            /////      <layout type="log4net.Layout.PatternLayout">
            /////        <conversionPattern value="%date %level %logger - %message %exception%newline" />
            /////      </layout>
            /////    </appender>
            /////    <root>
            /////      <level value="ALL" />
            /////      <appender-ref ref="LogFileAppender" />
            /////    </root>

            var fileappender = new log4net.Appender.RollingFileAppender();
            var projectName = Helper.Utility.GetAppSettingByKey("ProjectName");
            var logPath = Path.Combine($"{Path.GetTempPath()}/{projectName}", "Log");
            fileappender.File = Path.Combine(logPath, "YaPProcess.log");
            fileappender.AppendToFile = true;
            fileappender.RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Size;
            fileappender.MaxSizeRollBackups = 5;
            fileappender.MaximumFileSize = "1024KB";
            fileappender.StaticLogFileName = true;
            fileappender.Threshold = log4net.Core.Level.Debug;
            fileappender.Layout = new log4net.Layout.PatternLayout("%date %level %logger - %message %exception%newline");
            fileappender.ActivateOptions();
            ((log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository()).Root.AddAppender(fileappender);
            log4net.Config.BasicConfigurator.Configure(fileappender);
        }
    }
}
