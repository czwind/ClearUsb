using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace ClearUsb
{
    public partial class ClearUsbService : ServiceBase
    {
        public ClearUsbService()
        {
            InitializeComponent();
        }
       

        protected override void OnStart(string[] args)
        {
            this.RequestAdditionalTime(100000);
            ClearUSBRecordsFromRegistry();

        }

    
        protected override void OnStop()
        {
            
        }


        private bool ClearUSBRecordsFromRegistry()
        {

            #region 清除USB记录
            string strToRegistry = string.Empty;
            RegistryKey HKLM = Registry.LocalMachine;
            try
            {
                using (RegistryKey systemkey = HKLM.OpenSubKey("SYSTEM", true))
                {
                    string key = "CurrentControlSet";

                    #region 处理Enum下的USB、USBSTOR键
                    //处理USB、USBSTOR键

                    RegistryKey enumKey = systemkey.OpenSubKey(key + @"\" + "ENUM", true);
                        if (enumKey != null)
                        {
                            //enumKey.DeleteSubKey("USBSTOR");
                            RegistryKey usbStorkey = enumKey.OpenSubKey("USBSTOR", true);
                            if (usbStorkey != null)
                            {
                                enumKey.DeleteSubKeyTree("USBSTOR");
                            }

                            RegistryKey UsbKey = enumKey.OpenSubKey("USB", true);
                            if (UsbKey != null)
                            {
                                string[] UsbSubkeyNames = UsbKey.GetSubKeyNames();
                                foreach (string usbsubkey in UsbSubkeyNames)
                                {
                                    if (!usbsubkey.ToUpper().Contains("ROOT"))
                                    {
                                        //每行后面添加两空行 \r\n\r\n
                                        //strToRegistry += string.Format(@"[-{0}\{1}]" + "\r\n\r\n", UsbKey.Name, usbsubkey);
                                        UsbKey.DeleteSubKeyTree(usbsubkey);
                                    }
                                }
                            }
                        }
                    #endregion

                    #region 处理ConstrolSet下面的Control键下面的DevicesClasses键值
                    string[] a5d53fkeys = { "{2da1fe75-aab3-4d2c-acdf-39088cada665}","{325ddf96-938c-11d3-9e34-0080c82727f4}", "{a5dcbf10-6530-11d2-901f-00c04fb951ed}", "{53f56307-b6bf-11d0-94f2-00a0c91efb8b}",};

                    //处理HKLM\System\ControlSet\Control\DeviceClasses下面的USB使用记录
                    RegistryKey deviceClassesKey = systemkey.OpenSubKey(key + @"\" + "Control" + @"\" + "DeviceClasses", false);
                    if (deviceClassesKey != null)
                    {
                        string[] deviceSubkeyNames = deviceClassesKey.GetSubKeyNames();

                        foreach (string devicesubkey in deviceSubkeyNames)
                        {

                            #region 删除DeviceClasses下的a5d...53f..含usb字眼的部分子项
                            foreach (string strkey in a5d53fkeys)
                            {

                                if (devicesubkey.ToLower().Contains(strkey))
                                {
                                    RegistryKey usba5dkey = deviceClassesKey.OpenSubKey(strkey, true);
                                    if (usba5dkey != null)
                                    {
                                        string[] usba5dkeySubNames = usba5dkey.GetSubKeyNames();
                                        foreach (string usba5dsubkey in usba5dkeySubNames)
                                        {
                                            if (usba5dsubkey.ToUpper().Contains("USB"))
                                            {
                                                // strToRegistry += string.Format(@"[-{0}\{1}\{2}]" + "\r\n\r\n", deviceClassesKey.Name, strkey, usba5dsubkey);

                                                usba5dkey.DeleteSubKeyTree(usba5dsubkey);
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion

                        }
                    }

                    #endregion
                        
                   

                }
                LogToFile(DateTime.Now + "   " + "清除USB使用记录。 ", new FileInfo("zhxyWindowsSecurity.txt"));
                return true;
            }
            catch (Exception ex)
            {
                LogToFile(DateTime.Now + "   " + "ClearUSBRecordsFromRegistry(): " + "错误： " + ex.Message, new FileInfo("zhxyWindowsSecurity.txt"));
                return false;
            }
            #endregion
        }
        private void LogToFile(string s, FileInfo fi)
        {
            try
            {
                if (!fi.Exists)
                {
                    using (StreamWriter sw = fi.CreateText())
                    {
                        sw.WriteLine(s);
                    }
                }

                else
                {
                    using (StreamWriter sw = fi.AppendText())
                    {
                        sw.WriteLine(s);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile(DateTime.Now + "   LogFile():" + "错误： " + ex.Message, new FileInfo("zhxyWindowsSecurity.txt"));
            }
        }


        private void UninstallService(string ServiceExeName)
        {

            //组合出需要shell的完整格式
            string appPath = System.Environment.CurrentDirectory;
            string filePath = System.IO.Path.Combine(appPath, ServiceExeName);
            string shellArguments = string.Format("/u \"{0}\"", filePath); //需要卸载的服务程序名称

            //用Process调用 installutil 卸载服务
            using (Process uninstallProc = new Process())
            {
                uninstallProc.StartInfo.FileName = Path.Combine(System.Environment.GetEnvironmentVariable("WINDIR"), @"Microsoft.NET\Framework\v2.0.50727\installutil.exe");
                uninstallProc.StartInfo.Arguments = shellArguments;
                //隐藏进程本身的窗口
                uninstallProc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                uninstallProc.Start();
                //等待进程完成
                uninstallProc.WaitForExit();
                uninstallProc.Close();
            }
        }

    }
}
