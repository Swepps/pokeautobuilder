using PokeApiNet;
using System.Text.Json.Serialization;
using Type = PokeApiNet.Type;

namespace PokemonDataModel
{
    public class PokemonMoveset
    {
        [JsonIgnore]
        public static readonly int MaxMovesetSize = 4;

		[JsonIgnore]
		private bool _multipliersNeedUpdating = true;

		[JsonIgnore]
        public Dictionary<string, double> AttackMultipliers = new Dictionary<string, double>();

		[JsonIgnore]
        private List<Move?> _moves = new();

		[JsonPropertyName("moves")]
		public List<string?> MoveNames { get; set; } = new();

		[JsonIgnore]
        public bool IsEmpty
        {
            get
            {
                foreach (Move? m in _moves)
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
                MoveNames.Add(null);
            }
        }

		public List<Move?> GetMoves()
		{
			return _moves.Take(MaxMovesetSize).ToList();
		}
        public List<string?> GetMoveNames()
        {
            return MoveNames.Take(MaxMovesetSize).ToList();
        }

        public int CountMoves()
        {
            int count = 0;
            for (int i = 0; i < MaxMovesetSize; i++)
            {
                Move? m = _moves[i];
                if (m != null) count++;
            }
            return count;
        }

        public async Task SetAt(int index, Move? move)
        {
			if (index >= 0 && index < MaxMovesetSize)
			{
                _moves[index] = move;
                MoveNames[index] = move?.Name;
				_multipliersNeedUpdating = true;
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

        public async Task UpdateAttackMultipliers()
        {
			if (!_multipliersNeedUpdating)
				return;

			_multipliersNeedUpdating = false;

			// load in move details
			if (MoveNames.Count > _moves.Count)
			{
				_moves.Clear();
				foreach (string? name in MoveNames) 
				{
					if (name is null)
					{
						_moves.Add(null);
						continue;
					}

					_moves.Add(await PokeApiService.Instance!.GetMoveAsync(name));
				}
			}

            AttackMultipliers.Clear();

			foreach (Move? move in _moves)
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
