namespace Weatherman
{
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
