using System;
using System.Collections;
using System.Collections.Generic;
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


            var context = new ProgressContext<string>(users.Select(n =>
            {
                // Возможно надо добавить юзинги??
                var ad = new DirectoryEntry("WinNT://" +
                                            Environment.MachineName + ",computer");

                var newUser = ad.Children.Add(n, "user");

                newUser.Invoke("SetPassword", "Pa$$worD");
                newUser.CommitChanges();
                return n;
            }));


            context.UpdateProgress += (sender, e) =>
            {
                var currentPercentage = (e.Count*100)/users.Count;
                if (currentPercentage != e.Count)
                {
                    Console.Clear();
                    Console.WriteLine("Create user {0}%", currentPercentage);
                }
            };

            context.ToArray();


            sw.Stop();
            log.Debug($"Creating users took {sw.Elapsed}");

            var dInfo = new DirectoryInfo(settings.NameFile);
            var dSecurity = dInfo.GetAccessControl();

            sw.Start();

            var addRule = new ProgressContext<string>(users);
            addRule.UpdateProgress += (sender, e) =>
            {
                var currentPercentage = (e.Count*100)/users.Count;
                if (currentPercentage != e.Count)
                {
                    Console.Clear();
                    Console.WriteLine("Set rules {0}%", currentPercentage);
                }
            };


            addRule.Select(login =>
            {
                dSecurity.AddAccessRule(new FileSystemAccessRule(login, FileSystemRights.FullControl,
                    AccessControlType.Allow));
                return login;
            }).ToArray();

            dInfo.SetAccessControl(dSecurity);
            sw.Stop();
            log.Debug($"Setting rights took {sw.Elapsed}");
        }

        public class ProgressArgs : EventArgs
        {
            public ProgressArgs(int count)
            {
                Count = count;
            }

            public int Count { get; }
        }

        public class ProgressContext<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> source;

            public ProgressContext(IEnumerable<T> source)
            {
                this.source = source;
            }

            public event EventHandler<ProgressArgs> UpdateProgress;

            protected virtual void OnUpdateProgress(int count)
            {
                var handler = UpdateProgress;
                handler?.Invoke(this, new ProgressArgs(count));
            }

            public IEnumerator<T> GetEnumerator()
            {
                var count = 0;
                foreach (var item in source)
                {
                    // The yield holds execution until the next iteration,
                    // so trigger the update event first.
                    OnUpdateProgress(++count);
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}