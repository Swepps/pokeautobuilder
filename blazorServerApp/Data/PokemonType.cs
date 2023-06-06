using PokeApiNet;

namespace blazorServerApp.Data
{
    using Type = PokeApiNet.Type;

    // currently there's no difference between PokemonType and Type baseclass, but I might need
    // to add some more functions here in the future
    public class PokemonType : Type
    {
        public PokemonType(Type type)
        {
            Id = type.Id;
            Name = type.Name;
            DamageRelations = type.DamageRelations;
            GameIndices = type.GameIndices;
            Generation = type.Generation;
            MoveDamageClass = type.MoveDamageClass;
            Names = type.Names;
            Pokemon = type.Pokemon;
            Moves = type.Moves;
        }
    }
}
