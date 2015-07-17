using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectCleaner
{
    class Program
    {
        static string GetCleanPath(string value)
        {
            string newChar = value;
            newChar = newChar.Replace("-path ", "");
            newChar = newChar.Replace(@"""", "");
            return newChar;
        }

        static bool IsTexturePath(string value)
        {
            string tempValue = value;

            if (!string.IsNullOrEmpty(value))
                tempValue = value.ToLower();

            if (tempValue.Contains("$basetexture") || tempValue.Contains("$envmap") || tempValue.Contains("$detail") || tempValue.Contains("$bumpmap") || tempValue.Contains("$normalmap") || tempValue.Contains("$reflecttexture") || tempValue.Contains("$refracttexture") || tempValue.Contains("$Iris") || tempValue.Contains("$AmbientOcclTexture") || tempValue.Contains("$CorneaTexture"))
                return true;

            return false;
        }

        static void CleanupVMTFile(string file)
        {
            string fileContents = null;

            using (StreamReader reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // If this is a texture, fix slashes and force it all to lower-case!
                    if (IsTexturePath(line))
                    {
                        line = line.Replace(@"\", "/");
                        line = line.ToLower();
                    }

                    fileContents += (line + Environment.NewLine);
                }
            }

            // Refresh/Update the file:
            if (!string.IsNullOrEmpty(fileContents))
                File.WriteAllText(file, fileContents);

            Console.WriteLine("Updated File " + file + "\n");
        }

        static void Main(string[] args)
        {
            string path = null;
            for (int i = 0; i < args.Count(); i++)
            {
                if (args[i].Contains("-path "))
                    path = GetCleanPath(args[i]);
            }

            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Please write in the path you wish to enumerate!\n");
                path = GetCleanPath(Console.ReadLine());
            }
            if (Directory.Exists(path))
            {
                Console.WriteLine("Path choosen: " + path + "\n");

                // Change folder names to lowercase.
                foreach (string dir in Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories))
                {
                    string newDir = dir.ToLower();
                    string tempDir = newDir + "_temp";
                    Console.WriteLine(string.Format("Changed old directory\n{0}\nto\n{1}\n", dir, newDir));
                    Directory.Move(dir, tempDir);
                    Directory.Move(tempDir, newDir);
                }

                // Change all .VTF file to lowercase!
                foreach (string file in Directory.EnumerateFiles(path, "*.vtf", SearchOption.AllDirectories))
                {
                    string newFilePath = file.ToLower();
                    File.Move(file, newFilePath);
                    Console.WriteLine(string.Format("Changed old filename\n{0}\nto\n{1}\n", file, newFilePath));
                }

                // Parse all .vmt files. Change the basetextures to lowercase within the vmt's... Also change the filename to lower-case!
                foreach (string file in Directory.EnumerateFiles(path, "*.vmt", SearchOption.AllDirectories))
                {
                    string newFilePath = file.ToLower();
                    File.Move(file, newFilePath);
                    Console.WriteLine(string.Format("Changed old filename\n{0}\nto\n{1}\n", file, newFilePath));
                    CleanupVMTFile(newFilePath);
                }

                Console.WriteLine("Finito!\nPress any key to close the app!\n");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Path is invalid!\n");
                Console.WriteLine("Press any key to close the app!\n");
                Console.ReadLine();
            }
        }
    }
}
