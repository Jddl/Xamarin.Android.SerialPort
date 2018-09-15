using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Forms.Test
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        ISerialPort serialPort;

        int sendCount = 0;
        int recvCount = 0;


        public MainPage()
        {
            InitializeComponent();
            serialPort = DependencyService.Get<ISerialPort>();
            serialPort.Received += (s, data) =>
            {
                recvCount += data.Length;

            };
            serialPort.Open("/dev/ttyS3", 115200);

            Task.Run(() =>
            {

                var sendBuffer = new byte[234];
                for (byte i = 0; i < sendBuffer.Length; i++)
                {
                    sendBuffer[i] = i;
                }

                while (true)
                {
                    //System.Diagnostics.Debug.WriteLine($"Count:{sendCount} {recvCount}");
                    //Device.BeginInvokeOnMainThread(() =>
                    //{
                    //    labSend.Text = $"Send:{sendCount}";
                    //    labRecv.Text = $"Recv:{recvCount}";
                    //});

                    serialPort.Send(sendBuffer);
                    sendCount += sendBuffer.Length;
                    Thread.Sleep(200);
                }
            });

            Device.StartTimer(TimeSpan.FromMilliseconds(500), () =>
            {
                labSend.Text = $"Send:{sendCount}";
                labRecv.Text = $"Recv:{recvCount}";
                // do something every 60 seconds
                return true; // runs again, or false to stop
            });
        }
    }
}
