Oddworld Access
Accessibility Mod for Oddworld: New 'n' Tasty
This mod adds screen reader support (TTS), Braille output, and audio navigation aids to Oddworld: New 'n' Tasty. It is designed to make the game playable for blind and visually impaired users.
🎮 Controls & Hotkeys
Navigation Scanner (Abe's "Eyes")
Use these keys to scan the environment around Abe.
Function
Key 1
Key 2
Description
Next Category
= (Equals)
` (Backtick)
Switches what the scanner looks for (e.g., Levers, Enemies, Doors).
Previous Category
- (Minus)
Backspace
Switches to the previous category.
Next Object
0
] or Num+
Cycles to the next object in the current category.
Previous Object
9
/ or Num-
Cycles to the previous object.
Object Info
Y
 
Reads the name, distance, and direction of the currently selected object.
In Google Sheets exportieren
Tabelle kopieren
General Features
Function
Key
Description
Repeat Text
H
Repeats the last spoken text (useful for hints or fast dialogue).
Toggle Audio Beacon
N
Turns the 3D "pinging" sound on or off for the selected target.
Status Report
R
Reads the number of Mudokons nearby and the total rescue percentage.
In Google Sheets exportieren
Tabelle kopieren
 
📂 Source Code Structure
Here is an explanation of what each file in the source code does. This helps you understand how the mod interacts with the Unity Engine.
1. Core Mod Files
• 
nnt.cs (Main Entry Point)
• 
What it does: This is the heart of the mod. It initializes MelonLoader, loads the Screen Reader library (Tolk), and runs the OnUpdate loop.
• 
Features:
• 
Detects if a cutscene/movie is playing.
• 
Reads Menus (Options, Pause Menu) by checking what the Unity UI Camera is selecting.
• 
Handles global hotkeys like N (Audio Beacon) and R (Status).
• 
AbeNavSystem.cs (The Scanner)
• 
What it does: Calculates the position of objects relative to the player (Abe).
• 
Logic: It searches for GameObjects based on the selected category (e.g., "Danger", "Lever"). It calculates distance and direction (Left, Right, Up, Down) and sends this text to the screen reader. It specifically looks for special objects like "Hint Flies" or "Motion Detectors".
• 
NavAudio.cs (3D Audio Beacon)
• 
What it does: Creates a "soft ping" sound that comes from the direction of the selected object.
• 
Tech: It attaches a new AudioSource to an invisible object. It adjusts the Pitch (height) and Stereo Pan (left/right) based on where the target is relative to Abe.
2. Information Readers
• 
StatusReader.cs (Game Stats)
• 
What it does: Reads the number of Mudokons nearby and the global rescue statistics.
• 
Tech: It uses Reflection to look inside the game's internal MudokonList and LevelList classes to get accurate numbers that are normally only shown on the scoreboard.
• 
SubtitleReader.cs (Dialogue)
• 
What it does: Reads spoken dialogue and cutscenes.
• 
Tech: It patches the game's Subtitles.Display() method using Harmony. Whenever the game tries to show text on screen, this file intercepts it and sends it to the screen reader. It includes spam-protection to prevent reading the same line twice instantly.
• 
DirectoryPatch.cs (Sign Reading)
• 
What it does: Specifically handles "Story Stones" (the rune stones) and Map Directories.
• 
Tech: These objects handle text differently than standard subtitles. This patch hooks into StoryStoneIdleEnter to extract the hidden text string from the stone's internal memory and read it aloud.
3. External Libraries
• 
tolk.cs (Screen Reader Bridge)
• 
What it does: Acts as a bridge between C# and the Tolk.dll (unmanaged code).
• 
Features: It tries to speak via Tolk (which supports NVDA, JAWS, SAPI, etc.) and also attempts a direct connection to the NVDA Controller for Braille output.
 
🛠 Requirements
• 
MelonLoader: To inject the code into the game.
• 
Tolk.dll / Tolk.dotnet.dll: Must be in the game directory for TTS to work.
• 
Unity Engine: The game runs on Unity 4/5.
 
Created for the blind gaming community.
