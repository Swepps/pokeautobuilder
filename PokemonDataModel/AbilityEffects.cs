using Utility;

namespace PokemonDataModel
{
    // Maps ability names to their effect on a Pokemon's already-computed type-effectiveness
    // multipliers. Extracted out of SmartPokemon.UpdateMultipliers's ability switch so adding a
    // new ability's effect doesn't mean editing one ever-growing method.
    public static class AbilityEffects
    {
        private static void ReduceSuperEffectiveByQuarter(Multipliers multipliers)
        {
            foreach (KeyValuePair<string, double> kvp in multipliers.Defense)
            {
                if (kvp.Value >= 2.0)
                {
                    multipliers.Defense[kvp.Key] = kvp.Value * 0.75;
                }
            }
        }

        private static void HalveDamageFrom(Multipliers multipliers, string type)
        {
            if (multipliers.Defense.ContainsKey(type))
            {
                multipliers.Defense[type] = multipliers.Defense[type] * 0.5;
            }
            else
            {
                multipliers.Defense[type] = 0.5;
            }
        }

        private static readonly Dictionary<string, Action<Multipliers>> Effects =
            new()
            {
                // Dry Skin makes a pokemon immune to water attacks
                ["dry-skin"] = m => m.Defense["water"] = 0,

                // Earth Eater makes a pokemon immune to ground attacks
                ["earth-eater"] = m => m.Defense["ground"] = 0,

                // Filter reduces super effective attacks by 25%
                ["filter"] = ReduceSuperEffectiveByQuarter,

                // Flash Fire makes a pokemon immune to fire attacks
                ["flash-fire"] = m => m.Defense["fire"] = 0,

                // Fluffy makes a pokemon take double damage from fire attacks
                ["fluffy"] = m =>
                    m.Defense["fire"] = m.Defense.ContainsKey("fire") ? m.Defense["fire"] * 2.0 : 2.0,

                // heatproof makes a pokemon take half damage from fire attacks
                ["heatproof"] = m => HalveDamageFrom(m, "fire"),

                // Levitate makes a pokemon immune to ground attacks
                ["levitate"] = m => m.Defense["ground"] = 0,

                // Lightning rod makes a pokemon immune to electric attacks
                ["lightning-rod"] = m => m.Defense["electric"] = 0,

                // Motor Drive makes a pokemon immune to electric attacks
                ["motor-drive"] = m => m.Defense["electric"] = 0,

                // Prism Armor reduces super effective attacks by 25%
                ["prism-armor"] = ReduceSuperEffectiveByQuarter,

                // purifying salt makes a pokemon take half damage from ghost attacks
                ["purifying-salt"] = m => HalveDamageFrom(m, "ghost"),

                // Sap Sipper makes a pokemon immune to grass attacks
                ["sap-sipper"] = m => m.Defense["grass"] = 0,

                // Solid Rock reduces super effective attacks by 25%
                ["solid-rock"] = ReduceSuperEffectiveByQuarter,

                // Storm drain makes a pokemon immune to water attacks
                ["storm-drain"] = m => m.Defense["water"] = 0,

                // thick fat makes a pokemon take half damage from fire and ice attacks
                ["thick-fat"] = m =>
                {
                    HalveDamageFrom(m, "fire");
                    HalveDamageFrom(m, "ice");
                },

                // Volt absorb makes a pokemon immune to electric attacks
                ["volt-absorb"] = m => m.Defense["electric"] = 0,

                // water absorb makes a pokemon immune to water attacks
                ["water-absorb"] = m => m.Defense["water"] = 0,

                // water bubble makes a pokemon take half damage from fire attacks
                ["water-bubble"] = m => HalveDamageFrom(m, "fire"),

                // Well-baked body makes a pokemon immune to fire attacks
                ["well-baked-body"] = m => m.Defense["fire"] = 0,

                // wonder guard makes a pokemon immune to all types which aren't super-effective
                ["wonder-guard"] = m =>
                {
                    foreach (string type in Globals.AllTypes)
                    {
                        if (m.Defense.ContainsKey(type))
                        {
                            if (m.Defense[type] <= 1.0)
                                m.Defense[type] = 0;
                        }
                        else
                        {
                            m.Defense[type] = 0;
                        }
                    }
                },
            };

        public static void Apply(string abilityName, Multipliers multipliers)
        {
            if (Effects.TryGetValue(abilityName, out Action<Multipliers>? effect))
            {
                effect(multipliers);
            }
        }
    }
}
