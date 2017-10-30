using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using Nefarius.Devcon;

namespace Shibari.Dom.Driver.Installer.Util
{
    public class ViGEmDevice
    {
        public static Guid ClassGuid => Guid.Parse("{96E42B22-F5E9-42F8-B043-ED0F932F014F}");

        public static IEnumerable<ViGEmDevice> Devices
        {
            get
            {
                var list = new List<ViGEmDevice>();
                var instance = 0;

                while (Devcon.Find(ClassGuid, out var path, out var instanceId, instance++))
                {
                    using (var objSearcher =
                        new ManagementObjectSearcher($"Select * from Win32_PnPSignedDriver Where DeviceID = '{instanceId.Replace("\\", "\\\\")}'")
                    )
                    {
                        using (var objCollection = objSearcher.Get())
                        {
                            var device = objCollection.Cast<ManagementObject>().First();

                            list.Add(new ViGEmDevice()
                            {
                                DevicePath = path,
                                InstanceId = instanceId,
                                DeviceName = device["DeviceName"].ToString()
                            });
                        }
                    }
                }

                return list;
            }
        }

        public string DevicePath { get; private set; }

        public string InstanceId { get; private set; }

        public string DeviceName { get; private set; }
    }
}