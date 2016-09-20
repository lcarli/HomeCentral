using HomeCentral.Library;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TouchPanels.Devices;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HomeCentral.Controls;
using Newtonsoft.Json;

namespace HomeCentral.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        //variaveis do Touch
        const string CalibrationFilename = "TSC2046";
        private Tsc2046 tsc2046;
        private TouchPanels.TouchProcessor processor;
        private Point lastPosition = new Point(double.NaN, double.NaN);

        //variaveis I2C 
        private static I2cDevice Device;
        private Timer periodicTimer;

        //variaveis gerais
        public static House myHouse;

        //IoTHub
        static string deviceKey;

        public Home()
        {
            this.InitializeComponent();

            //Inicializa serviços IoTHub, Comunicação, etc.
            initcomunica();
            InitTouch();
            InitHouse();
            InitIoTHub();

        }

        #region [ Inicializadores ]
        private async void InitHouse()
        {
            //lista de rooms
            myHouse = await House.LoadHome();

            if (myHouse.isFirstTime)
            {
                myHouse.Rooms.Clear();
                #region [ adicionando rooms na mao apenas a primeira vez]
                Room r2 = new Room();
                r2.Name = "Quarto Casal";
                myHouse.Rooms.Add(r2);

                Room r3 = new Room();
                r3.Name = "Quarto Crianças";
                myHouse.Rooms.Add(r3);

                Room r4 = new Room();
                r4.Name = "Banheiro Casal";
                myHouse.Rooms.Add(r4);

                Room r5 = new Room();
                r5.Name = "Banheiro";
                myHouse.Rooms.Add(r5);

                Room r6 = new Room();
                r6.Name = "Cozinha";
                myHouse.Rooms.Add(r6);

                Room r7 = new Room();
                r7.Name = "Área de Serviço";
                myHouse.Rooms.Add(r7);

                Room r8 = new Room();
                r8.Name = "Sala de Jantar";
                myHouse.Rooms.Add(r8);

                Room r9 = new Room();
                r9.Name = "Sala de Estar";
                myHouse.Rooms.Add(r9);

                Room r10 = new Room();
                r10.Name = "Escritório";
                myHouse.Rooms.Add(r10);

                myHouse.isFirstTime = false;

                Library.House.SaveHome(myHouse);
                #endregion
            }
            UpdateList();
        }

        private async void InitIoTHub()
        {
            deviceKey = await IIoTHub.AddDeviceAsync(App.deviceName);
        }

        private void SendDataToCloud()
        {
            var data = new
            {
                //Receber dados e enviar.
            };
            IIoTHub.SendData(data, deviceKey);
        }

        private async void initcomunica()
        {
            var settings = new I2cConnectionSettings(0x40); // Arduino address
            settings.BusSpeed = I2cBusSpeed.StandardMode;
            string aqs = I2cDevice.GetDeviceSelector("I2C1");
            var dis = await DeviceInformation.FindAllAsync(aqs);
            Device = await I2cDevice.FromIdAsync(dis[0].Id, settings);
            periodicTimer = new Timer(this.TimerCallback, null, 0, 100); // Create a timmer
        }
        #endregion

        private void TimerCallback(object state)
        {
            byte[] RegAddrBuf = new byte[] { 0x40 };
            byte[] ReadBuf = new byte[5];
            try
            {
                Device.Read(ReadBuf); // read the data
            }
            catch (Exception f)
            {
                //f.message
            }
            char[] cArray = System.Text.Encoding.UTF8.GetString(ReadBuf, 0, 5).ToCharArray();  // Converte  Byte to Char
            String c = new String(cArray);
            if (c.Substring(0,2) == "99")
            {
                if (!App.ListSource.Contains(c))
                {
                    App.ListSource.Add(c);
                }
                
            }
            // refresh the screen
            var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (myHouse.Rooms.Count > 0)
                {
                    foreach (var Room in myHouse.Rooms)
                    {
                        foreach (var disp in Room.Devices)
                        {
                            if (disp.Id == c.Substring(0,2))
                            {
                                //atualizar o device.
                            }
                        }
                    }
                }
            });
        }

        public static void sendI2C(string data)
        {
            byte[] buff = GetBytes(data);
            var i2cTransferResult = Device.WritePartial(buff);
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                bytes[i] = byte.Parse(str.Substring(i, 1));
            }
            return bytes;
        }

        #region [ DEVICE TOUCH ]
        private async void InitTouch()
        {
            tsc2046 = await TouchPanels.Devices.Tsc2046.GetDefaultAsync();
            try
            {
                await tsc2046.LoadCalibrationAsync(CalibrationFilename);
            }
            catch (System.IO.FileNotFoundException)
            {
                await CalibrateTouch(); //Initiate calibration if we don't have a calibration on file
            }
            catch (System.UnauthorizedAccessException)
            {
                //No access to documents folder
                await new Windows.UI.Popups.MessageDialog("Make sure the application manifest specifies access to the documents folder and declares the file type association for the calibration file.", "Configuration Error").ShowAsync();
                throw;
            }
            //Load up the touch processor and listen for touch events
            processor = new TouchPanels.TouchProcessor(tsc2046);
            processor.PointerDown += Processor_PointerDown;
            processor.PointerMoved += Processor_PointerMoved;
            processor.PointerUp += Processor_PointerUp;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitTouch();
            //Status.Text = "Init Success";
            base.OnNavigatedTo(e);
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (processor != null)
            {
                //Unhooking from all the touch events, will automatically shut down the processor.
                //Remember to do this, or you view could be staying in memory.
                processor.PointerDown -= Processor_PointerDown;
                processor.PointerMoved -= Processor_PointerMoved;
                processor.PointerUp -= Processor_PointerUp;
            }
            base.OnNavigatingFrom(e);
        }

        IScrollProvider currentScrollItem;

        private void Processor_PointerDown(object sender, TouchPanels.PointerEventArgs e)
        {
            WriteStatus(e, "Down");
            currentScrollItem = FindElementsToInvoke(e.Position);
            lastPosition = e.Position;
        }
        private void Processor_PointerMoved(object sender, TouchPanels.PointerEventArgs e)
        {
            WriteStatus(e, "Moved");
            if (currentScrollItem != null)
            {
                double dx = e.Position.X - lastPosition.X;
                double dy = e.Position.Y - lastPosition.Y;
                if (!currentScrollItem.HorizontallyScrollable) dx = 0;
                if (!currentScrollItem.VerticallyScrollable) dy = 0;

                Windows.UI.Xaml.Automation.ScrollAmount h = Windows.UI.Xaml.Automation.ScrollAmount.NoAmount;
                Windows.UI.Xaml.Automation.ScrollAmount v = Windows.UI.Xaml.Automation.ScrollAmount.NoAmount;
                if (dx < 0) h = Windows.UI.Xaml.Automation.ScrollAmount.SmallIncrement;
                else if (dx > 0) h = Windows.UI.Xaml.Automation.ScrollAmount.SmallDecrement;
                if (dy < 0) v = Windows.UI.Xaml.Automation.ScrollAmount.SmallIncrement;
                else if (dy > 0) v = Windows.UI.Xaml.Automation.ScrollAmount.SmallDecrement;
                currentScrollItem.Scroll(h, v);
            }
            lastPosition = e.Position;
        }
        private void Processor_PointerUp(object sender, TouchPanels.PointerEventArgs e)
        {
            WriteStatus(e, "Up");
            currentScrollItem = null;
        }
        private void WriteStatus(TouchPanels.PointerEventArgs args, string type)
        {
            //Status.Text = $"{type}\nPosition: {args.Position.X},{args.Position.Y}\nPressure:{args.Pressure}";
        }

        private async void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            await CalibrateTouch();
        }


        //Method used to create UI and calibrate.
        private bool _isCalibrating = false; //flag used to ignore the touch processor while calibrating
        private async Task CalibrateTouch()
        {
            _isCalibrating = true;
            var calibration = await TouchPanels.UI.LcdCalibrationView.CalibrateScreenAsync(tsc2046);
            _isCalibrating = false;
            tsc2046.SetCalibration(calibration.A, calibration.B, calibration.C, calibration.D, calibration.E, calibration.F);
            try
            {
                await tsc2046.SaveCalibrationAsync(CalibrationFilename);
            }
            catch (Exception ex)
            {
                //Status.Text = ex.Message;
                ContentDialog msgError = new ContentDialog();
                msgError.Content = ex.Message;
                await msgError.ShowAsync();
            }
        }

        private IScrollProvider FindElementsToInvoke(Point screenPosition)
        {
            if (_isCalibrating) return null;

            var elements = VisualTreeHelper.FindElementsInHostCoordinates(new Windows.Foundation.Point(screenPosition.X, screenPosition.Y), this, false);
            //Search for buttons in the visual tree that we can invoke
            //If we can find an element button that implements the 'Invoke' automation pattern (usually buttons), we'll invoke it
            foreach (var e in elements.OfType<FrameworkElement>())
            {
                var element = e;
                AutomationPeer peer = null;
                object pattern = null;
                while (true)
                {
                    peer = FrameworkElementAutomationPeer.FromElement(element);
                    if (peer != null)
                    {
                        pattern = peer.GetPattern(PatternInterface.Invoke);
                        if (pattern != null)
                        {
                            break;
                        }
                        pattern = peer.GetPattern(PatternInterface.Scroll);
                        if (pattern != null)
                        {
                            break;
                        }
                    }
                    var parent = VisualTreeHelper.GetParent(element);
                    if (parent is FrameworkElement)
                        element = parent as FrameworkElement;
                    else
                        break;
                }
                if (pattern != null)
                {
                    var p = pattern as Windows.UI.Xaml.Automation.Provider.IInvokeProvider;
                    p?.Invoke();
                    return pattern as IScrollProvider;
                }
            }
            return null;
        }

        #endregion

        private void UpdateList()
        {
            noneText.Visibility = Visibility.Collapsed;
            if (myHouse.Rooms.Count > 0)
            {
                foreach (var Room in myHouse.Rooms)
                {
                    listRooms.Items.Add(Room);
                }
            }
            else
            {
                noneText.Visibility = Visibility.Visible;
            }
        }

        private void listRooms_ItemClick(object sender, ItemClickEventArgs e)
        {
            //navegar para detalhes do Room passando o room selecionado
            this.Frame.Navigate(typeof(RoomDetails), e.ClickedItem);
        }
    }
}
