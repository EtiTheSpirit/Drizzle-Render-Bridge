# Drizzle Render Bridge
A personal app made public, designed to streamline level renders for Rain World in tandem with Community Editor.

## What does it do?

The Drizzle Render Bridge's purpose is to easily delegate out the task of rendering levels *away* from that of the RW Editor's built in renderer (which is horrifyingly slow) without introducing too much hassle.

## How do I set it up?

Download it [here](https://github.com/EtiTheSpirit/Drizzle-Render-Bridge/releases/tag/1.0.0) (see `RWDrizzleRenderBridge.zip`).

Start by running the .exe file. It will prompt you to give it `Drizzle.ConsoleApp.exe`. Consider using [SlimeCubed's fork of Drizzle](https://github.com/SlimeCubed/Drizzle/releases) (which is required if you are using Community Editor anyway). 

When prompted for the location of Drizzle...
1. Install the latest version (`Drizzle.base.Release.win-x64.zip`, under "Assets" of the top-most version on the link above). Extract it to a folder of your choosing.
2. Find `Drizzle.ConsoleApp.exe` in that extracted folder. Drag and drop this exe file onto the window of the Render Bridge. You should see blue text appear with the path of the thing you just dragged. Press enter.
3. If everything works, you will see magenta text saying that a file has been created to remember this location.

When prompted for the location of your editor...
1. Go to the folder containing your level editor's folder (don't open the level editor folder, go back out to the folder it's inside of instead)
2. Drag your editor's folder (i.e. the `Rain World Community Editor 0.4.21` folder, the numbers might be different, that's OK) onto the window of the Render Bridge. You should see blue text appear with the path of the thing you just dragged. Press enter.
3. If everything works, you will see magenta text saying that a file has been created to remember this location.

The app will only ask you for the location of these files once, unless they are moved or if you delete those special files used to remember the location.

## How do I use it?
The app will prompt you for your level file. This should be **the .txt file for your level, in your `LevelEditorProjects` folder.**
1. Find your level's .txt file in `LevelEditorProjects` (this should be a folder within your editor).
2. Drag the .txt file onto the Render Bridge's window. You should see blue text appear with the path of the thing you just dragged. Press enter.
3. Wait until it is done rendering. You will see the output from Drizzle in the console.
4. You *might* be prompted to replace files if you are rendering a level that you have rendered in the past (you will see `File ... already exists. Would you like to replace this file? (Press: [Y]es / [N]o / yes to [A]ll / [C]ancel)`). Press the letter in cyan to choose that option. If you don't see this, that's okay, it means that there's no existing renders that need to be replaced.
5. Your rendered levels will now be in the `Levels` folder of your editor, just like normal renders.
