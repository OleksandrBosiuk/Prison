namespace Prison.Core
{
    public class Mechanic : Prisoner, IWork
    {
        public Mechanic(string name) : base(name) { }

        public string Work()
        {
            Energy -= 15;
            return $"{Name} repairs a vehicle in the prison garage.";
        }

        public override string CrazyAction()
        {
            Energy -= 35;
            return $"{Name} turns the bunk into an imaginary racing car.";
        }
    }
}
