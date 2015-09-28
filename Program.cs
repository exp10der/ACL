using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using NLog;

namespace AclGen
{
    internal class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static void Main(string[] args)
        {
            var settings = new
            {
                CountUserGen = int.Parse(ConfigurationSettings.AppSettings["CountUserGen"]),
                CompName = ConfigurationSettings.AppSettings["ComputerName"],
                NameFile = ConfigurationSettings.AppSettings["FileName"]
            };

            log.Debug(
                $"Apply Settings: {nameof(settings.CountUserGen)} ={settings.CountUserGen}, {nameof(settings.CompName)}={settings.CompName}, {nameof(settings.NameFile)} ={settings.NameFile}");

            var users = Enumerable.Range(1, settings.CountUserGen)
               .Select(n => "user" + n).ToList();

            var sw = Stopwatch.StartNew();

            users
                .ForEach(n =>
                {
                    var p = new Process
                    {
                        StartInfo =
                        {
                            WindowStyle = ProcessWindowStyle.Hidden,
                            FileName = "net",
                            Arguments = $"user {n} Pa$$worD /ADD"
                        }
                    };
                    p.Start();
                    p.WaitForExit();
                });

            sw.Stop();
            log.Debug($"Creating users took {sw.Elapsed}");
            sw.Start();
            users.ForEach(
                login =>
                    AddDirectorySecurity(settings.NameFile, settings.CompName + @"\" + login, FileSystemRights.FullControl,
                        AccessControlType.Allow));
            sw.Stop();
            log.Debug($"Setting rights took {sw.Elapsed}");
        }
        // Adds an ACL entry on the specified directory for the specified account.
        public static void AddDirectorySecurity(string FileName, string Account, FileSystemRights Rights,
            AccessControlType ControlType)
        {
            // Create a new DirectoryInfo object.
            var dInfo = new DirectoryInfo(FileName);

            // Get a DirectorySecurity object that represents the 
            // current security settings.
            var dSecurity = dInfo.GetAccessControl();

            // Add the FileSystemAccessRule to the security settings. 
            dSecurity.AddAccessRule(new FileSystemAccessRule(Account,
                Rights,
                ControlType));

            // Set the new access settings.
            dInfo.SetAccessControl(dSecurity);
        }
    }
}