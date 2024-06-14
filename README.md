# Poké Autobuilder

Poké Autobuilder is a small web-app designed to make planning a team for a playthrough much easier.

Features:
- Pokémon Team Builder.

  - This allows the user to select up to 6 Pokémon to build a team.
  - A selected Pokémon can be expanded to view their details including resistances and weaknesses, base stats, and STAB move coverage. Additionally a Pokémon's moves and ability can be selected in this dialog.
  - The team's overall statistics can be viewed below to evaluate the strengths/weaknesses of the team.
  - Pokémon can be saved to the Pokémon Storage (more details on that below).
  - Pokémon teams can be named and saved to Pokémon Team Storage (more details on that below).

- Pokémon Storage
  
  - The Pokémon Storage holds a collection of your saved Pokémon in your browser's local storage for easy access throughout the app.
  - On the right of the page, the Storage Controls can be expanded in and out using the expansion button.
      - These controls are used for easily adding and removing Pokémon to the Storage.
  - Stored Pokémon can be loaded into the Team Builder page.
      - Use the radio buttons to change the Pokémon search boxes to search the Storage instead of the Pokédex.

- Team Storage

    - The Team Storage holds a collection of your saved Pokémon teams in your browser's local storage.
    - Saved teams can be expanded to view the details about that team including a chart of the team's base stats, the team's type defense, and the team's coverage.
    - Teams can be loaded back into the Team Builder using the 'Load Into Editor' button. Changes via the editor won't modify this team and will require saving to the Team Storage independently.
 
- Auto Builder

    - On the Team Builder page, the Auto Builder button can be pressed when there are at least 7 Pokémon in your storage.
    - Pressing the button will open the Auto Builder dialog where teams can be generated automatically using a genetic algorithm.
    - Use the sliders and checkboxes to choose what aspects you would like to prioritise when generating your team. You can also change the amount of generations performed or the population size if you would like to try for better results.
    - Pokémon can be locked on the Team Builder page which will be reflected in the Auto Builder dialog meaning locked Pokémon will remain when a team is generated.


Poké Autobuilder can be found here:

https://pokeautobuilder.com
