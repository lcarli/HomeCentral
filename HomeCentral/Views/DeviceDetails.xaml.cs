using HomeCentral.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
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
    public sealed partial class DeviceDetails : Page
    {
        private Device d;

        public DeviceDetails()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            d = e.Parameter as Device;
            Title.Text = d.Name;
            UpdateList();
        }

        private void UpdateList()
        {
            noneText.Visibility = Visibility.Collapsed;
            if (d.sensors.Count() > 0)
            {
                foreach (var sensor in d.sensors)
                {
                    listSensors.Items.Add(sensor);
                }
            }
            else
            {
                noneText.Visibility = Visibility.Visible;
            }
        }

        private void listSensors_ItemClick(object sender, ItemClickEventArgs e)
        {

        }
    }
}
