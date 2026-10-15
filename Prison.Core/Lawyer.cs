namespace Prison.Core
{
    public class Lawyer : Prisoner, IStudy
    {
        public Lawyer(string name) : base(name) { }

        public string Study()
        {
            Energy -= 10;
            return $"{Name} studies law and prepares an appeal.";
        }

        public override string CrazyAction()
        {
            Energy -= 25;
            return $"{Name} holds an improvised trial in the cell.";
        }
    }
}
