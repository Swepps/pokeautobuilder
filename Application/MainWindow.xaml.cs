using PokeApiNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

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
        private Pokedex box;
        private PokemonTeam team = new PokemonTeam();

        public static List<Type> AllTypes = new List<Type>();
        public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string SaveFile = BaseDir + "/save/box.xml";

        public MainWindow()
        {
            InitializeComponent();

            // create the pokedex from the text file resource
            // TODO: use PokeApi to build the pokedex
            StringReader reader = new StringReader(FileStore.Resource.pokedexAll);
            pokedex = new(reader);
            box = new Pokedex();

            Init();
        }

        // ----------------------------------- Public Methods -----------------------------------

        public void RefreshBox()
        {
            listBox.Items.Refresh();
            comboPoke1.Items.Refresh();
            comboPoke2.Items.Refresh();
            comboPoke3.Items.Refresh();
            comboPoke4.Items.Refresh();
            comboPoke5.Items.Refresh();
            comboPoke6.Items.Refresh();
        }

        // ----------------------------------- Private Methods -----------------------------------

        private void Init()
        {
            // need to attempt to read the saved box before setting the listBox.ItemsSource
            ReadBox();

            comboPoke1.ItemsSource = box;
            comboPoke2.ItemsSource = box;
            comboPoke3.ItemsSource = box;
            comboPoke4.ItemsSource = box;
            comboPoke5.ItemsSource = box;
            comboPoke6.ItemsSource = box;

            gridResults.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            listBox.ItemTemplate = (DataTemplate)Resources["boxPokemon"];
            listBox.ItemsSource = box;

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

        private void WriteBox()
        {
            // save the contents of the box when closing the app
            XmlSerializer ser = new XmlSerializer(typeof(Pokedex));

            if (!Directory.Exists(BaseDir + "/save/"))
                Directory.CreateDirectory(BaseDir + "/save/");

            TextWriter writer = new StreamWriter(SaveFile);
            ser.Serialize(writer, box);
            writer.Close();
        }
        private async void ReadBox()
        {
            if (!File.Exists(SaveFile))
                return;

            XmlSerializer ser = new XmlSerializer(typeof(Pokedex));
            FileStream fs = new FileStream(SaveFile, FileMode.Open);
            Pokedex? box = (Pokedex?)ser.Deserialize(fs);

            if (box != null)
            {
                this.box = box;
                await this.box.DownloadPokemon();
                RefreshBox();
            }
        }

        private void RemoveSelectedFromBox()
        {
            var selectedItems = listBox.SelectedItems.Cast<PokedexEntry>().ToArray();
            foreach (PokedexEntry entry in selectedItems)
            {
                ((ObservableCollection<PokedexEntry>)listBox.ItemsSource).Remove(entry);
                //box.RemovePokemon(entry.Name);
            }
            RefreshBox();
        }

        private void CalculateTeamWeighting()
        {
            double weighting = TeamBuilder.CalculateWeighting(team);

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

            // get the selected pokemon from the combobox
            SmartPokemon? pokemon = null;
            if (comboBox.SelectedItem != null)
            {
                pokemon = await PokeApiHandler.GetPokemonAsync(((PokedexEntry)comboBox.SelectedItem).Name);
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

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WriteBox();
        }

        private async void OnClickAddToBox(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            PokedexEntry randomPokemonName = pokedex[rand.Next(pokedex.Count)];
            box.Add(randomPokemonName);
            await box.DownloadPokemon();
            RefreshBox();
        }

        private void OnClickBuildTeam(object sender, RoutedEventArgs e)
        {
            PokemonTeam newTeam = TeamBuilder.BuildTeam(box, team);

            SmartPokemon? pokemon1 = newTeam.GetPokemon(0);
            SmartPokemon? pokemon2 = newTeam.GetPokemon(1);
            SmartPokemon? pokemon3 = newTeam.GetPokemon(2);
            SmartPokemon? pokemon4 = newTeam.GetPokemon(3);
            SmartPokemon? pokemon5 = newTeam.GetPokemon(4);
            SmartPokemon? pokemon6 = newTeam.GetPokemon(5);

            comboPoke1.SelectedItem = pokemon1 != null ? box.FindPokemon(LowercaseToNamecaseConverter.FirstCharToUpper(pokemon1.Name)) : null;
            comboPoke2.SelectedItem = pokemon2 != null ? box.FindPokemon(LowercaseToNamecaseConverter.FirstCharToUpper(pokemon2.Name)) : null;
            comboPoke3.SelectedItem = pokemon3 != null ? box.FindPokemon(LowercaseToNamecaseConverter.FirstCharToUpper(pokemon3.Name)) : null;
            comboPoke4.SelectedItem = pokemon4 != null ? box.FindPokemon(LowercaseToNamecaseConverter.FirstCharToUpper(pokemon4.Name)) : null;
            comboPoke5.SelectedItem = pokemon5 != null ? box.FindPokemon(LowercaseToNamecaseConverter.FirstCharToUpper(pokemon5.Name)) : null;
            comboPoke6.SelectedItem = pokemon6 != null ? box.FindPokemon(LowercaseToNamecaseConverter.FirstCharToUpper(pokemon6.Name)) : null;

            RefreshBox();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key) 
            {
                case Key.Delete:
                    RemoveSelectedFromBox();
                    break;

                default:
                    break;
            }
        }
    }

    // ----------------------------------- Helper classes -----------------------------------

    // each row of the results grid
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

    // ----------------------------------- Conversion classes -----------------------------------

    // function used for turning lower case pokemon names into normal case
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

    // function used for inverting value in data binding
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
