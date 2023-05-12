using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace autoteambuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    using PokemonType = PokemonTypes.PokemonType;

    internal class TypeRow
    {
        public PokemonType Type { get; set; }
        public double Pokemon1Eff { get; set; }
        public double Pokemon2Eff { get; set; }
        public double Pokemon3Eff { get; set; }
        public double Pokemon4Eff { get; set; }
        public double Pokemon5Eff { get; set; }
        public double Pokemon6Eff { get; set; }
        public int Weaknesses { get; set; }
        public int Resists { get; set; }
    }

    public partial class MainWindow : Window
    {
        private Pokedex pokedex;
        private PokemonTeam team = new PokemonTeam();
        public MainWindow()
        {
            InitializeComponent();

            StringReader reader = new StringReader(FileStore.Resource.pokedexAll);
            pokedex = new(reader);

            comboPoke1.ItemsSource = pokedex;
            comboPoke2.ItemsSource = pokedex;
            comboPoke3.ItemsSource = pokedex;
            comboPoke4.ItemsSource = pokedex;
            comboPoke5.ItemsSource = pokedex;
            comboPoke6.ItemsSource = pokedex;

            // setting columns of grid
            DataGridTextColumn typeCol = new DataGridTextColumn();
            typeCol.Header = "Type";
            typeCol.Binding = new Binding("Type");
            typeCol.Width = DataGridLength.SizeToCells;
            gridResults.Columns.Add(typeCol);

            DataGridTextColumn pokemon1Col = new DataGridTextColumn();
            pokemon1Col.Header = "";
            pokemon1Col.Binding = new Binding("Pokemon1Eff");
            gridResults.Columns.Add(pokemon1Col);
            DataGridTextColumn pokemon2Col = new DataGridTextColumn();
            pokemon2Col.Header = "";
            pokemon2Col.Binding = new Binding("Pokemon2Eff");
            gridResults.Columns.Add(pokemon2Col);
            DataGridTextColumn pokemon3Col = new DataGridTextColumn();
            pokemon3Col.Header = "";
            pokemon3Col.Binding = new Binding("Pokemon3Eff");
            gridResults.Columns.Add(pokemon3Col);
            DataGridTextColumn pokemon4Col = new DataGridTextColumn();
            pokemon4Col.Header = "";
            pokemon4Col.Binding = new Binding("Pokemon4Eff");
            gridResults.Columns.Add(pokemon4Col);
            DataGridTextColumn pokemon5Col = new DataGridTextColumn();
            pokemon5Col.Header = "";
            pokemon5Col.Binding = new Binding("Pokemon5Eff");
            gridResults.Columns.Add(pokemon5Col);
            DataGridTextColumn pokemon6Col = new DataGridTextColumn();
            pokemon6Col.Header = "";
            pokemon6Col.Binding = new Binding("Pokemon6Eff");
            gridResults.Columns.Add(pokemon6Col);

            DataGridTextColumn weaknessesCol = new DataGridTextColumn();
            weaknessesCol.Header = "Total Weak";
            weaknessesCol.Binding = new Binding("Weaknesses");
            weaknessesCol.Width = DataGridLength.SizeToHeader;
            gridResults.Columns.Add(weaknessesCol);

            DataGridTextColumn resistsCol = new DataGridTextColumn();
            resistsCol.Header = "Total Resist";
            resistsCol.Binding = new Binding("Resists");
            resistsCol.Width = DataGridLength.SizeToHeader;
            gridResults.Columns.Add(resistsCol);

            PokemonType[] pokemonTypes = PokemonTypes.GetTypeArray();
            foreach (PokemonType t in pokemonTypes)
            {
                gridResults.Items.Add(new TypeRow() { Type = t, Weaknesses = 0, Resists = 0 });
            }
        }

        private void CalculateTeamWeighting()
        {
            double weighting = team.CalculateWeighting();

            labelWeighting.Content = weighting.ToString();
        }

        private void OnChangeTeam(object sender, SelectionChangedEventArgs e)
        {
            team.SetPokemon(0, (Pokemon)comboPoke1.SelectedItem);
            team.SetPokemon(1, (Pokemon)comboPoke2.SelectedItem);
            team.SetPokemon(2, (Pokemon)comboPoke3.SelectedItem);
            team.SetPokemon(3, (Pokemon)comboPoke4.SelectedItem);
            team.SetPokemon(4, (Pokemon)comboPoke5.SelectedItem);
            team.SetPokemon(5, (Pokemon)comboPoke6.SelectedItem);

            CalculateTeamWeighting();

            for (int i = 0; i < 6; i++)
            {
                Pokemon p = team.Pokemon[i];
                DataGridTextColumn col = (DataGridTextColumn)gridResults.Columns[i + 1];
                if (p != null) 
                {
                    col.Header = p.Name;
                }
                else
                {
                    col.Header = "";
                }                
            }            

            foreach (TypeRow row in gridResults.Items) 
            {
                row.Pokemon1Eff = team.Pokemon[0] != null ? team.Pokemon[0].GetEffectiveness(row.Type) : 0.0;
                row.Pokemon2Eff = team.Pokemon[1] != null ? team.Pokemon[1].GetEffectiveness(row.Type) : 0.0;
                row.Pokemon3Eff = team.Pokemon[2] != null ? team.Pokemon[2].GetEffectiveness(row.Type) : 0.0;
                row.Pokemon4Eff = team.Pokemon[3] != null ? team.Pokemon[3].GetEffectiveness(row.Type) : 0.0;
                row.Pokemon5Eff = team.Pokemon[4] != null ? team.Pokemon[4].GetEffectiveness(row.Type) : 0.0;
                row.Pokemon6Eff = team.Pokemon[5] != null ? team.Pokemon[5].GetEffectiveness(row.Type) : 0.0;
                row.Weaknesses = team.CountWeaknesses(row.Type);
                row.Resists = team.CountResistances(row.Type);
            }

            gridResults.Items.Refresh();
        }
    }
}
