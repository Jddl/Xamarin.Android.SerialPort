using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using System.Threading.Tasks;
using System.Threading;

namespace Xamarin.Android.SerialPort.Test
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var value = SerialPortDevice.Test(5);

            System.Diagnostics.Debug.WriteLine(value);


            var portNames = SerialPortDevice.GetAllDevicesPath();
            foreach (var item in portNames)
            {
                System.Diagnostics.Debug.WriteLine(item);
            }
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);


            var serialport = new SerialPortDevice("/dev/ttyS3", 115200);
            int RecvCount = 0;
            int SendCount = 0;
            serialport.Received += (s, e) =>
            {
                RecvCount += e.Length;

            };
            try
            {
                serialport.Open();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return;
            }

            Task.Run(() =>
            {
                while (true)
                {
                    System.Diagnostics.Debug.WriteLine($"Count:{SendCount} {RecvCount}");


                    serialport.Send(new byte[] { 0x01, 0x02 });
                    SendCount += 2;
                    Thread.Sleep(500);
                }
            });

        }
    }
}

