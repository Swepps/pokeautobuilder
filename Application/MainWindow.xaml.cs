using Accord.Math;
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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        private PokemonStorage box;

        public static PokemonTeam Team { get; set; } = new PokemonTeam();
        public static ObservableCollection<PokemonStat> TeamStats { get; set; } = new ObservableCollection<PokemonStat>();

        public static List<Type> AllTypes = new List<Type>();


        public static ObservableCollection<ContentControl> ResistanceBars = new ObservableCollection<ContentControl>();
        public static ObservableCollection<ContentControl> WeaknessBars = new ObservableCollection<ContentControl>();
        public static ObservableCollection<ContentControl> CoverageBars = new ObservableCollection<ContentControl>();
        public static int ListBoxItemHeight { get; set; } = 30;
        public static long TeamCombinations { get; set; } = 0;

        public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string SaveFile = BaseDir + "/save/box.xml";
        public static readonly string PokedexFile = BaseDir + "/save/pokedex.xml";

        public static double TypeDefenseWeighting { get; set; } = 1.0;
        public static double TypeCoverageWeighting { get; set; } = 1.0;
        public static double HpWeighting { get; set; } = 1.0;
        public static double AttWeighting { get; set; } = 1.0;
        public static double SpAttWeighting { get; set; } = 1.0;
        public static double DefWeighting { get; set; } = 1.0;
        public static double SpDefWeighting { get; set; } = 1.0;
        public static double SpeWeighting { get; set; } = 1.0;
        public static Dictionary<string, double> BaseStatsWeighting = new Dictionary<string, double>();

        public MainWindow()
        {
            InitializeComponent();
            PauseUI(true);
            DataContext = this;

            pokedex = new();
            box = new PokemonStorage();

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
            FillPokedex();
            AllTypes = await PokeApiHandler.GetAllTypesAsync();
            InitGrid();

            // need to attempt to read the saved box before setting the listBox.ItemsSource
            await ReadBox();            

            comboPoke1.ItemsSource = box;
            comboPoke2.ItemsSource = box;
            comboPoke3.ItemsSource = box;
            comboPoke4.ItemsSource = box;
            comboPoke5.ItemsSource = box;
            comboPoke6.ItemsSource = box;

            listBox.ItemsSource = box;

            comboPokedex.ItemsSource = pokedex;

            PauseUI(false);
        }

        private void InitGrid()
        {
            for (int i = 0; i < AllTypes.Count; i++) 
            {
                Type type = AllTypes[i];

                teamTotalsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });

                ContentControl headerItem = new ContentControl();
                headerItem.DataContext = type.Name;
                headerItem.Template = (ControlTemplate)FindResource("typeItemGridHeader");
                Grid.SetRow(headerItem, 0);
                Grid.SetColumn(headerItem, i + 2);
                teamTotalsGrid.Children.Add(headerItem);

                ContentControl valueBar = new ContentControl();
                valueBar.DataContext = 0;
                valueBar.Template = (ControlTemplate)FindResource("totalBar");
                Grid.SetRow(valueBar, 1);
                Grid.SetColumn(valueBar, i + 2);
                teamTotalsGrid.Children.Add(valueBar);
                ResistanceBars.Add(valueBar);

                valueBar = new ContentControl();
                valueBar.DataContext = 0;
                valueBar.Template = (ControlTemplate)FindResource("totalBar");
                Grid.SetRow(valueBar, 2);
                Grid.SetColumn(valueBar, i + 2);
                teamTotalsGrid.Children.Add(valueBar);
                WeaknessBars.Add(valueBar);

                valueBar = new ContentControl();
                valueBar.DataContext = 0;
                valueBar.Template = (ControlTemplate)FindResource("totalBar");
                Grid.SetRow(valueBar, 3);
                Grid.SetColumn(valueBar, i + 2);
                teamTotalsGrid.Children.Add(valueBar);
                CoverageBars.Add(valueBar);
            }
        }

        private void WriteBox()
        {
            // if the count is 0 then don't bother trying
            if (box.Count == 0)
                return;

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
                    CalculateTeamCombinations();
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
            CalculateTeamCombinations();
        }

        private Dictionary<string, double> GetBaseStatsWeighting()
        {
            return new Dictionary<string, double>
            {   { "hp", HpWeighting },
                { "attack", AttWeighting },
                { "special-attack", SpAttWeighting },
                { "defense", DefWeighting },
                { "special-defense", SpDefWeighting },
                { "speed", SpeWeighting } };
        }

        // return a pokemon team containing only the locked members in the team selections
        private PokemonTeam GetLockedMembers()
        {
            PokemonTeam lockedMembers = new PokemonTeam();

            // setting locked members to the selected combobox controls
#pragma warning disable CS8629 // Nullable value type may be null.
            lockedMembers[0] = (bool)checkLock1.IsChecked ? comboPoke1.SelectedItem != null ? (SmartPokemon)comboPoke1.SelectedItem : null : null;
            lockedMembers[1] = (bool)checkLock2.IsChecked ? comboPoke2.SelectedItem != null ? (SmartPokemon)comboPoke2.SelectedItem : null : null;
            lockedMembers[2] = (bool)checkLock3.IsChecked ? comboPoke3.SelectedItem != null ? (SmartPokemon)comboPoke3.SelectedItem : null : null;
            lockedMembers[3] = (bool)checkLock4.IsChecked ? comboPoke4.SelectedItem != null ? (SmartPokemon)comboPoke4.SelectedItem : null : null;
            lockedMembers[4] = (bool)checkLock5.IsChecked ? comboPoke5.SelectedItem != null ? (SmartPokemon)comboPoke5.SelectedItem : null : null;
            lockedMembers[5] = (bool)checkLock6.IsChecked ? comboPoke6.SelectedItem != null ? (SmartPokemon)comboPoke6.SelectedItem : null : null;
#pragma warning restore CS8629 // Nullable value type may be null.

            return lockedMembers;
        }

        private void SetTeam(PokemonTeam team)
        {
            comboPoke1.SelectedItem = team[0];
            comboPoke2.SelectedItem = team[1];
            comboPoke3.SelectedItem = team[2];
            comboPoke4.SelectedItem = team[3];
            comboPoke5.SelectedItem = team[4];
            comboPoke6.SelectedItem = team[5];
        }

        private void CalculateTeamWeighting()
        {
            BaseStatsWeighting = GetBaseStatsWeighting();
            double weighting = TeamBuilder.CalculateScore(Team);

            if (labelScore != null)
            {
                labelScore.Text = weighting.ToString();
            }
        }

        private void CalculateTeamCombinations()
        {
            PokemonTeam lockedMembers = GetLockedMembers();

            int countLockedMembers = lockedMembers.CountPokemon();
            TeamCombinations = PermutationsAndCombinations.nCr(box.Count, 6 - countLockedMembers);
            labelCombinations.Content = TeamCombinations;
        }

        private void PauseUI(bool paused)
        {
            if (paused)
            {
                gridMain.IsEnabled = false;
                gridDim.Visibility = Visibility.Visible;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            }
            else
            {
                gridMain.IsEnabled = true;
                gridDim.Visibility = Visibility.Hidden;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            }
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

            // get the pokemon from the team once
            SmartPokemon? pokemon1 = Team[0];
            SmartPokemon? pokemon2 = Team[1];
            SmartPokemon? pokemon3 = Team[2];
            SmartPokemon? pokemon4 = Team[3];
            SmartPokemon? pokemon5 = Team[4];
            SmartPokemon? pokemon6 = Team[5];

            TeamStats.Clear();
            foreach (SmartPokemon? p in Team)
            {
                if (p == null) continue;

                for (int i = 0; i < p.Stats.Count; i++)
                {
                    if (i < TeamStats.Count && TeamStats[i] != null)
                    {
                        TeamStats[i].BaseStat += p.Stats[i].BaseStat;
                    }
                    else
                    {
                        TeamStats.Add(new PokemonStat() { BaseStat = p.Stats[i].BaseStat, Stat = new NamedApiResource<Stat> { Name = p.Stats[i].Stat.Name } });
                    }
                }
            }

            for (int i = 0; i < AllTypes.Count; i++)
            {
                Type type = AllTypes[i];
                ResistanceBars[i].DataContext = Team.CountResistances(type.Name);
                WeaknessBars[i].DataContext = Team.CountWeaknesses(type.Name);
                CoverageBars[i].DataContext = Team.CountCoverage(type.Name);
            }
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
                    CalculateTeamCombinations();
                }                
            }
        }

        private void OnClickBuildTeam(object sender, RoutedEventArgs e)
        {
            // warn the user this may take a while
            if (TeamCombinations > 100000)
            {
                if (MessageBox.Show("There are " + TeamCombinations.ToString() +
                    " possible permutations of unlocked team members from the Pokemon Storage box.\nConsider using the genetic algorithm team builder as a much faster alternative.\n\nContinue?",
                    "Warning!",
                    MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    return;
            }
            PauseUI(true);

            BaseStatsWeighting = GetBaseStatsWeighting();
            PokemonTeam lockedMembers = GetLockedMembers();
            PokemonTeam newTeam = TeamBuilder.BuildTeam(box, lockedMembers);
            SetTeam(newTeam);

            PauseUI(false);
        }

        private void OnClickBuildTeamGenetic(object sender, RoutedEventArgs e)
        {
            PauseUI(true);

            BaseStatsWeighting = GetBaseStatsWeighting();
            PokemonTeam lockedMembers = GetLockedMembers();
            PokemonTeam newTeam = TeamBuilder.BuildTeamGenetic(box, lockedMembers);
            SetTeam(newTeam);

            PauseUI(false);
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

        private async void OnClickEvolveAll(object sender, RoutedEventArgs e)
        {
            PauseUI(true);

            List<SmartPokemon> newBox = new List<SmartPokemon>();
            var tasks = box.Select(async pokemon =>
            {
                SmartPokemon? evolvedP = await PokeApiHandler.GetFinalEvolution(pokemon);
                if (evolvedP != null && evolvedP.Name != pokemon.Name)
                {
                    newBox.Add(evolvedP);
                }
                else
                {
                    newBox.Add(pokemon);
                }
            });
            await Task.WhenAll(tasks);

            // replace box with newbox
            box.Clear();
            foreach (SmartPokemon p in newBox)
                box.Add(p);

            RefreshBox();

            PauseUI(false);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WriteBox();
        }

        private void OnWeightingChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CalculateTeamWeighting();
        }

        private void OnScrollStorage(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (ListBoxItemHeight < 100)
                    ListBoxItemHeight += 5;
            }
            else if (e.Delta < 0)
            {
                if (ListBoxItemHeight > 5)
                    ListBoxItemHeight -= 5;
            }
            listBox.Items.Refresh();
        }

        private void OnTeamLockChanged(object sender, RoutedEventArgs e)
        {
            CalculateTeamCombinations();
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

    // function used for checking if we want to show team stats
    public class TeamStatsBindingConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PokemonTeam @team)
            {
                if (team.IsEmpty)
                    return Binding.DoNothing;

                else
                    return value;
            }

            return Binding.DoNothing;
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

    // function used for calculating bar height from row and column stored in datacontext in the team totals grid
    public class CalculateBarHeight : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int @count)
            {
                return (@count * 8);
            }

            return value;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    
    // function used for calculating bar colour from the height
    public class CalculateBarColour : IValueConverter
    {
        private static CalculateBarHeight converter = new CalculateBarHeight();
        private static Color red = (Color)ColorConverter.ConvertFromString("#FFCE4B18");
        private static Color green = (Color)ColorConverter.ConvertFromString("#FF3AA03B");

        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int height)
            {
                double scale = (int)converter.Convert(height, targetType, parameter, culture) / 50.0;
                byte newR = (byte)((green.R * scale) + (red.R * (1.0 - scale)));
                byte newG = (byte)((green.G * scale) + (red.G * (1.0 - scale)));
                byte newB = (byte)((green.B * scale) + (red.B * (1.0 - scale)));
                return Color.FromRgb(newR, newG, newB);
            }

            return value;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // function used for checking if a binding is not null
    public class HasBindingConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // function used for getting stat value
    public class StatConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string)
                return 0;

            if (value is not List<PokemonStat>)
                return 0;

            string statName = (string)parameter;
            List<PokemonStat> stats = (List<PokemonStat>)value;

           foreach (PokemonStat stat in stats)
            {
                if (stat.Stat.Name == statName)
                    return stat.BaseStat;
            }

            return 0;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // function used for getting stat value
    public class StatTotalConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ObservableCollection<PokemonStat>)
                return 0;

            ObservableCollection<PokemonStat> stats = (ObservableCollection<PokemonStat>)value;

            int statTotal = 0;
            foreach (PokemonStat stat in stats)
            {
                statTotal += stat.BaseStat;
            }

            return "Base Stat Total: " + statTotal.ToString();
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // function used for getting stat start angle
    public class StatStartAngleConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string)
                return 0;

            if (value is not ObservableCollection<PokemonStat>)
                return 0;

            string statName = (string)parameter;
            ObservableCollection<PokemonStat> stats = (ObservableCollection<PokemonStat>)value;

            // collect all the stat data
            int hp = 0;       
            int attack = 0;       
            int specialAttack = 0;       
            int defense = 0;       
            int specialDefense = 0;       
            int speed = 0;

            int statTotal = 0;
            
            foreach (PokemonStat stat in stats)
            {
                statTotal += stat.BaseStat;

                switch (stat.Stat.Name)
                {
                    case "hp":
                        hp = stat.BaseStat;
                        break;

                    case "attack":
                        attack = stat.BaseStat;
                        break;

                    case "special-attack":
                        specialAttack = stat.BaseStat;
                        break;

                    case "defense":
                        defense = stat.BaseStat;
                        break;

                    case "special-defense":
                        specialDefense = stat.BaseStat;
                        break;

                    case "speed":
                        speed = stat.BaseStat;
                        break;
                }
            }

            if (statTotal < 1)
            {
                return 0;
            }

            // now get the angle
            switch (statName)
            {
                case "hp":
                    return 0;

                case "attack":
                    return 360.0 * ((hp) / (double)statTotal);

                case "special-attack":
                    return 360.0 * ((hp + attack) / (double)statTotal);

                case "defense":
                    return 360.0 * ((hp + attack + specialAttack) / (double)statTotal);

                case "special-defense":
                    return 360.0 * ((hp + attack + specialAttack + defense) / (double)statTotal);

                case "speed":
                    return 360.0 * ((hp + attack + specialAttack + defense + specialDefense) / (double)statTotal);
            }

            return 0;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // function used for getting stat end angle
    public class StatEndAngleConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string)
                return 0;

            if (value is not ObservableCollection<PokemonStat>)
                return 0;

            string statName = (string)parameter;
            ObservableCollection<PokemonStat> stats = (ObservableCollection<PokemonStat>)value;

            // collect all the stat data
            int hp = 0;
            int attack = 0;
            int specialAttack = 0;
            int defense = 0;
            int specialDefense = 0;
            int speed = 0;

            int statTotal = 0;

            foreach (PokemonStat stat in stats)
            {
                statTotal += stat.BaseStat;

                switch (stat.Stat.Name)
                {
                    case "hp":
                        hp = stat.BaseStat;
                        break;

                    case "attack":
                        attack = stat.BaseStat;
                        break;

                    case "special-attack":
                        specialAttack = stat.BaseStat;
                        break;

                    case "defense":
                        defense = stat.BaseStat;
                        break;

                    case "special-defense":
                        specialDefense = stat.BaseStat;
                        break;

                    case "speed":
                        speed = stat.BaseStat;
                        break;
                }
            }

            if (statTotal < 1)
            {
                return 0;
            }

            // now get the angle
            switch (statName)
            {
                case "hp":
                    return 360.0 * ((hp) / (double)statTotal);

                case "attack":
                    return 360.0 * ((hp + attack) / (double)statTotal);

                case "special-attack":
                    return 360.0 * ((hp + attack + specialAttack) / (double)statTotal);

                case "defense":
                    return 360.0 * ((hp + attack + specialAttack + defense) / (double)statTotal);

                case "special-defense":
                    return 360.0 * ((hp + attack + specialAttack + defense + specialDefense) / (double)statTotal);

                case "speed":
                    return 360.0 * ((hp + attack + specialAttack + defense + specialDefense + speed) / (double)statTotal);
            }

            return 0;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // animated class for automatically setting the height of the bars in the totals grid
    public static class Animated
    {
        private static Duration duration = TimeSpan.FromSeconds(1);

        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.RegisterAttached(
                "Height", typeof(double), typeof(Animated),
                new PropertyMetadata(HeightPropertyChanged));

        public static double GetHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(HeightProperty);
        }

        public static void SetHeight(DependencyObject obj, double value)
        {
            obj.SetValue(HeightProperty, value);
        }

        private static void HeightPropertyChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var element = obj as FrameworkElement;

            if (element != null)
            {
                var to = (double)e.NewValue;
                var animation = double.IsNaN(element.Height)
                    ? new DoubleAnimation(0, to, duration)
                    : new DoubleAnimation(to, duration);

                element.BeginAnimation(FrameworkElement.HeightProperty, animation);
            }
        }
    }

    public static class PermutationsAndCombinations
    {
        public static long nCr(int n, int r)
        {
            // naive: return Factorial(n) / (Factorial(r) * Factorial(n - r));
            return nPr(n, r) / Factorial(r);
        }

        public static long nPr(int n, int r)
        {
            // naive: return Factorial(n) / Factorial(n - r);
            return FactorialDivision(n, n - r);
        }

        private static long FactorialDivision(int topFactorial, int divisorFactorial)
        {
            long result = 1;
            for (int i = topFactorial; i > divisorFactorial; i--)
                result *= i;
            return result;
        }

        private static long Factorial(int i)
        {
            if (i <= 1)
                return 1;
            return i * Factorial(i - 1);
        }
    }
}
