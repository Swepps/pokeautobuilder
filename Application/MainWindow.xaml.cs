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

    public partial class MainWindow : Window
    {
        private Pokedex pokedex;
        public MainWindow()
        {
            InitializeComponent();

            StringReader reader = new StringReader(FileStore.Resource.pokedexAll);
            pokedex = new(reader);

            foreach (Pokemon pokemon in pokedex.PokemonList)
            {
                comboPoke1.Items.Add(pokemon);
                comboPoke2.Items.Add(pokemon);
                comboPoke3.Items.Add(pokemon);
                comboPoke4.Items.Add(pokemon);
                comboPoke5.Items.Add(pokemon);
                comboPoke6.Items.Add(pokemon);
            }

            comboPoke1.SelectedItem = pokedex.RandomPokemon();
            comboPoke2.SelectedItem = pokedex.RandomPokemon();
            comboPoke3.SelectedItem = pokedex.RandomPokemon();
            comboPoke4.SelectedItem = pokedex.RandomPokemon();
            comboPoke5.SelectedItem = pokedex.RandomPokemon();
            comboPoke6.SelectedItem = pokedex.RandomPokemon();
        }

        private void CalculateTeamWeighting(object sender, RoutedEventArgs e)
        {
            PokemonTeam team = new PokemonTeam();

            team.SetPokemon(0, (Pokemon)comboPoke1.SelectedItem);
            team.SetPokemon(1, (Pokemon)comboPoke2.SelectedItem);
            team.SetPokemon(2, (Pokemon)comboPoke3.SelectedItem);
            team.SetPokemon(3, (Pokemon)comboPoke4.SelectedItem);
            team.SetPokemon(4, (Pokemon)comboPoke5.SelectedItem);
            team.SetPokemon(5, (Pokemon)comboPoke6.SelectedItem);

            double weighting = team.CalculateWeighting();

            labelWeighting.Content = weighting.ToString();
        }
    }
}
