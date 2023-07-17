using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RWDrizzleRenderBridge {
	public class Program {
		private static void Main(string[] args) {

			bool help = HasArg(args, "-h");
			if (help) {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("Drizzle Render Bridge Info ");
				
				Console.WriteLine();
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("Behavioral Notice: The application will prompt you for your level editor's location,");
				Console.WriteLine("and operates with the expectation that you are using this alongside a level editor.");
				Console.WriteLine("If you need to run this without an editor, consider just using Drizzle directly.");
				Console.WriteLine();
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Drizzle Render Bridge Command Line Args");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("-h ");
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine(": Display this list of valid arguments to pass into the application.");
				Console.WriteLine();
				/*
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("-verbose ");
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine(": Allow Drizzle's console output to be put into this console.");
				Console.WriteLine();
				*/
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("[any text] ");
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine(": Treated as a file to convert.");
				Console.WriteLine();
				Console.WriteLine("I haven't implemented anything else :(");
				Console.WriteLine();
				Console.WriteLine();
			}

			bool useVerboseDrizzle = true;// HasArg(args, "-verbose");

			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("Welcome to the Drizzle Render Bridge. ");
			if (!help) Console.Write("Run with \"-h\" for help.");
			
			Console.WriteLine();

			FileInfo drizzle = GetOrPromptForDrizzle();
			DirectoryInfo levelEditor = GetOrPromptForLevelEditor();

			Queue<FileInfo> jobs = new Queue<FileInfo>();
			foreach (string arg in args) {
				if (arg.StartsWith('-')) continue;
				jobs.Enqueue(new FileInfo(arg));
			}

			Console.WriteLine();

			do {
				FileInfo txt;
				if (jobs.Count == 0) {
					txt = ReadFile("Please paste the file path of your room (<editor>/LevelEditorProjects/.../RG_WHATEVER.txt)...");
				} else {
					txt = jobs.Dequeue();
				}
				bool hasRemainingJob = jobs.Count > 0;
				string relative = Path.GetRelativePath(levelEditor.FullName, txt.FullName);

				string relativeToProjects = Path.GetRelativePath(Path.Combine(levelEditor.FullName, "LevelEditorProjects"), txt.Directory!.FullName);
				string folderRelativeToLevel = Path.Combine(levelEditor.FullName, "Levels", relativeToProjects);
				bool canCopy = true;
				if (relative == txt.FullName) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("This level file is not in a subdirectory of your level editor. It will not automatically copy into your editor's Levels folder.");
					canCopy = false;
				}

				ProcessStartInfo startDrizzle = new ProcessStartInfo {
					FileName = drizzle.FullName,
					RedirectStandardError = !useVerboseDrizzle,
					RedirectStandardOutput = !useVerboseDrizzle, // Redirect stdio stuff because I don't want it cluttering the console.
					// bless the guy that wrote drizzle for actually using integer return codes where possible
				};

				startDrizzle.ArgumentList.Add("render");
				startDrizzle.ArgumentList.Add(txt.FullName);
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Working...");
				Console.ForegroundColor = ConsoleColor.DarkGray;
				Process drizzleProcess = Process.Start(startDrizzle)!;
				drizzleProcess.WaitForExit();
				int code = drizzleProcess.ExitCode;

				if (code == 0) {
					if (canCopy) {
						Console.ForegroundColor = ConsoleColor.Green;
						Console.WriteLine("Copying...");
						DirectoryInfo levels = new DirectoryInfo(Path.Combine(drizzle.Directory!.FullName, "Data", "Levels"));
						string inputFileNoExt = StripExtension(txt).ToLower();
						IEnumerable<FileInfo> relevant = levels.GetFiles().Where(file => {
							string nameNoExt = StripExtension(file).ToLower();
							if (nameNoExt == inputFileNoExt) {
								return true;
							} else if (file.Extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase)) {
								return Regex.IsMatch(file.Name.ToLower(), @$"({Regex.Escape(inputFileNoExt)})_\d+.png");
							}
							return false;
						});

						bool optionAppliesToAll = false;
						int replaceType = int.MaxValue;
						foreach (FileInfo file in relevant) {
							Console.ForegroundColor = ConsoleColor.DarkGreen;
							string dest = Path.Combine(folderRelativeToLevel, file.Name);
							FileInfo destFile = new FileInfo(dest);
							Directory.CreateDirectory(destFile.Directory!.FullName); // Ensure the folder exists.
							if (!File.Exists(dest)) {
								file.CopyTo(dest);
								Console.WriteLine($"Copied to {dest}");
							} else {
								if (!optionAppliesToAll || replaceType == int.MaxValue) {
									replaceType = PromptReplace($"A file at {dest} already exists!\nWould you like to replace this file?");
								}
								if (replaceType > 0) {
									if (replaceType == 2) optionAppliesToAll = true;

									File.Delete(dest);
									file.CopyTo(dest);
									Console.ForegroundColor = ConsoleColor.DarkCyan;
									Console.WriteLine($"Overwrote {dest}");
								} else {
									if (replaceType == -1) optionAppliesToAll = true;

									Console.ForegroundColor = ConsoleColor.DarkYellow;
									Console.WriteLine($"Skipped copying to {dest}");
								}
							}
						}
					} else {
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine("As the previous note mentions, your rendered level cannot be copied. You will need to go get it out of Drizzle's Data folder yourself...");
					}
				} else {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("An error occurred when trying to convert one or more levels.");
					if (!useVerboseDrizzle) {
						// Forward the application's output if it was hidden normally[
						do {
							if (!drizzleProcess.StandardOutput.EndOfStream) {
								string? line = drizzleProcess.StandardOutput.ReadLine();
								if (line != null) {
									Console.ForegroundColor = ConsoleColor.DarkGray;
									Console.WriteLine(line);
									continue;
								}
							}
							break;
						} while (true);
					}
				}
				if (!hasRemainingJob) {
					Console.ForegroundColor = ConsoleColor.White;
					Console.WriteLine("Press ENTER to render another level, or any other key to quit . . . ");
					if (Console.ReadKey().Key != ConsoleKey.Enter) break;
				} else {
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine("Moving on to next file...");
				}
			} while (true);
		}

		/// <summary>
		/// Uses string matching to find if an argument has been declared.
		/// </summary>
		/// <param name="args"></param>
		/// <param name="arg"></param>
		/// <param name="technique"></param>
		/// <returns></returns>
		private static bool HasArg(string[] args, string arg, StringComparison technique = StringComparison.OrdinalIgnoreCase) {
			for (int i = 0; i < args.Length; i++) {
				if (args[i].Equals(arg, technique)) return true;
			}
			return false;
		}

		/// <summary>
		/// Asks the user for the location of the Drizzle console app.
		/// </summary>
		/// <returns></returns>
		private static FileInfo GetOrPromptForDrizzle() {
			FileInfo data = new FileInfo(@".\drizzleconsole.txt");
			if (data.Exists) {
				string path = File.ReadAllText(data.FullName);
				FileInfo exe = new FileInfo(path);
				if (exe.Exists) {
					return exe;
				}
			}

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Preliminary note: If you are using Community Editor, use SlimeCubed's version of the app.");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("To copy from a command prompt, select the text, and press the right mouse button. This will copy the selected text.");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("Click on the top-most release, then find \"Drizzle.base.Release.win-x64.zip\" and click on that. ");
			Console.Write("You can get the fork here: ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("https://github.com/SlimeCubed/Drizzle/releases");
			FileInfo drizzle = ReadFile("Please paste the file path to Drizzle.ConsoleApp.exe...");
			while (drizzle.Name.ToLower() != "drizzle.consoleapp.exe") {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("Are you sure this is correct? Usually, the console app is named ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("Drizzle.ConsoleApp.exe");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(", but you input");
				Console.Write("a file named ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(drizzle.Name);
				bool @continue = PromptYN($"Continue with this file ({drizzle.Name}) anyway?");
				if (@continue) break;
				drizzle = ReadFile("Please paste the file path to Drizzle.ConsoleApp.exe...");
			}
			File.WriteAllText(data.FullName, drizzle.FullName);
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine("A new file has been created in the same directory as this exe named drizzleconsole.txt");
			Console.WriteLine();
			Console.WriteLine();
			return drizzle;
		}

		/// <summary>
		/// Removes the extension from a file's name.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private static string StripExtension(FileInfo file) {
			string name = file.Name;
			string ext = file.Extension;
			if (ext.Length > 0) {
				name = name[..^ext.Length];
			}
			return name;
		} 

		/// <summary>
		/// Asks the user for the location of the level editor.
		/// </summary>
		/// <returns></returns>
		private static DirectoryInfo GetOrPromptForLevelEditor() {
			FileInfo data = new FileInfo(@".\editorlocation.txt");
			if (data.Exists) {
				string path = File.ReadAllText(data.FullName);
				DirectoryInfo editorDir = new DirectoryInfo(path);
				if (editorDir.Exists) {
					return editorDir;
				}
			}

			DirectoryInfo lvEditor = ReadDirectory("Please paste the folder path to your level editor (such as the Community Editor folder).\nIt should contain a LevelEditorProjects folder and Levels folder...");
			do {
				bool levelsSubdirExists = Directory.Exists(Path.Combine(lvEditor.FullName, "Levels"));
				bool projectsSubdirExists = Directory.Exists(Path.Combine(lvEditor.FullName, "LevelEditorProjects"));
				if (levelsSubdirExists && projectsSubdirExists) break;

				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("Are you sure this is correct? Usually, level editors contain a ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("Levels ");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("folder");
				Console.Write("and a ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write("LevelEditorProjects ");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("folder.");
				Console.WriteLine("Either one or both of these folders does not exist in the folder you put here.");

				bool @continue = PromptYN($"Continue with this folder ({lvEditor.Name}) anyway?");
				if (@continue) break;
				lvEditor = ReadDirectory("Please paste the folder path to your level editor (such as the Community Editor folder).\nIt should contain a LevelEditorProjects folder and Levels folder...");
			} while (true);
			File.WriteAllText(data.FullName, lvEditor.FullName);
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine("A new file has been created in the same directory as this exe named editorlocation.txt");
			Console.WriteLine();
			Console.WriteLine();
			return lvEditor;
		}

		/// <summary>
		/// Removes leading and trailing quotation marks from a string iff it starts and ends with <c>"</c>.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static string TrimQuotes(string str) {
			if (str.Length > 2) {
				if (str[0] == '"' && str[^1] == '"') {
					return str[1..^1];
				}
			}
			return str;
		}

		/// <summary>
		/// Writes a prompt about how to handle overwriting files, returning a value reflecting on the user's choice:
		/// <list type="bullet">
		/// <item>
		/// <term>-1</term>
		/// <description>Cancel the operation (do not overwrite any files relating to the current level)</description>
		/// </item>
		/// <item>
		/// <term>0</term>
		/// <description>Do not overwrite the current file. Prompt again.</description>
		/// </item>
		/// <item>
		/// <term>1</term>
		/// <description>Overwrite the current file, and prompt again.</description>
		/// </item>
		/// <item>
		/// <term>2</term>
		/// <description>Overwrite all files pertaining to the current level.</description>
		/// </item>
		/// </list>
		/// </summary>
		/// <param name="prompt"></param>
		/// <returns></returns>
		private static int PromptReplace(string prompt) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(prompt);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("(Press: [");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("Y");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("]es / [");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("N");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("]o / yes to [");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("A");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("]ll / [");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("C");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("]ancel)");
			do {
				char c = Console.ReadKey(true).KeyChar;
				if (c == 'y') {
					return 1;
				} else if (c == 'n') {
					return 0;
				} else if (c == 'a') {
					return 2;
				} else if (c == 'c') {
					return -1;
				} else {
					Console.Beep();
				}
			} while (true);
		}

		private static bool PromptYN(string prompt) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(prompt);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("(Press: [");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("Y");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("]es or [");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write("N");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("]o)");
			do {
				char c = Console.ReadKey(true).KeyChar;
				if (c == 'y') {
					return true;
				} else if (c == 'n') {
					return false;
				} else {
					Console.Beep();
				}
			} while (true);
		}

		/// <summary>
		/// Reads a file path from the command line, ensuring that the file exists. Trims leading and trailing quotes from the input if needed, but otherwise
		/// treats the entire input string as one singular filepath, so spaces are allowed.
		/// </summary>
		/// <param name="prompt"></param>
		/// <returns></returns>
		private static FileInfo ReadFile(string prompt) {
			do {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(prompt);
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine("Protip: You can just drag the file onto this console window. Quotation marks are ignored.");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write(" > ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				string? data = Console.ReadLine();
				
				if (data == null) {
					Console.Clear();
					continue;
				}
				try {
					FileInfo file = new FileInfo(TrimQuotes(data));
					if (!file.Exists) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("The provided file does not exist.");
						continue;
					}
					return file;
				} catch (Exception exc) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"The path you input isn't quite right: {exc.Message}");

				}
			} while (true);
		}

		private static DirectoryInfo ReadDirectory(string prompt) {
			do {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine(prompt);
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine("Protip: You can just drag the file onto this console window. Quotation marks are ignored.");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write(" > ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				string? data = Console.ReadLine();
				if (data == null) {
					Console.Clear();
					continue;
				}
				try {
					DirectoryInfo file = new DirectoryInfo(TrimQuotes(data));
					if (!file.Exists) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("The provided folder does not exist.");
						continue;
					}
					return file;
				} catch (Exception exc) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"The path you input isn't quite right: {exc.Message}");

				}
			} while (true);
		}

	}
}