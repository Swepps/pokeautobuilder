# Poké Autobuilder

Poké Autobuilder is a small web-app designed to make planning a team for a playthrough much easier.

Features:
- Pokémon Team Builder.

  - This allows the user to select up to 6 Pokémon to build a team.
  - A selected Pokémon can be expanded to view their details including resistances and weaknesses, base stats, and STAB move coverage (selected move coverage will be released in a later version).
  - The team's overall statistics can be viewed below to evaluate the strengths/weaknesses of the team.
  - Pokémon can be saved to the Pokémon Storage (more details on that below).
  - Pokémon teams can be named and saved to Pokémon Team Storage (more details on that below).

- Pokémon Storage
  
  - The Pokémon Storage holds a collection of your saved Pokémon in your browser's local storage for easy access throughout the app.
  - On the right of the page, the Storage Controls can be expanded in and out using the expansion button.
      - These controls are used for easily adding and removing Pokémon to the Storage.
  - Stored Pokémon can be accessed on the Team Builder page using the radio buttons to change the Pokémon search boxes to search the Storage instead of the Pokédex.

- Team Storage

    - The Team Storage holds a collection of your saved Pokémon teams in your browser's local storage.
    - Saved teams can be expanded to view the details about that team including a chart of the team's base stats, the team's type defense, and the team's coverage.
    - Teams can be loaded back into the Team Builder using the 'Load Into Editor' button. Changes via the editor won't modify this team and will require saving to the Team Storage independently.

Poké Autobuilder can be found here:

https://pokeautobuilder.com

There are two archived applications in this repository currently:
- The first WPF app I wrote which will only work on desktop Windows
- A Blazor Server App which was the beginnings of a port to Blazor so that it could be accessed online instead as a web app
