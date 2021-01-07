using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ServerManagerTool.Lib
{
    [DataContract]
    public class StatsMultipliers
    {
        [DataMember]
        public float Player
        {
            get;
            set;
        }

        [DataMember]
        public float WildDino
        {
            get;
            set;
        }

        [DataMember]
        public float TamedDino
        {
            get;
            set;
        }
    }
}
