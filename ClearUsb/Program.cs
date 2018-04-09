using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ClearUsb
{
    class ClearUsb
    {
        static System.Timers.Timer timer1;
        static bool bExit = false;
        static void Main(string[] args)
        {
         
            if (args.Length == 0)
            {
                InstallService("ClearUsbService.exe");

                timer1 = new System.Timers.Timer();
                timer1.Interval = 1000 * 60 * 5;
                //必须调用此语句,定时器才能定时产生elapsed事件。
                timer1.Start();
                //或者
                //timer1.Enabled = true;
                timer1.Elapsed += Timer1_Elapsed;
                timer1.AutoReset = true;  //false: 定时事件只产生一次。 true: 重复地产生定时事件。

                while(!bExit)
                {
                    Thread.Sleep(4000);
                }
            }
            if (args.Length == 1)
            {
                if (args[0].ToLower() == "/u" || args[0].ToLower() == "/uninstall")
                {
                    UninstallService("ClearUsbService.exe");
                    bExit = true;
                }
                else
                {
                    Console.WriteLine("作用：安装或者卸载清除Usb使用记录服务。");
                    Console.WriteLine("用法：");
                    Console.Write("\tClearUsb");
                    Console.WriteLine("\t安装清除Usb使用记录服务。");
                    Console.Write("\tClearUsb /u");
                    Console.WriteLine("\t卸载清除Usb使用记录服务。");

                }

            }
        }

        private static void Timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UninstallService("ClearUsbService.exe");
            bExit = true;
        }


        private static void InstallService(string ServiceExeName)
        {
            try
            {

                string appPath = System.Environment.CurrentDirectory;
                string filePath = System.IO.Path.Combine(appPath, ServiceExeName);

                string shellArguments = string.Format("\"{0}\"", filePath); //需要安装的服务程序名称

                //用Process调用secedit生成sec.sdb文档
                using (Process InstallUtilProc = new Process())
                {

                    InstallUtilProc.StartInfo.FileName = Path.Combine(System.Environment.GetEnvironmentVariable("WINDIR"), @"Microsoft.NET\Framework\v2.0.50727\installutil.exe");

                    InstallUtilProc.StartInfo.Arguments = shellArguments;
                    //隐藏 secedit 本身的窗口

                    InstallUtilProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    InstallUtilProc.Start();
                    //等待服务安装完成
                    InstallUtilProc.WaitForExit();

                    //启动服务
                    shellArguments = string.Format("Start {0}", ServiceExeName.Substring(0, ServiceExeName.IndexOf(".")));  //启动服务的命令行参数
                    InstallUtilProc.StartInfo.FileName = "net";
                    InstallUtilProc.StartInfo.Arguments = shellArguments;

                    //隐藏net本身的窗口
                    InstallUtilProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    InstallUtilProc.Start();

                    //等待启动服务完成
                    InstallUtilProc.WaitForExit();

                    //设置服务自动启动
                    //下面一行start={1}中一定不要包含空格，否则命令不能正确执行，不出错，但不能正确设置服务的状态
                    shellArguments = string.Format("config {0} start= {1}", ServiceExeName.Substring(0, ServiceExeName.IndexOf(".")), "auto");
                    InstallUtilProc.StartInfo.FileName = @"sc.exe";
                    InstallUtilProc.StartInfo.Arguments = shellArguments;

                    InstallUtilProc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    InstallUtilProc.Start();

                    InstallUtilProc.WaitForExit();

                    InstallUtilProc.Close();

                }

                LogToFile(DateTime.Now + "   " + "服务安装完成。", new FileInfo("ClearUsb.txt"));
            }
            catch (Exception ex)
            {
                LogToFile(DateTime.Now + "   " + "InstallService(): " + ex.Message, new FileInfo("ClearUsb.txt"));
            }
        }
        private static void UninstallService(string ServiceExeName)
        {

            //组合出需要shell的完整格式
            string appPath = System.Environment.CurrentDirectory;
            string filePath = System.IO.Path.Combine(appPath, ServiceExeName);
            string shellArguments = string.Format("/u \"{0}\"", filePath); //需要卸载的服务程序名称

            //用Process调用 installutil 卸载服务
            using (Process uninstallProc = new Process())
            {
                try
                {
                    uninstallProc.StartInfo.FileName = Path.Combine(System.Environment.GetEnvironmentVariable("WINDIR"), @"Microsoft.NET\Framework\v2.0.50727\installutil.exe");
                    uninstallProc.StartInfo.Arguments = shellArguments;
                    //隐藏进程本身的窗口
                    uninstallProc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    uninstallProc.Start();
                    //等待进程完成
                    uninstallProc.WaitForExit();
                    uninstallProc.Close();
                    LogToFile(DateTime.Now + "   " + "UninstallService(): 已经运行，服务被卸载", new FileInfo("ClearUsb.txt"));
                }
                catch(Exception ex)
                {
                    LogToFile(DateTime.Now + "   " + "UninstallService(): " + ex.Message, new FileInfo("ClearUsb.txt"));

                }

            }
        }
        private static void LogToFile(string s, FileInfo fi)
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

    }


}
