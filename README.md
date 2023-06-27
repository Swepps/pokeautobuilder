# pokemonautoteambuilder

Pokémon Auto Team Builder is a small web-app designed to make planning a team for a playthrough much easier.

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
  
https://swepps.github.io/pokemonautoteambuilder/

One day I will write a proper README...

There are three applications in this repository currently:
- The first WPF app I wrote which will only work on desktop Windows
- A Blazor Server App which was the beginnings of a port to Blazor so that it could be accessed online instead and as a web app
- A Blazor WebAssembly App which is the current latest project. I changed from the server app to this one when I realised that I don't want people running heavy genetic algorithms on a server.

TODO: Archive the other projects and clean up this repository

Thanks to GitHub for hosting this for me. I'll look into using a proper domain in the future.
