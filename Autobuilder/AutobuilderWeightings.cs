using Utility;

namespace Autobuilder
{
    // contains doubles that represent the importance of each parameter for team generation
    // all doubles are between 0 and 1.0
    public class AutobuilderWeightings(
        Dictionary<string, bool> typeWeightings,
        double resistanceAll = 1.0,
        double resistanceBalance = 1.0,
        double resistanceAmount = 1.0,
        double weaknessAmount = 1.0,
        double weaknessBalance = 1.0,
        double stabAll = 1.0,
        double stabBalance = 1.0,
        double stabAmount = 1.0,
        double moveSetAll = 1.0,
        double moveSetBalance = 1.0,
        double moveSetAmount = 1.0,
        double coverageOnOffensive = 1.0,
        double resistancesOnDefensive = 1.0,
        double baseStatTotal = 1.0,
        double baseStatHp = 1.0,
        double baseStatAtt = 1.0,
        double baseStatDef = 1.0,
        double baseStatSpAtt = 1.0,
        double baseStatSpDef = 1.0,
        double baseStatSpe = 1.0,
        bool allowMultipleMegas = false,
        bool allowMultipleGmax = false
    )
    {
        public Dictionary<string, bool> Types = typeWeightings;

        // resistances
        public double ResistanceAll { get; set; } = resistanceAll;
        public double ResistanceBalance { get; set; } = resistanceBalance;
        public double ResistanceAmount { get; set; } = resistanceAmount;

        // weaknesses
        public double WeaknessAmount { get; set; } = weaknessAmount;
        public double WeaknessBalance { get; set; } = weaknessBalance;

        // STAB
        public double StabAll { get; set; } = stabAll;
        public double StabBalance { get; set; } = stabBalance;
        public double StabAmount { get; set; } = stabAmount;

        // Moves
        public double MoveSetAll { get; set; } = moveSetAll;
        public double MoveSetBalance { get; set; } = moveSetBalance;
        public double MoveSetAmount { get; set; } = moveSetAmount;

        // misc weightings
        public double CoverageOnOffensive { get; set; } = coverageOnOffensive;
        public double ResistancesOnDefensive { get; set; } = resistancesOnDefensive;

        // base stats
        public double BaseStatTotal { get; set; } = baseStatTotal; // scales other stat weightings
        public double BaseStatHp { get; set; } = baseStatHp;
        public double BaseStatAtt { get; set; } = baseStatAtt;
        public double BaseStatDef { get; set; } = baseStatDef;
        public double BaseStatSpAtt { get; set; } = baseStatSpAtt;
        public double BaseStatSpDef { get; set; } = baseStatSpDef;
        public double BaseStatSpe { get; set; } = baseStatSpe;

        // generation settings
        public bool AllowMultipleMegas { get; set; } = allowMultipleMegas;
        public bool AllowMultipleGmax { get; set; } = allowMultipleGmax;

        public AutobuilderWeightings()
            : this(MakeDefaultTypeWeightings()) { }

        // copy constructor
        public AutobuilderWeightings(AutobuilderWeightings clone)
            : this(
                clone.Types,
                resistanceAll: clone.ResistanceAll,
                resistanceBalance: clone.ResistanceBalance,
                resistanceAmount: clone.ResistanceAmount,
                weaknessBalance: clone.WeaknessBalance,
                weaknessAmount: clone.WeaknessAmount,
                stabAll: clone.StabAll,
                stabBalance: clone.StabBalance,
                stabAmount: clone.StabAmount,
                moveSetAll: clone.MoveSetAll,
                moveSetBalance: clone.MoveSetBalance,
                moveSetAmount: clone.MoveSetAmount,
                coverageOnOffensive: clone.CoverageOnOffensive,
                resistancesOnDefensive: clone.ResistancesOnDefensive,
                baseStatTotal: clone.BaseStatTotal,
                baseStatHp: clone.BaseStatHp,
                baseStatAtt: clone.BaseStatAtt,
                baseStatDef: clone.BaseStatDef,
                baseStatSpAtt: clone.BaseStatSpAtt,
                baseStatSpDef: clone.BaseStatSpDef,
                baseStatSpe: clone.BaseStatSpe
            ) { }

        public double SumWeightings()
        {
            double sum = 0;

            sum += ResistanceAll;
            sum += ResistanceBalance;
            sum += ResistanceAmount;

            sum += WeaknessBalance;
            sum += WeaknessAmount;

            sum += StabAll;
            sum += StabBalance;
            sum += StabAmount;

            sum += MoveSetAll;
            sum += MoveSetBalance;
            sum += MoveSetAmount;

            sum += CoverageOnOffensive;
            sum += ResistancesOnDefensive;

            sum += BaseStatHp;
            sum += BaseStatAtt;
            sum += BaseStatDef;
            sum += BaseStatSpAtt;
            sum += BaseStatSpDef;
            sum += BaseStatSpe;

            return sum;
        }

        public static Dictionary<string, bool> MakeDefaultTypeWeightings()
        {
            Dictionary<string, bool> typeWeightings = [];
            foreach (string type in Globals.AllTypes)
            {
                typeWeightings[type] = true;
            }
            return typeWeightings;
        }
    }
}
