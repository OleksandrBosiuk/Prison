namespace Prison.Core
{
    public class Philosopher : Prisoner, IStudy
    {
        public Philosopher(string name) : base(name) { }

        public string Study()
        {
            Energy -= 10;
            return string.Format(Resources.PhilStudy, Name);
        }

        public override string CrazyAction()
        {
            Energy -= 25;
            return string.Format(Resources.PhilCrazy, Name);
        }
    }
}