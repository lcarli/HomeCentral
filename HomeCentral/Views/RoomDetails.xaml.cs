﻿using HomeCentral.Library;
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
    public sealed partial class RoomDetails : Page
    {
        private Room r;

        public RoomDetails()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            r = e.Parameter as Room;
            Title.Text = r.Name;
            UpdateList();
        }

        private void UpdateList()
        {
            noneText.Visibility = Visibility.Collapsed;

            if (r.Devices.Count() > 0)
            {
                foreach (var device in r.Devices)
                {
                    listDevices.Items.Add(device);
                }
            }
            else
            {
                noneText.Visibility = Visibility.Visible;
            }

        }

        private void listDevices_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.Frame.Navigate(typeof(DeviceDetails), e.ClickedItem);
        }
    }
}
