using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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

    using Type = PokeApiNet.Type;

    public partial class MainWindow : Window
    {
        private Pokedex pokedex;
        private PokemonTeam team = new PokemonTeam();

        public static List<Type> AllTypes = new List<Type>();

        public MainWindow()
        {
            InitializeComponent();

            // create the pokedex from the text file resource
            // TODO: use PokeApi to build the pokedex
            StringReader reader = new StringReader(FileStore.Resource.pokedexAll);
            pokedex = new(reader);

            Init();
        }

        private void Init()
        {
            comboPoke1.ItemsSource = pokedex;
            comboPoke2.ItemsSource = pokedex;
            comboPoke3.ItemsSource = pokedex;
            comboPoke4.ItemsSource = pokedex;
            comboPoke5.ItemsSource = pokedex;
            comboPoke6.ItemsSource = pokedex;

            gridResults.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            InitGrid();
        }

        private async void InitGrid()
        {
            // setting columns of grid
            DataGridTextColumn typeCol = new DataGridTextColumn();
            typeCol.Header = "Type";
            typeCol.Binding = new Binding("Type");
            typeCol.Width = DataGridLength.SizeToCells;
            gridResults.Columns.Add(typeCol);

            // this is pretty repetitive, can it be put in a function?
            DataGridTextColumn pokemon1Col = new DataGridTextColumn();
            pokemon1Col.Header = "";
            pokemon1Col.Binding = new Binding("Pokemon1Eff");
            pokemon1Col.HeaderTemplate = (DataTemplate)Resources["headerPokemon"];
            gridResults.Columns.Add(pokemon1Col);
            DataGridTextColumn pokemon2Col = new DataGridTextColumn();
            pokemon2Col.Header = "";
            pokemon2Col.Binding = new Binding("Pokemon2Eff");
            pokemon2Col.HeaderTemplate = (DataTemplate)Resources["headerPokemon"];
            gridResults.Columns.Add(pokemon2Col);
            DataGridTextColumn pokemon3Col = new DataGridTextColumn();
            pokemon3Col.Header = "";
            pokemon3Col.Binding = new Binding("Pokemon3Eff");
            pokemon3Col.HeaderTemplate = (DataTemplate)Resources["headerPokemon"];
            gridResults.Columns.Add(pokemon3Col);
            DataGridTextColumn pokemon4Col = new DataGridTextColumn();
            pokemon4Col.Header = "";
            pokemon4Col.Binding = new Binding("Pokemon4Eff");
            pokemon4Col.HeaderTemplate = (DataTemplate)Resources["headerPokemon"];
            gridResults.Columns.Add(pokemon4Col);
            DataGridTextColumn pokemon5Col = new DataGridTextColumn();
            pokemon5Col.Header = "";
            pokemon5Col.Binding = new Binding("Pokemon5Eff");
            pokemon5Col.HeaderTemplate = (DataTemplate)Resources["headerPokemon"];
            gridResults.Columns.Add(pokemon5Col);
            DataGridTextColumn pokemon6Col = new DataGridTextColumn();
            pokemon6Col.Header = "";
            pokemon6Col.Binding = new Binding("Pokemon6Eff");
            pokemon6Col.HeaderTemplate = (DataTemplate)Resources["headerPokemon"];
            gridResults.Columns.Add(pokemon6Col);

            DataGridTextColumn weaknessesCol = new DataGridTextColumn();
            weaknessesCol.Header = "Total\nWeak";
            weaknessesCol.Binding = new Binding("Weaknesses");
            weaknessesCol.Width = 50;
            gridResults.Columns.Add(weaknessesCol);

            DataGridTextColumn resistsCol = new DataGridTextColumn();
            resistsCol.Header = "Total\nResist";
            resistsCol.Binding = new Binding("Resists");
            resistsCol.Width = 50;
            gridResults.Columns.Add(resistsCol);

            AllTypes = await PokeApiHandler.GetAllTypesAsync();

            // filling grid with a row for each pokemon type
            foreach (Type t in AllTypes)
            {
                gridResults.Items.Add(new TypeRow() { Type = t.Name, Weaknesses = 0, Resists = 0 });
            }
        }

        private void CalculateTeamWeighting()
        {
            double weighting = team.CalculateWeighting();

            labelWeighting.Content = weighting.ToString();
        }

        private async void OnChangeTeam(object sender, SelectionChangedEventArgs e)
        {
            // only deal with comboboxes
            if (sender == null || sender.GetType() != typeof(ComboBox))
                return;

            ComboBox comboBox = (ComboBox)sender;
            int teamIdx;

            // we're using the tag to identify which team member this combobox relates to
            if (comboBox.Tag != null && comboBox.Tag.GetType() == typeof(string))
            {
                string tag = (string)comboBox.Tag;
                teamIdx = int.Parse(tag);
                if (teamIdx < 0 || teamIdx > 5)
                    return;
            }
            else
            {
                return;
            }

            SmartPokemon? pokemon = null;
            if (comboBox.SelectedItem != null)
            {
                pokemon = await PokeApiHandler.GetPokemonAsync((string)comboBox.SelectedItem);
                team.SetPokemon(teamIdx, pokemon);
            }
            else
            {
                team.SetPokemon(teamIdx, null);
            }

            CalculateTeamWeighting();

            // update the grid header
            DataGridTextColumn col = (DataGridTextColumn)gridResults.Columns[teamIdx + 1];
            if (pokemon != null)
            {
                col.Header = pokemon;
            }
            else
            {
                col.Header = "";
            }

            // get the pokemon from the team once
            SmartPokemon? pokemon1 = team.GetPokemon(0);
            SmartPokemon? pokemon2 = team.GetPokemon(1);
            SmartPokemon? pokemon3 = team.GetPokemon(2);
            SmartPokemon? pokemon4 = team.GetPokemon(3);
            SmartPokemon? pokemon5 = team.GetPokemon(4);
            SmartPokemon? pokemon6 = team.GetPokemon(5);

            // update the grid rows
            foreach (TypeRow row in gridResults.Items) 
            {
                row.Pokemon1Eff = pokemon1 != null ? pokemon1.GetAttackEffectiveness(row.Type) : 0.0;
                row.Pokemon2Eff = pokemon2 != null ? pokemon2.GetAttackEffectiveness(row.Type) : 0.0;
                row.Pokemon3Eff = pokemon3 != null ? pokemon3.GetAttackEffectiveness(row.Type) : 0.0;
                row.Pokemon4Eff = pokemon4 != null ? pokemon4.GetAttackEffectiveness(row.Type) : 0.0;
                row.Pokemon5Eff = pokemon5 != null ? pokemon5.GetAttackEffectiveness(row.Type) : 0.0;
                row.Pokemon6Eff = pokemon6 != null ? pokemon6.GetAttackEffectiveness(row.Type) : 0.0;
                row.Weaknesses = team.CountWeaknesses(row.Type);
                row.Resists = team.CountResistances(row.Type);
            }

            // needs a refresh or the changes won't be reflected
            gridResults.Items.Refresh();
        }
    }

    // ----------------------------------- Helper classes -----------------------------------

    internal class TypeRow
    {
        public string Type { get; set; }
        public double Pokemon1Eff { get; set; }
        public double Pokemon2Eff { get; set; }
        public double Pokemon3Eff { get; set; }
        public double Pokemon4Eff { get; set; }
        public double Pokemon5Eff { get; set; }
        public double Pokemon6Eff { get; set; }
        public int Weaknesses { get; set; }
        public int Resists { get; set; }

        public TypeRow()
        {
            Type = "";
        }
    }


    public class LowercaseToNamecaseConverter : IValueConverter
    {
        public static string FirstCharToUpper(string input)
        {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
        }

        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string @string) 
            {
                return FirstCharToUpper(@string);
            }

            return value;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string @string)
            {
                return @string.ToLower();
            }

            return value;
        }
    }
}
