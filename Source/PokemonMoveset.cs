using PokeApiNet;

namespace pokeAutoBuilder.Source
{
    public class PokemonMoveset : List<Move?>
    {
        public static readonly int MaxMovesetSize = 4;

        public bool IsEmpty
        {
            get
            {
                foreach (Move? m in this)
                {
                    if (m != null)
                        return false;
                }
                return true;
            }
        }

        public PokemonMoveset()
        {
            // fill moveset with null moves
            for (int i = 0; i < MaxMovesetSize; i++)
            {
                Add(null);
            }
        }

        public int CountMoves()
        {
            int count = 0;
            for (int i = 0; i < MaxMovesetSize; i++)
            {
                Move? m = this[i];
                if (m != null) count++;
            }
            return count;
        }
    }
}
