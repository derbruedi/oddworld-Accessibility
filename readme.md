\# Oddworld: New 'n' Tasty - Access Mod



This mod makes \*\*Oddworld: New 'n' Tasty\*\* accessible for blind and visually impaired players. It adds screen reader support (via Tolk/NVDA), navigation aids, and an environment scanner to the game.



\## Prerequisites



1\.  \*\*Oddworld: New 'n' Tasty\*\* (PC Version).

2\.  \*\*MelonLoader\*\* (Version \*\*0.5.7\*\* is REQUIRED).

3\.  A Screen Reader (Tested with NVDA).



\## Installation



1\.  Download the \*\*MelonLoader Installer\*\*.

2\.  Run the installer.

&nbsp;   \* Select your `NNT.exe` in the game folder.

&nbsp;   \* \*\*IMPORTANT:\*\* Under "Version", uncheck "Latest" and select \*\*v0.5.7\*\*. The mod will \*\*not work\*\* with newer versions!

&nbsp;   \* Click "Install".

3\.  Run the game once to let MelonLoader create the necessary folders, then close it.

4\.  Copy the compiled `OddworldAccess.dll` into the `Mods` folder in your game directory.

5\.  \*\*Important:\*\* Copy `Tolk.dll` and `nvdaControllerClient32.dll` directly into the game's \*\*root folder\*\* (where `NNT.exe` is located), NOT into the Mods folder. This is required for speech output.



\## Controls \& Features



\### General

Upon starting the game, you should hear "Access Mod Loaded". Menus and subtitles are read aloud automatically.



\### Status \& Information

\* \*\*T\*\*: Status Report. Announces the number of Mudokons nearby and the global rescue status.

\* \*\*H\*\*: Health. Announces Abe's current health.

\* \*\*L\*\*: Repeat Message. Repeats the last spoken text or subtitle.

\* \*\*F1\*\*: Debug Coordinates. Announces your current X and Y position.



\### Navigation (The Navi)

\* \*\*N\*\*: Path Scan. Scans for the nearest navigation path (spline) and announces the distance and name.

\* \*\*B\*\*: Audio Beacon (Sonar) Toggle.

&nbsp;   \* Activates a beeping sound guiding you to your current target.

&nbsp;   \* \*\*Fast beeping:\*\* Target is close.

&nbsp;   \* \*\*Slow beeping:\*\* Target is far.

&nbsp;   \* \*\*Panning (Left/Right):\*\* Target direction relative to the player.

&nbsp;   \* \*\*Pitch (High/Low):\*\* Target is above or below you.



\### Object Scanner

Use the scanner to find specific objects in the environment (Enemies, Doors, Switches, etc.).



\* \*\*Dash (-)\*\* or \*\*Backspace\*\*: Previous Category.

\* \*\*Equals (=)\*\* or \*\*Grave (`)\*\*: Next Category.

\* \*\*Categories:\*\* All, Interact, Door, Danger, Enemy, Friend, Platform, Pickup, Portal, Secret.



Once a category is selected:

\* \*\*0 (Zero)\*\* or \*\*Plus (+)\*\*: Cycle to next object.

\* \*\*9 (Nine)\*\* or \*\*Minus (-)\*\*: Cycle to previous object.

\* \*\*Y\*\*: Object Info. Announces:

&nbsp;   \* What the object is.

&nbsp;   \* Distance (in steps).

&nbsp;   \* Direction (Left, Right, High Up, Deep Down).

&nbsp;   \* Navigation Hints (e.g., "Look for a ledge").



\### Assists

\* \*\*M (Hold)\*\*: Levitation. Abe floats at the current height. Allows moving over gaps without falling.

\* \*\*M (Release)\*\*: Gravity On.

