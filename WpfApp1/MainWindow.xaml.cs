using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Emoji.Wpf;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VoiceClient;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using TextBlock = Emoji.Wpf.TextBlock;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        StreamClient sc;
        HubConnection connection;
        Bitmap pic = null;
        int x = 10, y = 10;
        bool mic_muted = false;
        bool speaker_muted = false;
        int port;
        string group;
        public MainWindow()
        {
            InitializeComponent();
            pass = group = Logger.group;
            pic = new Bitmap(1, 1);
            ConfigSignalRConnection();
        }

        async void ConfigSignalRConnection()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://192.168.1.111:5000/CastHub")
                .WithAutomaticReconnect()
                .Build();
            connection.On<string, int, int, bool, int, int>("UpdateScreen", UpdateScreen);
            connection.On<string, string>("newMessage", NewMessage);
            await connection.StartAsync();
            await connection.InvokeAsync("SetName", Logger.name);
            await connection.InvokeAsync("AddToGroup", group);
            port = await connection.InvokeAsync<int>("getport", group);
            await connection.InvokeAsync("getscreen");
            await connection.InvokeAsync("getMessages");
            sc = new StreamClient(port, "192.168.1.111");
            sc.Init();
            sc.ConnectToServer();
        }
        void NewMessage(string sender, string message)
        {
            Border b = new Border();
            b.CornerRadius = new CornerRadius(8);
            b.BorderBrush = Brushes.Gray;
            if(sender == Logger.name)b.BorderBrush = Brushes.DarkGreen;
            b.BorderThickness = new Thickness(2);
            
            Frame container = new Frame();
            container.MouseDoubleClick += (s,e) => { };
            b.Margin = new Thickness { Left = 5,Right=5,Top=5 };
            StackPanel msg = new StackPanel();
            Label sndr = new Label();
            sndr.FontSize = 11;
            sndr.Foreground = Brushes.DarkGray;
            if(sender == Logger.name)sndr.Foreground = Brushes.DarkGreen;

            TextBlock mesg = new TextBlock();
            mesg.Text = message;
            mesg.FontSize = 14;
            mesg.Foreground = Brushes.Black;
            mesg.Margin = new Thickness { Right = 3, Bottom = 3, Left = 3 };
            sndr.Content = sender;
            if (ia(message[0])) mesg.FlowDirection = FlowDirection.RightToLeft;
            msg.Children.Add(sndr);
            msg.Children.Add(mesg);
            container.Content = msg;
            
            b.Child = container;
            MessageList.Children.Add(b);
            scrollViewer.ScrollToEnd();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            connection.DisposeAsync();
            sc.ConnectToServer();
        }

        private void pbmic_Click(object sender, EventArgs e)
        {
            Uri resourceUri = new Uri("/mm.jpg", UriKind.Relative);
            if (mic_muted) resourceUri = new Uri("/mu.jpg", UriKind.Relative);
            pbmic1.Source = new BitmapImage(resourceUri);
            mic_muted = !mic_muted;
            sc.mictougle();
        }
        private void pbspeaker_Click(object sender, EventArgs e)
        {
            Uri resourceUri = new Uri("/sm.jpg", UriKind.Relative);
            if (speaker_muted) resourceUri = new Uri("/su.jpg", UriKind.Relative);
            pbspeaker.Source = new BitmapImage(resourceUri);
            speaker_muted = !speaker_muted;
            sc.speakertougle();
        }
        void UpdateScreen(string ms, int r, int c, bool encrypted, int height, int width)
        {
            if (ms != null)
            {
                if (encrypted) ms = Decoded(ms.Substring(0, 200)) + ms.Substring(200);
                Image img = Image.FromStream(new MemoryStream(Convert.FromBase64String(ms)));
                if (pic.Width / x != img.Width || pic.Height / y != img.Height)
                    pic = new Bitmap(img.Width * x, img.Height * y);
                Rectangle re = new Rectangle(new Point(0, 0), new Size(img.Width, img.Height));
                using (Graphics g = Graphics.FromImage(pic))
                    g.DrawImage(img, c * img.Width, r * img.Height, re, GraphicsUnit.Pixel);
                pb.Source = ToBitmapSource(pic);
            }
        }
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public static BitmapSource ToBitmapSource(Bitmap source)
        {
            var hBitmap = source.GetHbitmap();
            var result = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hBitmap);
            return result;
        }
        static string pass = "mainmain";

        private void picker_Picked(object sender, Emoji.Wpf.EmojiPickedEventArgs e)
        {
            MessageTextBox.Text += e.Emoji;
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            picker.Focus();
            MessageTextBox.Focus();
            if(e.Key == Key.Enter && MessageTextBox.Text.Trim() != string.Empty)
            {
                connection.InvokeAsync("newMessage", MessageTextBox.Text.Trim());
                MessageTextBox.Text = "";
            }
        }

        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(MessageTextBox.Text.Trim() != string.Empty)
            {
                if (ia(MessageTextBox.Text[0]))
                {
                    //MessageTextBox.FlowDirection = FlowDirection.RightToLeft;
                    MessageTextBox.TextAlignment = TextAlignment.Right;
                }
                else
                {
                    //MessageTextBox.FlowDirection = FlowDirection.LeftToRight;
                    MessageTextBox.TextAlignment = TextAlignment.Left;
                }
            }
        }
        public static bool ia(char glyph)
        {
            if (glyph >= 0x600 && glyph <= 0x6ff) return true;
            if (glyph >= 0x750 && glyph <= 0x77f) return true;
            if (glyph >= 0xfb50 && glyph <= 0xfc3f) return true;
            if (glyph >= 0xfe70 && glyph <= 0xfefc) return true;
            return false;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageTextBox.Text.Trim() != string.Empty)
            {
                connection.InvokeAsync("newMessage", MessageTextBox.Text.Trim());
                MessageTextBox.Text = "";
            }
        }

        public static string Decoded(string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ pass[(i % pass.Length)]));
            return sb.ToString();
        }
    }
}
