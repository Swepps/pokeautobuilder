using PokeApiNet;
using pokeAutoBuilder.Source.Services;

using Type = PokeApiNet.Type;

namespace pokeAutoBuilder.Source
{
    public class PokemonMoveset : List<Move?>
    {
        public static readonly int MaxMovesetSize = 4;
        
        public Dictionary<string, double> AttackMultipliers = new Dictionary<string, double>();

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

        public async Task SetAt(int index, Move? move)
        {
			if (index >= 0 && index < MaxMovesetSize)
			{
				this[index] = move;
                await UpdateAttackMultipliers();
			}
		}

        public bool HasCoverageAgainst(string typeName)
        {
			if (AttackMultipliers.TryGetValue(typeName, out double defEff))
			{
				return defEff >= 2.0;
			}
			else
			{
				return false;
			}
		}

		public double GetBestEffectivenessAgaint(string typeName)
		{
			if (AttackMultipliers.TryGetValue(typeName, out double defEff))
			{
				return defEff;
			}
			else
			{
				return 1.0;
			}
		}

        private async Task UpdateAttackMultipliers()
        {
            AttackMultipliers.Clear();

			foreach (Move? move in this)
            {
                if (move is null || move.DamageClass.Name == "status")
                    continue;

				Type? type = await PokeApiService.Instance!.GetTypeAsync(move.Type.Name);
                if (type is null)
                    continue;

				TypeRelations tr = type.DamageRelations;
				var noDamageTo = tr.NoDamageTo;
				var halfDamageTo = tr.HalfDamageTo;
				var doubleDamageTo = tr.DoubleDamageTo;

				// immune types
				foreach (var namedType in noDamageTo)
				{
					if (AttackMultipliers.ContainsKey(namedType.Name))
					{
						AttackMultipliers[namedType.Name] = Math.Max(AttackMultipliers[namedType.Name], 0);
					}
					else
					{
						AttackMultipliers[namedType.Name] = 0;
					}
				}

				// resistant types
				foreach (var namedType in halfDamageTo)
				{
					if (AttackMultipliers.ContainsKey(namedType.Name))
					{
						AttackMultipliers[namedType.Name] = Math.Max(AttackMultipliers[namedType.Name], 0.5);
					}
					else
					{
						AttackMultipliers[namedType.Name] = 0.5;
					}
				}

				// super effective types
				foreach (var namedType in doubleDamageTo)
				{
					if (AttackMultipliers.ContainsKey(namedType.Name))
					{
						AttackMultipliers[namedType.Name] = Math.Max(AttackMultipliers[namedType.Name], 2.0);
					}
					else
					{
						AttackMultipliers[namedType.Name] = 2.0;
					}
				}
			}
        }
    }
}
