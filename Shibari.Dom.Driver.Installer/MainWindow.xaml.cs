using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Nefarius.Devcon;
using Shibari.Dom.Driver.Installer.Util;

namespace Shibari.Dom.Driver.Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InstallDriverMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Install");
        }

        private async void CreateNewEventSetter_OnHandler(object sender, RoutedEventArgs e)
        {
            await Task.Factory.StartNew(() =>
            {
                var ret = Devcon.Create("System", ViGEmDevice.ClassGuid, "Root\\ViGEmBus\0\0");

                if (ret)
                {
                    var t = Devcon.Install(
                        @"D:\Development\C\ViGEm\Signed\ViGEmBus_signed_Win7-10_x86_x64_v1.13.2.0\x64\ViGEmBus.inf",
                        out var rr);
                }

                var r = new Win32Exception(Marshal.GetLastWin32Error());

                ViGEmListBox.GetBindingExpression(ItemsControl.ItemsSourceProperty).UpdateTarget();
            });
        }
    }
}
