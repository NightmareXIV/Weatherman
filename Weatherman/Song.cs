namespace Weatherman
{
    public class Song
    {
        public int Id;
        public string Name;
        public Song(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Id + " / " + Name;
        }
    }
}
