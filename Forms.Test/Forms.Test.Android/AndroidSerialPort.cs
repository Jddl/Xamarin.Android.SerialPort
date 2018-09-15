using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Forms.Test.Droid;
using Xamarin.Android.SerialPort;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidSerialPort))]
namespace Forms.Test.Droid
{
    public class AndroidSerialPort : ISerialPort
    {
        SerialPortDevice serialPortDevice;

        public event EventHandler<byte[]> Received;

        public void Close()
        {
            if (serialPortDevice!=null)
            {
                serialPortDevice.Received -= SerialPortDevice_Received;
                serialPortDevice.Close();
            }
            serialPortDevice = null;
        }

        public bool Open(string name, int baudrate, int flags = 0)
        {
            serialPortDevice = new SerialPortDevice(name,baudrate,flags);
            serialPortDevice.Received += SerialPortDevice_Received;
            return serialPortDevice.Open();
        }

        private void SerialPortDevice_Received(object sender, byte[] e)
        {
            Received?.Invoke(sender, e);
        }

        public void Send(byte[] data)
        {
            serialPortDevice?.Send(data);
        }
    }
}