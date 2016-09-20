using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentral.Library
{
    [DataContract]
    public class Device
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        //[DataMember]
        //public string ImagePath { get; set; }
    }
}
