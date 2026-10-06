namespace Prison.Core
{
    public class Rookie : Prisoner, IWork
    {
        public Rookie(string name) : base(name) { }

        public string Work()
        {
            Energy -= 20;
            return string.Format(Resources.RookieWork, Name);
        }

        public override string CrazyAction()
        {
            Energy -= 50;
            return string.Format(Resources.RookieCrazy, Name);
        }
    }
}
