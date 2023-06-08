using System.Collections.ObjectModel;

namespace blazorWebAssemblyApp.Source
{
    public class PokemonStorage : ObservableCollection<SmartPokemon>
    {
        public PokemonTeam GetRandomTeam(PokemonTeam? lockedMembers = null)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= 6 || Count < 6)
            {
                // they're all locked!
                return lockedMembers;
            }

            // generate the random members and stick into an array for later
            Random rand = new Random();
            SmartPokemon[] randomMembers = new SmartPokemon[6 - lockedMembers.CountPokemon()];
            for (int i = 0; i < randomMembers.Length; i++)
            {
                SmartPokemon randPokemon = this[rand.Next(0, Count)];
                while (lockedMembers.Contains(randPokemon) || randomMembers.Contains(randPokemon))
                {
                    randPokemon = this[rand.Next(0, Count)];
                }

                randomMembers[i] = randPokemon;
            }

            // create a new team using lockedMembers and random members
            PokemonTeam newTeam = new PokemonTeam();
            int randIdx = 0;
            for (int i = 0; i < 6; i++)
            {
                if (lockedMembers[i] != null)
                {
                    newTeam[i] = lockedMembers[i];
                }
                else if (randIdx < randomMembers.Length)
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
    }
}
