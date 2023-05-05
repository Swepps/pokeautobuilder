using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PokemonAutoTeamBuilder
{
    internal class PokemonTypes
    {
        public enum PokemonType
        {
            Normal,
            Fire,
            Water,
            Electric,
            Grass,
            Ice,
            Fighting,
            Poison,
            Ground,
            Flying,
            Psychic,
            Bug,
            Rock,
            Ghost,
            Dragon,
            Dark,
            Steel,
            Fairy,
            None
        }

        private static double IMMU = 0.0;
        private static double WEAK = 0.5;
        private static double NORM = 1.0;
        private static double SUPE = 2.0;

        private static double[,] typeEffectivenessTable = new double[,]
        {   /*      NOR  FIR  WAT  ELE  GRA  ICE  FIG  POI  GRO  FLY  PSY  BUG  ROC  GHO  DRA  DAR  STE  FAI */   
            /*NOR*/{NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,WEAK,IMMU,NORM,NORM,WEAK,NORM},
            /*FIR*/{NORM,WEAK,WEAK,NORM,SUPE,SUPE,NORM,NORM,NORM,NORM,NORM,NORM,WEAK,NORM,WEAK,NORM,SUPE,NORM},
            /*WAT*/{NORM,SUPE,WEAK,NORM,WEAK,NORM,NORM,NORM,SUPE,NORM,NORM,NORM,SUPE,NORM,WEAK,NORM,NORM,NORM},
            /*ELE*/{NORM,NORM,SUPE,WEAK,WEAK,NORM,NORM,NORM,IMMU,SUPE,NORM,NORM,NORM,NORM,WEAK,NORM,NORM,NORM},
            /*GRA*/{NORM,WEAK,SUPE,NORM,WEAK,NORM,NORM,WEAK,SUPE,WEAK,NORM,WEAK,SUPE,NORM,WEAK,NORM,WEAK,NORM},
            /*ICE*/{NORM,WEAK,WEAK,NORM,SUPE,WEAK,NORM,NORM,SUPE,SUPE,NORM,NORM,NORM,NORM,SUPE,NORM,WEAK,NORM},
            /*FIG*/{SUPE,NORM,NORM,NORM,NORM,SUPE,NORM,WEAK,NORM,WEAK,WEAK,WEAK,SUPE,IMMU,NORM,SUPE,SUPE,WEAK},
            /*POI*/{NORM,NORM,NORM,NORM,SUPE,NORM,NORM,WEAK,WEAK,NORM,NORM,NORM,WEAK,WEAK,NORM,NORM,IMMU,SUPE},
            /*GRO*/{NORM,SUPE,NORM,SUPE,WEAK,NORM,NORM,SUPE,NORM,IMMU,NORM,WEAK,SUPE,NORM,NORM,NORM,SUPE,NORM},
            /*FLY*/{NORM,NORM,NORM,WEAK,NORM,NORM,SUPE,NORM,NORM,NORM,NORM,SUPE,WEAK,NORM,NORM,NORM,WEAK,NORM},
            /*PSY*/{NORM,NORM,NORM,NORM,NORM,NORM,SUPE,SUPE,NORM,NORM,WEAK,NORM,NORM,NORM,NORM,IMMU,WEAK,NORM},
            /*BUG*/{NORM,NORM,NORM,NORM,SUPE,NORM,WEAK,WEAK,NORM,WEAK,SUPE,NORM,NORM,WEAK,NORM,SUPE,WEAK,WEAK},
            /*ROC*/{NORM,SUPE,NORM,NORM,NORM,SUPE,WEAK,NORM,WEAK,SUPE,NORM,SUPE,NORM,NORM,NORM,NORM,WEAK,NORM},
            /*GHO*/{IMMU,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,SUPE,NORM,NORM,SUPE,NORM,WEAK,NORM,NORM},
            /*DRA*/{NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,NORM,SUPE,NORM,WEAK,IMMU},
            /*DAR*/{NORM,NORM,NORM,NORM,NORM,NORM,WEAK,NORM,NORM,NORM,SUPE,NORM,NORM,SUPE,NORM,WEAK,NORM,NORM},
            /*STE*/{NORM,WEAK,WEAK,WEAK,NORM,SUPE,NORM,NORM,NORM,NORM,NORM,NORM,SUPE,NORM,NORM,NORM,WEAK,SUPE},
            /*FAI*/{NORM,WEAK,NORM,NORM,NORM,NORM,SUPE,WEAK,NORM,NORM,NORM,NORM,NORM,NORM,SUPE,SUPE,WEAK,NORM}
        };

        public static double GetEffectiveness(PokemonType attack, PokemonType defense)
        {
            return typeEffectivenessTable[(int)attack, (int)defense];
        }
    }
}
