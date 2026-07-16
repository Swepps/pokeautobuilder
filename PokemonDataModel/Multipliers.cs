namespace PokemonDataModel
{
    // A Pokemon's type-effectiveness multipliers: Defense = how much damage it takes from each
    // attacking type, Attack = how effective its own types are against each defending type.
    public class Multipliers
    {
        public Dictionary<string, double> Defense;
        public Dictionary<string, double> Attack;

        public Multipliers()
        {
            Defense = new Dictionary<string, double>();
            Attack = new Dictionary<string, double>();
        }

        public void Clear()
        {
            Defense.Clear();
            Attack.Clear();
        }
    }
}
