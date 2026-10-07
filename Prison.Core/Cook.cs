namespace Prison.Core
{
    public class Cook : Prisoner, IWork
    {
        public Cook(string name) : base(name)
        {
        }

        public string Work()
        {
            Energy -= 20;
            return $"{Name} prepared prison stew. Energy: {Energy}";
        }

        public override string CrazyAction()
        {
            Energy -= 40;
            return $"{Name} threw porridge at the guards! Energy: {Energy}";
        }
    }
}