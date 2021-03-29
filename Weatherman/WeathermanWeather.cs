using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    //this is fucked and I do not like it but I have no choice
    class WeathermanWeather
    {
        public byte Id;
        public bool Selected;
        public bool IsNormal;
        public WeathermanWeather(byte id, bool selected, bool normal)
        {
            this.Id = id;
            this.Selected = selected;
            this.IsNormal = normal;
        }
    }
}
