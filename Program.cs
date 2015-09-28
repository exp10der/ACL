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
       static Logger log = LogManager.GetCurrentClassLogger();
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


            MainAsync(settings.CountUserGen, settings.CompName, settings.NameFile).Wait();
        }

        private static async Task MainAsync(int Count, string compName, string file)
        {
            var users = Enumerable.Range(1, Count)
                .Select(n => "user" + n).ToList();

            var sw = Stopwatch.StartNew();
            var tasks =
                users
                    .Select(async n => await RunProcessAsync("net", "user " + n + " Pa$$worD /ADD")).ToList();

            var results = await Task.WhenAll(tasks);
            sw.Stop();
            log.Debug($"Creating users took {sw.Elapsed}");
            sw.Start();
            users.ForEach(
                login =>
                    AddDirectorySecurity(file, compName+ @"\" + login, FileSystemRights.FullControl,
                        AccessControlType.Allow));
            sw.Stop();
            log.Debug($"Setting rights took {sw.Elapsed}");
        }

        // TODO : Стоит добавить анализ через bool создалась ли учетка возможно...
        private static Task<bool> RunProcessAsync(string fileName, string arg)
        {
            // there is no non-generic TaskCompletionSource
            var tcs = new TaskCompletionSource<bool>();

            var process = new Process
            {
                StartInfo = {FileName = fileName, Arguments = arg, WindowStyle = ProcessWindowStyle.Hidden },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(true);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
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