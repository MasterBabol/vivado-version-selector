using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vivado_version_selector
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string regKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            using (RegistryKey key0 = Registry.CurrentUser.OpenSubKey(regKey),
                key1 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(regKey),
                key2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(regKey))
            {
                var kd0 = GetXilinxProducts(key0);
                var kd1 = GetXilinxProducts(key1);
                var kd2 = GetXilinxProducts(key2);
                var kd01 = kd0.Concat(kd1.Where(x => !kd0.Keys.Contains(x.Key))).ToDictionary(x => x.Key, x => x.Value);
                var products = kd01.Concat(kd2.Where(x => !kd01.Keys.Contains(x.Key)));
                var xdts = products
                    .Where(x => new Regex(@"Xilinx Design Tools.*").Match(x.Key).Success)
                    .Select(x => new {
                        version = x.Value.displayVersion,
                        shortVersion = new Regex(@"Xilinx Design Tools.*?(\d+\.\d+)").Match(x.Key).Groups[1].Value,
                        installedPath = x.Value.installedPath });
                var xdtsDict = xdts.ToDictionary(x => x.version, x => new
                {
                    shortVersion = x.shortVersion,
                    installedPath = x.installedPath
                });

                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    try
                    {
                        using (var f = File.OpenText(args[1]))
                        {
                            f.ReadLine();
                            var secLine = f.ReadLine();
                            var match = new Regex(@"Product Version: Vivado v(\d+\.\d+(?:\.\d+)?)").Match(secLine);
                            if (match.Success)
                            {
                                var target = match.Groups[1].Value;
                                if (xdtsDict.ContainsKey(target))
                                {
                                    var launchExePath = xdtsDict[target].installedPath + @"\Vivado\" + xdtsDict[target].shortVersion + @"\bin\vivado.bat";
                                    ProcessStartInfo psi = new ProcessStartInfo(launchExePath, args[1]);
                                    psi.UseShellExecute = false;
                                    psi.CreateNoWindow = true;
                                    Process.Start(psi);
                                }
                                else
                                    MessageBox.Show(string.Format("Vivado {0} is not present.", target), "Vivado Version Selector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                                MessageBox.Show("Cannot determine the version of this project.", "Vivado Version Selector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "Vivado Version Selector", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                    MessageBox.Show("Please specify a valid Vivado project.", "Vivado Version Selector", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        struct XilinxProduct
        {
            public string displayVersion;
            public string installedPath;
        }

        private static Dictionary<string, XilinxProduct> GetXilinxProducts(RegistryKey regFrom)
        {
            Dictionary<string, XilinxProduct> products = new Dictionary<string, XilinxProduct>();
            var subkeys = regFrom.GetSubKeyNames();
            foreach (string subkey_name in subkeys)
            {
                using (RegistryKey subkey = regFrom.OpenSubKey(subkey_name))
                {
                    var curApp = (string)subkey.GetValue("DisplayName");
                    if (curApp != null && curApp.Contains("Xilinx"))
                    {
                        XilinxProduct xp;
                        xp.displayVersion = (string)subkey.GetValue("DisplayVersion");
                        xp.installedPath = (string)subkey.GetValue("InstallLocation");
                        products.Add(curApp, xp);
                    }
                }
            }

            return products;
        }
    }
}
