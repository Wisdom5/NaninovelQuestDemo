# Unity Visual Novel

## 🎮 Project Overview
A simple visual novel made with **Unity** and **Naninovel**, featuring an interactive story and a mini-game *"Matching Cards"*.

### Story & Gameplay Flow
1. Player chooses a name, which appears in dialogues.  
2. Initial dialogue with NPC-1; quest is assigned. **Location 1**.  
3. Player goes to NPC-2 for quest updates. **Location 2**.  
4. Mini-game *"Matching Cards"* starts after dialogue with NPC-2.  
5. Player interacts with an item in **Location 3**.  
6. Returns to **Location 2**: inner monologue and quest update.  
7. Final choice: give the item to NPC-1, NPC-2, or keep it.  
   - NPCs react emotionally: **happy** if they receive the item, **angry** if not.  
8. UI displays the current quest and updates.  
   - A **map** allows movement between locations.  
   - After quest completion, quest UI disappears, but the map remains.  

### Mini-Game: "Matching Cards"
- Flip two cards at a time.  
- Matching cards stay open.  
- Non-matching cards flip back after ~1.5s.  
- Game ends when all pairs are found.  
- Designed for **1–2 minutes of play**.  

---

## ⚙ Technical Implementation
- **Naninovel** handles dialogues and story flow.  
- **Custom Naninovel command**: `startCardGame` to launch the mini-game.  
- **Custom C# service**: `CardMatchingService` manages the mini-game asynchronously using **UniTask**.  
- NPC emotions change dynamically (`Default`, `Happy`, `AngryTalk`) depending on player decisions.  
- UI implemented with **CustomUI Naninovel**, replacing default UI with a custom interface.  

### Architecture
- Organized by **features**, each with:  
  - `Data` – models and enums  
  - `Declaration` – interfaces and contracts  
  - `Implementation` – concrete service logic  
  - `Presentation` – MonoBehaviours, Views, UI  
  - `UI` – custom UI prefabs and logic  
- Clear separation of concerns and modularity.  
- **No `asmdef`** used — Naninovel did not recognize scripts with assembly definitions, and documentation lacked guidance.  

---

## 📝 Notes & Current Limitations
- Briefly shows **Unity default scene** when switching Naninovel scripts (not critical for demonstration).  
- No sound or music added due to time constraints.  
- Core functionality fully implemented:  
  - Story progression  
  - Mini-game  
  - Item choice with NPC reactions  
  - Quest UI + map  

---
