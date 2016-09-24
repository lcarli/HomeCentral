using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentral.Library
{
    public class Sensor
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string GPIO { get; set; }

        [DataMember]
        public string  ImagePath { get; set; }
    }
}
