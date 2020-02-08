using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Serilog;
using Shibari.Dom.Server.Core;
using Topshelf;
using Trinet.Core.IO.Ntfs;

namespace Shibari.Dom.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            #region Logging

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .WriteTo.Console()
                .WriteTo.RollingFile("Logs\\Shibari.Dom.Server-{Date}.log")
                .CreateLogger();

            #endregion

            #region Global exception handler

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Log.Fatal("Unhandled exception: {Exception}", (Exception) eventArgs.ExceptionObject);
            };

            #endregion

            Log.Information("Launching Shibari, version: {Version}",
                Assembly.GetExecutingAssembly().GetName().Version);

            #region Self-Unblocking

            var domRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
            var rootDrive = new DriveInfo(domRoot);

            // ADS is only present on NTFS formatted drives
            if (rootDrive.DriveFormat.Equals("NTFS", StringComparison.InvariantCultureIgnoreCase))
                try
                {
                    var files = Directory.GetFiles(domRoot, "*.dll", SearchOption.AllDirectories);

                    foreach (var fileInfo in files.Select(f => new FileInfo(f)))
                    {
                        if (!fileInfo.AlternateDataStreamExists("Zone.Identifier")) continue;
                        Log.Information("Removing Zone.Identifier from file {File}", fileInfo.Name);
                        var ads = fileInfo.GetAlternateDataStream("Zone.Identifier", FileMode.Open);
                        ads.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal("Error unblocking files, program may be unusable, contact support! {@Exception}", ex);
                    Process.Start("https://forums.vigem.org/topic/375/manually-unblock-shibari-archive");
                    Console.WriteLine("Press any key to escape the madness! :)");
                    Console.ReadKey();
                    return;
                }
            else
                Log.Information("Process started from {Filesystem} formatted drive, no unblocking necessary",
                    rootDrive.DriveFormat);

            #endregion

            #region Single instance check & hosting

            // https://stackoverflow.com/a/229567

            // get application GUID as defined in AssemblyInfo.cs
            var appGuid = Guid.Parse("{E7A7AB5E-2C61-4677-9946-427A6B8E0C53}");

            // unique id for global mutex - Global prefix means it is global to the machine
            var mutexId = $"Global\\{{{appGuid}}}";

            // Need a place to store a return value in Mutex() constructor call
            bool createdNew;

            // edited by Jeremy Wiebe to add example of setting up security for multi-user usage
            // edited by 'Marc' to work also on localized systems (don't use just "Everyone") 
            var allowEveryoneRule =
                new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid
                        , null)
                    , MutexRights.FullControl
                    , AccessControlType.Allow
                );
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            // edited by MasonGZhwiti to prevent race condition on security settings via VanNguyen
            using (var mutex = new Mutex(false, mutexId, out createdNew, securitySettings))
            {
                // edited by acidzombie24
                var hasHandle = false;
                try
                {
                    try
                    {
                        // note, you may want to time out here instead of waiting forever
                        // edited by acidzombie24
                        // mutex.WaitOne(Timeout.Infinite, false);
                        hasHandle = mutex.WaitOne(200, false);
                        if (hasHandle == false)
                            throw new ApplicationException(
                                "This application can only be run once, please check if the service may be running already");
                    }
                    catch (AbandonedMutexException)
                    {
                        // Log the fact that the mutex was abandoned in another process,
                        // it will still get acquired
                        hasHandle = true;
                    }

                    HostFactory.Run(x =>
                    {
                        x.Service<BusEmulatorHubService>(s =>
                        {
                            s.ConstructUsing(name => new BusEmulatorHubService());
                            s.WhenStarted(tc => tc.Start());
                            s.WhenStopped(tc => tc.Stop());
                        });
                        x.RunAsLocalSystem();

                        x.SetDescription("Manages AirBender, FireShock & BthPS3 Devices.");
                        x.SetDisplayName("Shibari Dom Server");
                        x.SetServiceName("Shibari.Dom.Server");
                    });
                }
                finally
                {
                    // edited by acidzombie24, added if statement
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }

            #endregion
        }
    }
}