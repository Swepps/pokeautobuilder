# Poké Autobuilder

Poké Autobuilder is a web app that helps you plan and build a Pokémon team for your playthroughs.  
It takes the guesswork out of team composition by letting you explore stats, coverage, and weaknesses - or even generate complete teams automatically.

**Try it here:** [https://pokeautobuilder.com](https://pokeautobuilder.com)

---

## Quick Start

1. **Choose your game version**  
   - In the Team Builder, edit the **Search Location** box to select which game you’re playing (e.g. *FireRed & LeafGreen*, *Platinum*, *Sword & Shield*).  
   - This filters the Pokédex so you only see Pokémon available in that version.

2. **Build your team**  
   - Add up to six Pokémon. Tap the **Details** button on a Pokémon to view/edit its stats, ability, and moves.  

3. **Save or load teams**  
   - When you’re happy with your lineup, save it in **Team Storage**.  
   - You can view charts for stats, type defense, and coverage from your saved teams.
  
4. **Build your Pokémon Storage**
   - On the **Pokémon Storage** page, fill any number of **Boxes** using the **Pokémon Browser**.
   - **Import** and **Export** boxes you or your friends have created.
   - Select a Pokémon in a box to view/edit its stats, ability, and moves.

5. **Auto Build a new team**  
   - Switch to the **Auto Builder** page once you’ve filled your Storage with your Pokémon.  
   - Adjust sliders to prioritize what matters to you (type balance, stat coverage, etc.).  
   - Generate, compare, and pick from several optimized teams.

6. **Refine and customize**  
   - Use the **Settings** menu to toggle light/dark themes and adjust gameplay rules like allowing multiple Mega/Gmax Pokémon.

---

## Features

### **Team Builder**
- Build teams of up to six Pokémon.
- Tap the **Details** button on any Pokémon to view detailed info - including **stats, resistances, weaknesses, and STAB coverage**.
- Select **moves** and **abilities** directly in the Pokémon details view.
- Use the **Search Location** box to browse the Pokédex by **specific game version**, so you only see Pokémon that exist in your chosen playthrough.
- View an overall **team summary** below, showing your team’s strengths and weaknesses.
- Save and name full teams in **Team Storage**.

<img width="1968" height="1429" alt="image" src="https://github.com/user-attachments/assets/efecc95a-b262-4daa-b813-57bec7a3b015" />

---

### **Pokémon Storage**
- Stores your saved Pokémon right in your browser’s **local storage**.
- Organize Pokémon into multiple **named boxes**.
- Easily **import or export** your boxes as JSON.
- Switch your search input to look through your Storage instead of a Pokédex.

<img width="1910" height="1147" alt="image" src="https://github.com/user-attachments/assets/db5811fe-9b78-4aa3-83dd-adf734039a65" />

---

### **Team Storage**
- Keep a collection of saved teams for quick access.
- View team details, including:
  - Base stat charts  
  - Type defenses  
  - Move coverage
- Reload saved teams into the Team Builder for editing (saving again creates a new entry).

<img height="500" alt="image" src="https://github.com/user-attachments/assets/4ebcee35-0ec8-4133-a711-2e2ec5175612" />

---

### **Auto Builder**
- Automatically generate complete teams using a **genetic algorithm**.
- Adjust sliders and checkboxes to prioritize what matters most:
  - Balance of types  
  - Stat coverage  
  - Move diversity  
  - And more
- Requires at least **7 Pokémon in your Storage** (for a team size of 6) so the algorithm has options to work with.
- Teams must have between **3 and 6 Pokémon**.
- Generates not only the top team, but also a **descending list of other strong alternatives** - so you can pick what feels best.

<img width="1695" height="1284" alt="image" src="https://github.com/user-attachments/assets/9434ac3f-1695-4a3f-b71c-1245e6ca7720" />
Generation Parameters
<details>
  <img width="1103" alt="image" src="https://github.com/user-attachments/assets/9eac78b3-1c8a-43fd-840f-76fb50a042ca" />  
</details>
Alternate Generated Teams
<details>
  <img width="1094" alt="image" src="https://github.com/user-attachments/assets/d266d10c-7cb0-4332-8b9b-e86b8ffc09c6" />
</details>

---

### **Settings**
- Switch between **light** and **dark** themes.
- Enable or disable **multiple Mega/Gmax Pokémon per team** (for Legends: Z-A support).
- More customization options are planned for future updates.
  - e.g. Generation settings (e.g. physical/special split and Fairy type removal for older generations)

---

## How the Auto Builder Works

The Auto Builder uses a **genetic algorithm** - a kind of "evolution simulator" for teams.

Here’s a simplified breakdown:
1. It starts by generating a **population** of random teams (for example, 250).
2. Each team is scored for "fitness" based on your chosen priorities.
3. The best 50% survive; the weaker 50% are discarded.
4. The survivors are **mutated** - one member of each team is swapped for a random new Pokémon.
5. The process repeats for multiple generations, gradually improving the overall team quality.

- **Population size** = how many random teams are tested each generation.  
- **Generations** = how long the algorithm evolves for.  
- Higher values increase the chance of finding an ideal team, but take longer to compute.

The default of **50 generations** and **population size 250** works great for most cases, but you can tweak them for fun if you want to experiment.

---

## Built With
Poké Autobuilder is powered by **Blazor WebAssembly**, which lets the genetic algorithm run quickly and smoothly right in your browser.

---

## 💡 Tips
- You must have **at least one more Pokémon in your Storage than your target team size** so the Auto Builder has options to choose from.
- Save different configurations using **multiple boxes** for variety.
- Don’t worry about losing data - everything is stored locally in your browser.

---

## Future Plans
- More advanced Auto Builder tuning options
- UX improvments
- Performance improvements 
- User accounts for cloud saves
- Premium features?

---

## Try It Now
Build your perfect Pokémon team at **[pokeautobuilder.com](https://pokeautobuilder.com)**!

---
