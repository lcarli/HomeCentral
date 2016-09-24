using HomeCentral.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using TouchPanels.Devices;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HomeCentral.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddElement : Page
    {
        //variaveis do Touch
        const string CalibrationFilename = "TSC2046";
        private Tsc2046 tsc2046;
        private TouchPanels.TouchProcessor processor;
        private Point lastPosition = new Point(double.NaN, double.NaN);
        ObservableCollection<string> ListSource;
        ObservableCollection<string> Rooms;
        string ImagePathSelected;

        //Devices e Rooms
        private string itemSelected;
        private string selectedRoom;
        private House h;

        //Sensors
        private List<Sensor> DeviceSensors;


        public AddElement()
        {
            this.InitializeComponent();
            /* Register the URL textbox with an on-screen keyboard control. Note that currently this
             * keyboard does not support inputting into browser controls 
             */
            SIP_AddressBar.RegisterEditControl(roomName);
            SIP_AddressBar.RegisterHost(this);
            h = Home.myHouse;
            DeviceSensors = new List<Sensor>();
            ListSource = App.ListSource;
            Rooms = App._listRooms;
            listrooms.DataContext = this.DataContext;
            FillListIcons();
        }

        private void FillListIcons()
        {
            foreach (var item in App.spaces)
            {
                ListRoomIcons.Items.Add(new IconsItem() { ImagePath = item.Value, Name = item.Key});
            }
        }

        #region [Buttons AddRoom e AddDevice ]
        private void addRoom(object sender, RoutedEventArgs e)
        {
            DeviceSensors.Clear();
            contentDevice.Visibility = Visibility.Collapsed;
            contentRoom.Visibility = Visibility.Visible;
            deviceDetails.Visibility = Visibility.Collapsed;
        }
        private void addDevice(object sender, RoutedEventArgs e)
        {
            DeviceSensors.Clear();
            contentDevice.Visibility = Visibility.Visible;
            contentRoom.Visibility = Visibility.Collapsed;
            deviceDetails.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region [ Save Room e Save Device ]
        private async void SaveRoom(object sender, RoutedEventArgs e)
        {
            ShowKeyboard(sender, e);
            if (roomName.Text.Length > 0)
            {
                Room room = new Library.Room();
                room.Name = roomName.Text;
                room.ImagePath = ImagePathSelected;
                Home.myHouse.Rooms.Add(room);
                House.SaveHome(Home.myHouse);
                contentRoom.Visibility = Visibility.Collapsed;
                ImagePathSelected = "";
            }
            else
            {
                ContentDialog msg = new ContentDialog();
                msg.Content = "Você precisa inserir um nome antes de salvar.";
                await msg.ShowAsync();
            }
        }

        private async void SaveDevice(object sender, RoutedEventArgs e)
        {
            ShowKeyboard(sender, e);
            if (deviceName.Text.Length > 0)
            {
                Device device = new Library.Device();
                device.Name = deviceName.Text;
                device.Id = deviceID.Text.Remove(0,10);
                device.ImagePath = "ms-appx:///Assets/Icons/microchip.png";
                selectedRoom = listrooms.SelectedItem.ToString();
                device.sensors = DeviceSensors;
                foreach (Room r in h.Rooms)
                {
                    if (r.Name == selectedRoom)
                    {
                        r.Devices.Add(device);
                        break;
                    }
                }
                House.SaveHome(Home.myHouse);
                deviceDetails.Visibility = Visibility.Collapsed;
                ImagePathSelected = "";
            }
            else
            {
                ContentDialog msg = new ContentDialog();
                msg.Content = "Você precisa inserir um nome antes de salvar.";
                await msg.ShowAsync();
            }
        }
        #endregion

        #region [ Touch ]
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

        #region [ Keyboard ]
        private void ShowKeyboard(object sender, RoutedEventArgs e)
        {
            /* Toggle the on screen keyboard visibility */
            if (SIP_AddressBar.Visibility == Visibility.Collapsed)
            {
                SIP_AddressBar.Visibility = Visibility.Visible;
            }
            else
            {
                SIP_AddressBar.Visibility = Visibility.Collapsed;
            }
            roomName.Focus(FocusState.Programmatic);
        }
        #endregion

        private void listDevices_ItemClick(object sender, ItemClickEventArgs e)
        {
            itemSelected = e.ClickedItem.ToString();

            deviceID.Text = "Device ID: " + itemSelected.Split(';')[1]; //Get ID of the device. Always is the second one.

            string[] sensors = itemSelected.Remove(0,6).Split(';');
            Sensor s;
            for (int i = 0; i < sensors.Length; i++)
            {
                if (i % 2 == 0)
                {
                    s = new Sensor();
                    s.GPIO = sensors[i];
                    s.Name = sensors[i+1];
                    s.ImagePath = App.sensors[s.Name];
                    DeviceSensors.Add(s);
                }
            }
            foreach (var sensor in DeviceSensors)
            {
                listSensors.Items.Add(sensor);
            }
        }

        private void listrooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void listDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            contentDevice.Visibility = Visibility.Collapsed;
            deviceDetails.Visibility = Visibility.Visible;
        }

        private void listIcons_ItemClick(object sender, ItemClickEventArgs e)
        {
            var s = e.ClickedItem as IconsItem;
            ImagePathSelected = s.ImagePath;
        }

        private class IconsItem
        {
            public string ImagePath { get; set; }
            public string Name { get; set; }
        }
    }
}
