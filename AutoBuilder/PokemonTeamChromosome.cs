using GeneticSharp;
using PokemonDataModel;
using System.Diagnostics;

namespace AutoBuilder
{
    // basically this is the ChromosomeBase class but I needed to override some functions
    // in order for the locking of team members to work so it now implements everything


    [Serializable]
    [DebuggerDisplay("Fitness:{Fitness}, Genes:{Length}")]
    public class PokemonTeamChromosome : IChromosome, IComparable<IChromosome>
    {
        // chromosome vars
        private Gene[] m_genes;

        private int m_length;

        public double? Fitness { get; set; }

        public AutoBuilderWeightings? WeightingScores;

        public int Length => m_length;

        // pokemon chromosome vars
        static Random Random = new Random();

        private readonly PokemonBox _box;
        private readonly PokemonTeam _lockedMembers;

        public double Score { get; internal set; }

        public PokemonTeamChromosome(PokemonBox box, PokemonTeam lockedMembers)
        {
            int length = PokemonTeam.MaxTeamSize;
            ValidateLength(length);
            m_length = length;
            m_genes = new Gene[length];
            _box = box;
            _lockedMembers = lockedMembers;

            PokemonTeam randomTeam = _box.GetRandomTeam();

            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                ReplaceGene(i, new Gene(randomTeam.Pokemon[i]));
            }
        }

        public Gene GenerateGene(int geneIndex)
        {
            // we don't want to replace locked members
            if (_lockedMembers.Pokemon[geneIndex] is not null)
            {
                return GetGene(geneIndex);
            }

            return new Gene(_box.GetRandomPokemon());
        }

        // modified from normal ChromosomeBase function to not replace locked members
        public void ReplaceGenes(int startIndex, Gene[] genes)
        {
            ExceptionHelper.ThrowIfNull("genes", genes);
            if (genes.Length != 0)
            {
                if (startIndex < 0 || startIndex >= m_length)
                {
                    throw new ArgumentOutOfRangeException("startIndex", "There is no Gene on index {0} to be replaced.".With(startIndex));
                }

                // replace genes with new genes if they're not locked
                for (int i = startIndex; i < Math.Min(genes.Length, m_length); i++)
                {
                    if (_lockedMembers.Pokemon[i] is null)
                    {
                        m_genes[i] = new Gene(genes[i].Value);
                    }
                }
                Fitness = null;
            }
        }

        public IChromosome CreateNew()
        {
            return new PokemonTeamChromosome(_box, _lockedMembers);
        }

        public IChromosome Clone()
        {
            var clone = new PokemonTeamChromosome(_box, _lockedMembers);
            var genes = GetGenes();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                clone.ReplaceGene(i, new Gene((SmartPokemon?)genes[i].Value));
            }
            clone!.Score = Score;

            return clone;
        }

        public PokemonTeam GetTeam()
        {
            var genes = GetGenes();

            // put into a new team then sort it
            PokemonTeam team = new PokemonTeam();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                team.Pokemon[i] = genes[i].Value as SmartPokemon;
            }

            // don't want to sort anymore because it messes up locked pokemon
            //team.SortById();

            return team;
        }

        // IChromosome functions
        public static bool operator ==(PokemonTeamChromosome first, PokemonTeamChromosome second)
        {
            if ((object)first == second)
            {
                return true;
            }

            if ((object)first == null || (object)second == null)
            {
                return false;
            }

            return first.CompareTo(second) == 0;
        }

        public static bool operator !=(PokemonTeamChromosome first, PokemonTeamChromosome second)
        {
            return !(first == second);
        }

        public static bool operator <(PokemonTeamChromosome first, PokemonTeamChromosome second)
        {
            if ((object)first == second)
            {
                return false;
            }

            if ((object)first == null)
            {
                return true;
            }

            if ((object)second == null)
            {
                return false;
            }

            return first.CompareTo(second) < 0;
        }

        public static bool operator >(PokemonTeamChromosome first, PokemonTeamChromosome second)
        {
            if (!(first == second))
            {
                return !(first < second);
            }

            return false;
        }

        public void ReplaceGene(int index, Gene gene)
        {
            if (index < 0 || index >= m_length)
            {
                throw new ArgumentOutOfRangeException("index", "There is no Gene on index {0} to be replaced.".With(index));
            }

            // don't want to replace locked members
            if (_lockedMembers.Pokemon[index] is null)
                m_genes[index] = gene;
            else
                m_genes[index] = new Gene(_lockedMembers.Pokemon[index]);

            Fitness = null;
        }

        public void Resize(int newLength)
        {
            ValidateLength(newLength);
            Array.Resize(ref m_genes, newLength);
            m_length = newLength;
        }

        public Gene GetGene(int index)
        {
            return m_genes[index];
        }

        public Gene[] GetGenes()
        {
            return m_genes;
        }

        public int CompareTo(IChromosome? other)
        {
            if (other is null)
            {
                return -1;
            }

            double? fitness = other.Fitness;
            if (Fitness == fitness)
            {
                return 0;
            }

            if (!(Fitness > fitness))
            {
                return -1;
            }

            return 1;
        }

        public override bool Equals(object? obj)
        {
            PokemonTeamChromosome? chromosome = obj as PokemonTeamChromosome;
            if (chromosome is null)
            {
                return false;
            }

            return CompareTo(chromosome) == 0;
        }

        public override int GetHashCode()
        {
            return Fitness.GetHashCode();
        }

        protected virtual void CreateGene(int index)
        {
            ReplaceGene(index, GenerateGene(index));
        }

        protected virtual void CreateGenes()
        {
            for (int i = 0; i < Length; i++)
            {
                ReplaceGene(i, GenerateGene(i));
            }
        }

        private static void ValidateLength(int length)
        {
            if (length < 2)
            {
                throw new ArgumentException("The minimum length for a chromosome is 2 genes.", "length");
            }
        }
    }
}
