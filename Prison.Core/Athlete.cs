namespace Prison.Core
{
    public class Athlete : Prisoner, IWork
    {
        public Athlete(string name) : base(name) { }

        public string Work()
        {
            Energy -= 20;
            return $"{Name} trains other inmates in the prison gym.";
        }

        public override string CrazyAction()
        {
            Energy -= 40;
            return $"{Name} starts a marathon around the cell.";
        }
    }
}
