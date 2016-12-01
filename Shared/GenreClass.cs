namespace WhoWasIn.Shared
{
    public class GenresRoot
    {
        public Genre[] genres { get; set; }
    }

    public class Genre
    {
        public int id { get; set; }
        public string name { get; set; }
    }

}
