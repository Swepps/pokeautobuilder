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
        }

        private void CalculateTeamWeighting(object sender, RoutedEventArgs e)
        {
            PokemonTeam team = new PokemonTeam();
            //Pokemon garchomp = new Pokemon(445, "Garchomp", PokemonType.Dragon, PokemonType.Ground);

            team.SetPokemon(0, pokedex.RandomPokemon());
            team.SetPokemon(1, pokedex.RandomPokemon());
            team.SetPokemon(2, pokedex.RandomPokemon());
            team.SetPokemon(3, pokedex.RandomPokemon());
            team.SetPokemon(4, pokedex.RandomPokemon());
            team.SetPokemon(5, pokedex.RandomPokemon());

            double weighting = team.CalculateWeighting();
        }
    }
}
