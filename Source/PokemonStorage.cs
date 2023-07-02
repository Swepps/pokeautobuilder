using System.Collections.ObjectModel;

namespace pokeAutoBuilder.Source
{
    public class PokemonStorage : List<SmartPokemon>
    {
        public PokemonStorage() { }

        // used when getting cached pokemon storage list
        public PokemonStorage(List<SmartPokemon> pokemonList) 
        { 
        }

        public PokemonTeam GetRandomTeam(PokemonTeam? lockedMembers = null)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= PokemonTeam.MaxTeamSize || Count < PokemonTeam.MaxTeamSize)
            {
                // they're all locked!
                return lockedMembers;
            }

            // generate the random members and stick into an array for later
            Random rand = new Random();
            int numOfRandMembers = PokemonTeam.MaxTeamSize - lockedMembers.CountPokemon();
            List<SmartPokemon> randomMembers = this.OrderBy(p => rand.Next()).Take(numOfRandMembers).ToList();

            // create a new team using lockedMembers and random members
            PokemonTeam newTeam = new PokemonTeam();
            int randIdx = 0;
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                if (lockedMembers[i] != null)
                {
                    newTeam[i] = lockedMembers[i];
                }
                else if (randIdx < randomMembers.Count)
                {
                    newTeam[i] = randomMembers[randIdx];
                    randIdx++;
                }
            }

            return newTeam;
        }

        public SmartPokemon GetRandomPokemon()
        {
            Random rand = new Random();
            SmartPokemon randPokemon = this[rand.Next(0, Count)];
            return randPokemon;
        }

        public List<SmartPokemonSerializable> GetSerializableList()
        {
            List<SmartPokemonSerializable> smartPokemonSerializables = new List<SmartPokemonSerializable>();
            foreach (SmartPokemon sp in this)
            {
                smartPokemonSerializables.Add(new SmartPokemonSerializable(sp));
            }

            return smartPokemonSerializables;
        }
    }
}
