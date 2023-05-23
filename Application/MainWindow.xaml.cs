using FileStore;
using PokeApiNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
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
        private SmartPokedex pokedex;
        private ObservableCollection<SmartPokemon> box;

        public PokemonTeam Team { get; set; } = new PokemonTeam();
        public DataTable TeamTotalsData { get; set; } = new DataTable();

        public static List<Type> AllTypes = new List<Type>();
        public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string SaveFile = BaseDir + "/save/box.xml";
        public static readonly string PokedexFile = BaseDir + "/save/pokedex.xml";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // create the pokedex from the text file resource
            // TODO: use PokeApi to build the pokedex
            //StringReader reader = new StringReader(FileStore.Resource.pokedexAll);
            pokedex = new();
            box = new ObservableCollection<SmartPokemon>();

            //GridRows = new ObservableCollection<TypeRow>();

            Init();
        }

        // ----------------------------------- Public Methods -----------------------------------

        public void RefreshBox()
        {
            listBox.Items.Refresh();

            // TODO: put combo boxes into a collection which can be iterated through
            comboPoke1.Items.Refresh();
            comboPoke2.Items.Refresh();
            comboPoke3.Items.Refresh();
            comboPoke4.Items.Refresh();
            comboPoke5.Items.Refresh();
            comboPoke6.Items.Refresh();
        }

        // ----------------------------------- Private Methods -----------------------------------

        private async void Init()
        {
            AllTypes = await PokeApiHandler.GetAllTypesAsync();

            // need to attempt to read the saved box before setting the listBox.ItemsSource
            await ReadBox();
            FillPokedex();

            comboPoke1.ItemsSource = box;
            comboPoke2.ItemsSource = box;
            comboPoke3.ItemsSource = box;
            comboPoke4.ItemsSource = box;
            comboPoke5.ItemsSource = box;
            comboPoke6.ItemsSource = box;

            listBox.ItemsSource = box;

            comboPokedex.ItemsSource = pokedex;

            InitGrid();            
            teamTotalsGrid.DataContext = TeamTotalsData.DefaultView;
        }

        private void InitGrid()
        {
            DataColumn column;
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "type";
            column.ReadOnly = true;
            TeamTotalsData.Columns.Add(column);

            foreach (Type type in AllTypes)
            {
                column = new DataColumn();
                column.DataType = System.Type.GetType("System.Int32");
                column.ColumnName = type.Name;
                TeamTotalsData.Columns.Add(column);
            }

            DataRow row = TeamTotalsData.NewRow();
            row["type"] = "Resistances";
            foreach (Type type in AllTypes)
            {
                row[type.Name] = 0;
            }
            TeamTotalsData.Rows.Add(row);

            row = TeamTotalsData.NewRow();
            row["type"] = "Weaknesses";
            foreach (Type type in AllTypes)
            {
                row[type.Name] = 0;
            }
            TeamTotalsData.Rows.Add(row);

            row = TeamTotalsData.NewRow();
            row["type"] = "Coverage";
            foreach (Type type in AllTypes)
            {
                row[type.Name] = 0;
            }
            TeamTotalsData.Rows.Add(row);
        }

        private void WriteBox()
        {
            try
            {
                // save the contents of the box when closing the app
                XmlSerializer ser = new XmlSerializer(typeof(List<string>));

                // we really only need a list of pokemon names and the reading back in will do the rest
                // with loading all the pokemon back in from the API
                List<string> pokemonNames = new List<string>();
                foreach (SmartPokemon p in box)
                {
                    pokemonNames.Add(p.Name);
                }
                
                if (!Directory.Exists(BaseDir + "/save/"))
                    Directory.CreateDirectory(BaseDir + "/save/");

                TextWriter writer = new StreamWriter(SaveFile);
                ser.Serialize(writer, pokemonNames);
                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private async Task ReadBox()
        {
            if (!File.Exists(SaveFile))
                return;

            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<string>));
                FileStream fs = new FileStream(SaveFile, FileMode.Open);
                List<string>? pokemonNames = (List<string>?)ser.Deserialize(fs);

                if (pokemonNames != null)
                {
                    var tasks = pokemonNames.Select(async name =>
                    {
                        SmartPokemon? pokemon = await PokeApiHandler.GetPokemonAsync(name);
                        if (pokemon != null)
                            box.Add(pokemon);
                    });

                    await Task.WhenAll(tasks);
                    RefreshBox();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void FillPokedex()
        {
            if (File.Exists(PokedexFile))
            {
                // TODO - saving pokedex to file for faster load
            }
            else
            {
                Pokedex? downloadedDex = await PokeApiHandler.GetNationalDex();
                if (downloadedDex != null)
                {
                    pokedex.SetPokedex(downloadedDex);
                }
            }
        }

        private void RemoveSelectedFromBox()
        {
            var selectedItems = listBox.SelectedItems.Cast<SmartPokemon>().ToArray();
            foreach (SmartPokemon entry in selectedItems)
            {
                ((ObservableCollection<SmartPokemon>)listBox.ItemsSource).Remove(entry);
            }
            RefreshBox();
        }

        private void CalculateTeamWeighting()
        {
            double weighting = TeamBuilder.CalculateWeighting(Team);

            labelWeighting.Content = weighting.ToString();
        }

        private void OnChangeTeam(object sender, SelectionChangedEventArgs e)
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
            Team[teamIdx] = (SmartPokemon)comboBox.SelectedItem;

            CalculateTeamWeighting();

            // update the grid header
            //DataGridTextColumn col = (DataGridTextColumn)gridResults.Columns[teamIdx + 1];
            //if (Team.Pokemon[teamIdx] != null)
            //{
            //    col.Header = Team.Pokemon[teamIdx];
            //}
            //else
            //{
            //    col.Header = "";
            //}

            // get the pokemon from the team once
            SmartPokemon? pokemon1 = Team[0];
            SmartPokemon? pokemon2 = Team[1];
            SmartPokemon? pokemon3 = Team[2];
            SmartPokemon? pokemon4 = Team[3];
            SmartPokemon? pokemon5 = Team[4];
            SmartPokemon? pokemon6 = Team[5];

            foreach (Type type in AllTypes)
            {
                foreach (DataRow row in TeamTotalsData.Rows)
                {
                    string rowType = (string)row["type"];
                    switch (rowType)
                    {
                        case "Resistances":
                            row[type.Name] = Team.CountResistances(type.Name);
                            break;

                        case "Weaknesses":
                            row[type.Name] = Team.CountWeaknesses(type.Name);
                            break;

                        case "Coverage":
                            row[type.Name] = Team.CountCoverage(type.Name);
                            break;
                    }
                }
            }

            teamTotalsGrid.Items.Refresh();
        }

        private void OnChangePokedexCombo(object sender, SelectionChangedEventArgs e)
        {
            if (comboPokedex.SelectedItem == null)
                return;
            List<NamedApiResource<Pokemon>> pokemonSpeciesVarieties = ((SmartPokemonEntry)comboPokedex.SelectedItem).GetAllVarieties();
            comboPokemonVariety.ItemsSource = pokemonSpeciesVarieties;
            comboPokemonVariety.SelectedItem = pokemonSpeciesVarieties[0];
            if (pokemonSpeciesVarieties.Count > 1)
            {                
                comboPokemonVariety.Visibility = Visibility.Visible;
            }
            else
            {
                comboPokemonVariety.Visibility = Visibility.Hidden;
            }
            comboPokemonVariety.Items.Refresh();
        }

        private async void OnClickAddToBox(object sender, RoutedEventArgs e)
        {
            // only deal with buttons
            if (sender == null || sender.GetType() != typeof(Button))
                return;

            Button button = (Button)sender;

            string tag = "none";

            // we're using the tag to identify which team member this combobox relates to
            if (button.Tag != null && button.Tag.GetType() == typeof(string))
            {
                tag = (string)button.Tag;                
            }

            NamedApiResource<Pokemon>? pokemonResource = null;
            switch (tag)
            {
                case "none":
                    pokemonResource = (NamedApiResource<Pokemon>)comboPokemonVariety.SelectedItem;
                    break;

                case "random":
                    SmartPokemonEntry? entry = pokedex.RandomPokemon();
                    if (entry != null)
                    {
                        Random rand = new Random();
                        List<NamedApiResource<Pokemon>> varieties = entry.GetAllVarieties();
                        pokemonResource = varieties[rand.Next(varieties.Count)];
                    }
                    break;

                default:
                    break;
            }

            if (pokemonResource != null)
            {
                SmartPokemon? pokemon = await PokeApiHandler.GetPokemonAsync(pokemonResource.Name);
                if (pokemon != null)
                {
                    box.Add(pokemon);
                    RefreshBox();
                }                
            }
        }

        private void OnClickBuildTeam(object sender, RoutedEventArgs e)
        {
            PokemonTeam? lockedMembers = new PokemonTeam();

            // setting locked members to the selected combobox controls
            lockedMembers[0] = !comboPoke1.IsEnabled ? comboPoke1.SelectedItem != null ? (SmartPokemon)comboPoke1.SelectedItem : null : null;
            lockedMembers[1] = !comboPoke2.IsEnabled ? comboPoke2.SelectedItem != null ? (SmartPokemon)comboPoke2.SelectedItem : null : null;
            lockedMembers[2] = !comboPoke3.IsEnabled ? comboPoke3.SelectedItem != null ? (SmartPokemon)comboPoke3.SelectedItem : null : null;
            lockedMembers[3] = !comboPoke4.IsEnabled ? comboPoke4.SelectedItem != null ? (SmartPokemon)comboPoke4.SelectedItem : null : null;
            lockedMembers[4] = !comboPoke5.IsEnabled ? comboPoke5.SelectedItem != null ? (SmartPokemon)comboPoke5.SelectedItem : null : null;
            lockedMembers[5] = !comboPoke6.IsEnabled ? comboPoke6.SelectedItem != null ? (SmartPokemon)comboPoke6.SelectedItem : null : null;

            PokemonTeam newTeam = TeamBuilder.BuildTeam(box, lockedMembers);

            SmartPokemon? pokemon1 = newTeam[0];
            SmartPokemon? pokemon2 = newTeam[1];
            SmartPokemon? pokemon3 = newTeam[2];
            SmartPokemon? pokemon4 = newTeam[3];
            SmartPokemon? pokemon5 = newTeam[4];
            SmartPokemon? pokemon6 = newTeam[5];

            comboPoke1.SelectedItem = pokemon1;
            comboPoke2.SelectedItem = pokemon2;
            comboPoke3.SelectedItem = pokemon3;
            comboPoke4.SelectedItem = pokemon4;
            comboPoke5.SelectedItem = pokemon5;
            comboPoke6.SelectedItem = pokemon6;

            RefreshBox();
        }

        private void OnClickRemoveSelected(object sender, RoutedEventArgs e)
        {
            RemoveSelectedFromBox();
        }

        private void OnClickClearStorage(object sender, RoutedEventArgs e)
        {
            listBox.SelectAll();
            RemoveSelectedFromBox();
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

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WriteBox();
        }
    }

    // ----------------------------------- Helper classes -----------------------------------

    // each row of the results grid
    public class TypeRow
    {
        public string Type { get; set; }
        public double Pokemon1Eff { get; set; }
        public double Pokemon2Eff { get; set; }
        public double Pokemon3Eff { get; set; }
        public double Pokemon4Eff { get; set; }
        public double Pokemon5Eff { get; set; }
        public double Pokemon6Eff { get; set; }
        public int DefWeaknesses { get; set; }
        public int DefResists { get; set; }
        public int AttWeaknesses { get; set; }
        public int AttResists { get; set; }

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

    // function used for turning a type name into a type .png picture
    public class TypeNameToPngConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string @string)
            {
                switch (@string)
                {
                    case "normal":
                        return FileStore.Resource.Type_Normal;
                    case "fighting":
                        return FileStore.Resource.Type_Fighting;
                    case "flying":
                        return FileStore.Resource.Type_Flying;
                    case "poison":
                        return FileStore.Resource.Type_Poison;
                    case "ground":
                        return FileStore.Resource.Type_Ground;
                    case "rock":
                        return FileStore.Resource.Type_Rock;
                    case "bug":
                        return FileStore.Resource.Type_Bug;
                    case "ghost":
                        return FileStore.Resource.Type_Ghost;
                    case "steel":
                        return FileStore.Resource.Type_Steel;
                    case "fire":
                        return FileStore.Resource.Type_Fire;
                    case "water":
                        return FileStore.Resource.Type_Water;
                    case "grass":
                        return FileStore.Resource.Type_Grass;
                    case "electric":
                        return FileStore.Resource.Type_Electric;
                    case "psychic":
                        return FileStore.Resource.Type_Psychic;
                    case "ice":
                        return FileStore.Resource.Type_Ice;
                    case "dragon":
                        return FileStore.Resource.Type_Dragon;
                    case "dark":
                        return FileStore.Resource.Type_Dark;
                    case "fairy":
                        return FileStore.Resource.Type_Fairy;
                    default:
                        return "";
                }
            }
            return value;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
