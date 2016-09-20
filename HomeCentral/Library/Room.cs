using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentral.Library
{
    [DataContract]
    public class Room
    {
        /// <summary>
        /// Get or set user-friendly name for room
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        //[DataMember]
        //public string ImagePath { get; set; }

        [DataMember]
        public List<Device> Devices;

        public Room()
        {
            Devices = new List<Device>();
        }

        public void AddDevice(Device NewDevice, string name, string id)
        {
            NewDevice.Id = id;
            NewDevice.Name = name;
            Devices.Add(NewDevice);
        }
        public void UpdateDevice(string oldName, string newName)
        {
            foreach (var Device in Devices)
            {
                if (Device.Name == oldName)
                {
                    Device.Name = newName;
                }
            }
        }
    }
}
