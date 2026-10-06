namespace Prison.Core
{
    public class Artist : Prisoner, IWork, IStudy
    {
        public Artist(string name) : base(name) { }

        public string Work()
        {
            Energy -= 15;
            return string.Format(Resources.ArtistWork, Name);
        }

        public string Study()
        {
            Energy -= 15;
            return string.Format(Resources.ArtistStudy, Name);
        }

        public override string CrazyAction()
        {
            if (Energy < 30)
            {
                return string.Format(Resources.ArtistTired, Name);
            }
            Energy -= 30;
            return string.Format(Resources.ArtistCrazy, Name);
        }
    }
}
