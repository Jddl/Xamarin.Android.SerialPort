using Android.Runtime;
using Android.Util;
using Java.IO;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Xamarin.Android.SerialPort
{
    public class SerialPortDevice
    {
        private static string TAG = "SerialPortDevice";

        [DllImport("android_serial_port-shared", EntryPoint = "MyTestFunction")]
        public static extern int Test(int x);
        [DllImport("android_serial_port-shared", EntryPoint = "OpenSerialPort")]
        private static extern int OpenSerialPort(string path, int baudrate, int flags);
        [DllImport("android_serial_port-shared", EntryPoint = "CloseSerialPort")]
        private static extern void CloseSerialPort(int handle);

        class Driver
        {
            public string DriverName { get; set; }
            public string DeviceRoot { get; set; }

            private List<File> devices = null;

            public List<File> GetDevices()
            {
                if (devices == null)
                {
                    devices = new List<File>();
                    File dev = new File("/dev");
                    File[] files = dev.ListFiles();
                    int i;
                    for (i = 0; i < files.Length; i++)
                    {
                        if (files[i].AbsolutePath.StartsWith(DeviceRoot))
                        {
                            Log.Debug(TAG, "Found new device: " + files[i]);
                            devices.Add(files[i]);
                        }
                    }
                }
                return devices;
            }

        }

        private static List<Driver> drivers = null;

        static List<Driver> GetDrivers()
        {
            if (drivers == null)
            {
                drivers = new List<Driver>();
                LineNumberReader r = new LineNumberReader(new FileReader("/proc/tty/drivers"));
                string l;
                while ((l = r.ReadLine()) != null)
                {
                    // Issue 3:
                    // Since driver name may contain spaces, we do not extract driver name with split()
                    string drivername = l.Substring(0, 0x15).Trim();
                    string[] w = System.Text.RegularExpressions.Regex.Split(l, @"\s{1,}");
                    if ((w.Length >= 5) && (w[w.Length - 1].Equals("serial")))
                    {
                        Log.Debug(TAG, "Found new driver " + drivername + " on " + w[w.Length - 4]);
                        drivers.Add(new Driver { DriverName = drivername, DeviceRoot = w[w.Length - 4] });
                    }
                }
                r.Close();
            }
            return drivers;
        }
        public static string[] GetAllDevices()
        {
            List<string> devices = new List<string>();
            // Parse each driver
            var itdrivs = GetDrivers();
            foreach (var itdriv in itdrivs)
            {
                try
                {

                    Driver driver = itdriv;
                    List<File> itdevs = driver.GetDevices();
                    foreach (File itdev in itdevs)
                    {
                        string device = itdev.Name;
                        string value = string.Format("{0} {1}", device, driver.DriverName);
                        devices.Add(value);
                    }
                }
                catch (IOException e)
                {
                    e.PrintStackTrace();
                }
            }
            return devices.ToArray();
        }
        public static string[] GetAllDevicesPath()
        {
            List<string> devices = new List<string>();
            // Parse each driver
            var itdrivs = GetDrivers();
            foreach (var itdriv in itdrivs)
            {
                try
                {

                    Driver driver = itdriv;
                    List<File> itdevs = driver.GetDevices();
                    foreach (File itdev in itdevs)
                    {
                        string device = itdev.AbsolutePath;
                        devices.Add(device);
                    }


                }
                catch (IOException e)
                {
                    e.PrintStackTrace();
                }
            }
            return devices.ToArray();
        }

        public string Path { get; set; }

        public int Baudrate { get; set; }

        public int Flags { get; set; }


        private readonly int bufferLen = 1024;

        private int handle = -1;
        private bool isOpen = false;
        private FileInputStream fileInputStream;
        private FileOutputStream fileOutputStream;
        public SerialPortDevice(string path, int baudrate, int flags = 0)
        {
            Path = path;
            Baudrate = baudrate;
            Flags = flags;
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            var device = new File(Path);
            /* Check access permission */
            if (!device.CanRead() || !device.CanWrite())
            {
                try
                {
                    /* Missing read/write permission, trying to chmod the file */
                    Java.Lang.Process su;
                    su = Runtime.GetRuntime().Exec("/system/bin/su");
                    string cmd = "chmod 666 " + device.AbsolutePath + "\n"
                            + "exit\n";
                    byte[] cmdbytes = System.Text.Encoding.ASCII.GetBytes(cmd);
                    su.OutputStream.Write(cmdbytes, 0, cmdbytes.Length);
                    if ((su.WaitFor() != 0) || !device.CanRead()
                            || !device.CanWrite())
                    {
                        throw new SecurityException();
                    }
                }
                catch (Java.Lang.Exception e)
                {
                    e.PrintStackTrace();
                    throw new SecurityException();
                }
            }

            handle = OpenSerialPort(Path, Baudrate, Flags);
            if (handle <= -1)
            {
                return false;
            }

            IntPtr fp = JNIEnv.FindClass(typeof(FileDescriptor));
            IntPtr fpm = JNIEnv.GetMethodID(fp, "<init>", "()V");
            IntPtr fpObject = JNIEnv.NewObject(fp, fpm);
            IntPtr filed = JNIEnv.GetFieldID(fp, "descriptor", "I");
            JNIEnv.SetField(fpObject, filed, handle);
            FileDescriptor res = new Java.Lang.Object(fpObject, JniHandleOwnership.TransferGlobalRef).JavaCast<FileDescriptor>();
            fileInputStream = new FileInputStream(res);
            fileOutputStream = new FileOutputStream(res);
            isOpen = true;
            //开始接收线程
            Task.Run(() =>
            {
                byte[] readData = new byte[bufferLen];
                while (isOpen)
                {
                    try
                    {
                        if (fileInputStream == null)
                        {
                            return;
                        }
                        int size = fileInputStream.Read(readData);

                        if (size<=0)
                        {
                            Close();
                            return;
                        }

                        var readBuffer = new byte[size];
                        Array.Copy(readData, readBuffer, size);

                        Received?.Invoke(this, readBuffer);

                    }
                    catch (IOException e)
                    {
                        e.PrintStackTrace();
                    }
                    catch (InterruptedException e)
                    {
                        e.PrintStackTrace();
                    }
                }
            });
            return true;
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            isOpen = false;
            if (handle <= -1)
            {
                return;
            }
            CloseSerialPort(handle);
            handle = -1;
        }

        /// <summary>
        /// 串口接收事件
        /// </summary>
        public event EventHandler<byte[]> Received;

        public void Send(byte[] data)
        {
            lock (fileOutputStream)
            {
                try
                {
                    fileOutputStream.Write(data, 0, data.Length);
                    fileOutputStream.Flush();
                }
                catch (IOException e)
                {
                    e.PrintStackTrace();
                }
            }
        }
    }


}
