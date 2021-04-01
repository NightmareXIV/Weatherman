using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weatherman
{
    public class Song
    {
        public int Id;
        public string Name;
        public Song(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        override public string ToString()
        {
            return this.Id + " / " + this.Name;
        }
    }
}
