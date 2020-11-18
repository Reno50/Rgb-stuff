using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using ColoreColor = Colore.Data.Color;
using Colore;
using Colore.Effects.Keyboard;
using Colore.Effects.ChromaLink;
using ColorD = System.Drawing.Color;
using System.Threading.Tasks;

namespace RGBwithUIv2
{
    public partial class MainWindow : Window
    {
        public static HttpClient client = new HttpClient();
        public static List<float> wiocolor = new List<float> { 0, 0, 0 };
        public static string hexstring = "";
        public static string accessToken = "?access_token=0f776613643b54641d070ecd3842f565";
        public static IChroma chroma = ColoreProvider.CreateNativeAsync().Result;
        public static int wait = 0;
        public const float huemult = 360 / KeyboardConstants.MaxColumns;
        public static bool isRunning = false;
        public static System.Timers.Timer timer;
        public static int count = 0;
        public static CustomKeyboardEffect keyboard = new CustomKeyboardEffect(new ColoreColor(0, 0, 0));
        public MainWindow()
        {
            InitializeComponent();
            submain();
        }
        public static void ColorToHSV(ColorD color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }
        public static ColorD ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return ColorD.FromArgb(255, v, t, p);
            else if (hi == 1)
                return ColorD.FromArgb(255, q, v, p);
            else if (hi == 2)
                return ColorD.FromArgb(255, p, v, t);
            else if (hi == 3)
                return ColorD.FromArgb(255, p, q, v);
            else if (hi == 4)
                return ColorD.FromArgb(255, t, p, v);
            else
                return ColorD.FromArgb(255, v, p, q);
        }
        static void startLoop()
        {
            timer = null;
            timer = new System.Timers.Timer(100);
            timer.AutoReset = true;
            // Hook up the Elapsed event for the timer. 
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }
        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            submain();
        }
        static void stopLoop()
        {
            if (timer != null) {
                timer.Stop();
                timer.Dispose();
                System.GC.Collect();
            }
        }
        static void submain()
        {
            hexstring = colorFromList(count);
            string url = "https://us.wio.seeed.io/v1/node/GroveLedWs2812D0/segment/0/" + hexstring + accessToken;
            wioPost(url, client);
            for (int c = 0; c < 22; c++)
            {
                float red = 0;
                float gre = 0;
                float blu = 0;
                double hue;
                hue = (huemult * c) + count;
                if (hue > 360)
                {
                    hue = hue - ((int)(hue / 360) * 360);
                }
                double h = hue;
                double s = 1;
                double v = 1;
                ColorD color = ColorFromHSV(h, s, v);
                red = color.R * 255;
                gre = color.G * 255;
                blu = color.B * 255;
                for (int r = 0; r < 6; r++)
                {
                    keyboard[r, c] = new ColoreColor(red, gre, blu);
                }
            }
            //chroma.SetAllAsync(ColoreColor.Black);
            try
            {
                var task = Task.Run(() => chroma.Keyboard.SetCustomAsync(keyboard).Wait());
                task.Wait(TimeSpan.FromSeconds(0.05));
                var task2 = Task.Run(() => chroma.Mouse.SetAllAsync(keyboard[1, 21]));
                task2.Wait(TimeSpan.FromSeconds(0.05));
            }
            catch (Exception e) { }
            count+=1;
            if (count >= 360)
            {
                count = 0;
                System.GC.Collect();
            }
        }
        static string colorFromList(int count)
        {
            string returnstring = "";
            for (float i = 0; i < 30; i+=1)
            {
                float rede = 0;
                float gree = 0;
                float blue = 0;
                double h;
                h = (i * 12) + count;
                if (h > 360)
                {
                    h = h - ((int)(h / 360) * 360);
                }
                double s = 1;
                double v = 1;
                ColorD color2 = ColorFromHSV(h, s, v);
                rede = color2.R;
                gree = color2.G;
               blue = color2.B;
                List<float> list = new List<float> { rede, gree, blue };
                returnstring += toHex(list);
            }
            return returnstring;
        }
        static string toHex(List<float> color)
        {
            List<int> color2 = new List<int> { 0, 0, 0 };
            string aftercolor = "";
            for (int i = 0; i < color.Count; i++)
            {
                color2[i] = Convert.ToInt32(color[i]);
            }
            
            aftercolor += color2[0].ToString("X2");
            aftercolor += color2[1].ToString("X2");
            aftercolor += color2[2].ToString("X2");

            if (aftercolor.Length == 6)
            {
                return aftercolor;
            }
            return "000000";
        }
        static void wioPost(string url, HttpClient tempclient)
        {
            Dictionary<string, string> values = new Dictionary<string, string> { { "", "" } };
            var content = new FormUrlEncodedContent(values);
            //await tempclient.PostAsync(url, content);
            try
            {
                var response = Task.Run(() => client.PostAsync(url, content));
                response.Wait(TimeSpan.FromSeconds(0.05));
            }
            catch (Exception e) { }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            stopLoop();
            startLoop();
        }
    }
}
