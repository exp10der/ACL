using System;
using System.Configuration;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
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
                CountUserGen = int.Parse(ConfigurationManager.AppSettings["CountUserGen"]),
                NameFile = ConfigurationManager.AppSettings["FileName"]
            };

            log.Debug(
                $"Apply Settings: {nameof(settings.CountUserGen)} = {settings.CountUserGen}, {nameof(Environment.MachineName)} = {Environment.MachineName}, {nameof(settings.NameFile)} = {settings.NameFile}");

            var users = Enumerable.Range(1, settings.CountUserGen)
                .Select(n => "user" + n).ToList();

            var sw = Stopwatch.StartNew();

            users
                .ForEach(n =>
                {

                    // Возможно надо обернуть в юзинги?
                    var ad = new DirectoryEntry("WinNT://" +
                                                Environment.MachineName + ",computer");

                    var newUser = ad.Children.Add(n, "user");

                    newUser.Invoke("SetPassword", "Pa$$worD");
                    newUser.CommitChanges();
                });

            sw.Stop();
            log.Debug($"Creating users took {sw.Elapsed}");

            var dInfo = new DirectoryInfo(settings.NameFile);
            var dSecurity = dInfo.GetAccessControl();

            sw.Start();
            users.ForEach(
                login =>
                    dSecurity.AddAccessRule(new FileSystemAccessRule(login, FileSystemRights.FullControl,
                        AccessControlType.Allow))
                );
            dInfo.SetAccessControl(dSecurity);
            sw.Stop();
            log.Debug($"Setting rights took {sw.Elapsed}");
        }
    }
}