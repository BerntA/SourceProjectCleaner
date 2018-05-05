//=========       Copyright © Reperio Studios 2013-2018 @ Bernt Andreas Eide!       ============//
//
// Purpose: Project Cleaner, used for cleaning up vtf, vmt and such. Especially useful for enforcing linux & osx compatability materials.
// Also dumps a log of broken textures, ex missing normals, bumps, etc... Or broken vmts. BB2 had A LOT of 'em so this was useful!
//
//=============================================================================================//

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
        static string _vmtPath = null;
        static List<string> _vtfSearchPaths = null;
        static bool _cleanupFoldersAndFilenames = false;

        static string[] texturePaths = {
                                            "$basetexture",
                                            "$basetexture2",
                                            "$envmap",
                                            "$detail",
                                            "$bumpmap",
                                            "$bumpmap2",
                                            "$normalmap",
                                            "$normalmap2",
                                            "$reflecttexture",
                                            "$refracttexture",
                                            "$iris",
                                            "$blendmodulatetexture",
                                            "$phongexponenttexture",
                                            "$ambientoccltexture",
                                            "%tooltexture",
                                            "$corneatexture"
                                       };

        static string GetCleanPath(string value)
        {
            return value.Replace("\"", ""); ;
        }

        static bool IsTexturePath(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Contains("\\") || value.Contains("/"))
                    return true;

                string tempValue = value.ToLower();
                foreach (string val in texturePaths)
                {
                    if (tempValue.Contains(val.ToLower()))
                        return true;
                }
            }

            return false;
        }

        static bool FindTextureWithinVPK(string file, string material)
        {
            try
            {
                // TODO BUILD OR FIND A COMPETENT VPK LOADER...
                return false;
            }
            catch
            {
                return false;
            }
        }

        static void BuildVMTFile(string inputFile, string inputData, StreamWriter wr, out string content)
        {
            Console.WriteLine(string.Format("Processing file: {0}\n", inputFile));
            content = inputData;
            using (Material m = new Material(inputData))
            {
                if (!m.IsMaterialValid())
                {
                    content = null;
                    wr.WriteLine(string.Format("Unable to parse VMT {0}!", inputFile));
                    return;
                }

                // Check texture paths:
                foreach (string val in texturePaths)
                {
                    string res = m.getParam(val), path = null;
                    if (string.IsNullOrEmpty(res) || res.Contains("env_cubemap") || res.Contains("_rt_") || res.Contains("$"))
                        continue;

                    bool bFound = false;
                    foreach (string searchPath in _vtfSearchPaths)
                    {
                        if (searchPath.ToLower().EndsWith(".vpk") && FindTextureWithinVPK(searchPath, res.ToLower().Replace("\\", "/")))
                        {
                            bFound = true;
                            break;
                        }

                        path = string.Format("{0}\\{1}{2}", searchPath, res.Replace("/", "\\"), (res.ToLower().EndsWith(".vtf") ? "" : ".vtf"));
                        if (File.Exists(path))
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (bFound)
                        Console.WriteLine(string.Format("Found path for {0} - {1}", val, res));
                    else
                    {
                        wr.WriteLine(string.Format("Couldn't find VTF {0}.vtf defined in VMT {1}!", res, inputFile));
                        Console.WriteLine(string.Format("Couldn't find VTF {0}.vtf defined in VMT {1}!", res, inputFile));
                    }
                }
            }
        }

        static void CleanupVMTFile(string file, StreamWriter wr)
        {
            StringBuilder bldr = new StringBuilder();
            using (StreamReader reader = new StreamReader(file))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // If this is a texture, fix slashes and force it all to lower-case!
                    if (IsTexturePath(line))
                        line = line.Replace(@"\", "/").ToLower();

                    bldr.AppendLine(line);
                }
            }

            // Refresh/Update the file:
            string content;
            BuildVMTFile(file, bldr.ToString(), wr, out content);
            if (!string.IsNullOrEmpty(content))
            {
                File.WriteAllText(file, content);
                Console.WriteLine(string.Format("Updated file: {0}\n", file));
            }
            else
            {
                string log = string.Format("Failed to update file: {0}\n", file);
                Console.WriteLine(log);
            }
        }

        static void Startup()
        {
            int tries = 0;
            while (true)
            {
                if (string.IsNullOrEmpty(_vmtPath) || !Directory.Exists(_vmtPath))
                {
                    if (tries > 0)
                        Console.WriteLine("Something went wrong, please specify the details again!\n");

                    Console.Write("Specify the VMT path: ");
                    _vmtPath = GetCleanPath(Console.ReadLine());

                    Console.Write("Cleanup directory & file names, Y/N ?: (slow!) ");
                    _cleanupFoldersAndFilenames = Console.ReadLine().ToUpper().StartsWith("Y");

                    Console.WriteLine();
                    tries++;
                    continue;
                }

                break;
            }

            // Load searchpaths:
            _vtfSearchPaths = new List<string>();
            using (StreamReader r = new StreamReader(string.Format("{0}\\project_searchpaths.txt", Environment.CurrentDirectory)))
            {
                while (!r.EndOfStream)
                    _vtfSearchPaths.Add(r.ReadLine());
            }
        }

        static void Main(string[] args)
        {
            if (!File.Exists(string.Format("{0}\\project_searchpaths.txt", Environment.CurrentDirectory)))
            {
                Console.WriteLine("Missing project_searchpaths.txt, unable to proceed! Specify your searchpaths in this file, one path per line.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Do you wish to use the saved settings? Y/N");
            if (Console.ReadLine().ToUpper().StartsWith("Y"))
            {
                _vmtPath = Properties.Settings.Default.vmt_path;
                _cleanupFoldersAndFilenames = Properties.Settings.Default.cleanup_dirs_filenames;
            }

            Startup();

            Properties.Settings.Default.vmt_path = _vmtPath;
            Properties.Settings.Default.cleanup_dirs_filenames = _cleanupFoldersAndFilenames;
            Properties.Settings.Default.Save();

            Console.WriteLine(string.Format("Path choosen: {0}\n", _vmtPath));

            if (_cleanupFoldersAndFilenames)
            {
                // Change folder names to lowercase.
                foreach (string dir in Directory.EnumerateDirectories(_vmtPath, "*.*", SearchOption.AllDirectories))
                {
                    string newDir = dir.ToLower();
                    string tempDir = newDir + "_temp";
                    Console.WriteLine(string.Format("Changed old directory\n{0}\nto\n{1}\n", dir, newDir));
                    Directory.Move(dir, tempDir);
                    Directory.Move(tempDir, newDir);
                }

                // Change all .VTF & .VMT files to lowercase!
                foreach (string file in Directory.EnumerateFiles(_vmtPath, "*.*", SearchOption.AllDirectories))
                {
                    string newFilePath = file.ToLower();
                    File.Move(file, newFilePath);
                    Console.WriteLine(string.Format("Changed old filename\n{0}\nto\n{1}\n", file, newFilePath));
                }
            }

            string logPath = string.Format("{0}\\clean_log.txt", Environment.CurrentDirectory);
            File.WriteAllText(logPath, "");
            using (StreamWriter wr = new StreamWriter(logPath))
            {
                // Parse all .vmt files. Change the basetextures to lowercase within the vmt's... 
                foreach (string file in Directory.EnumerateFiles(_vmtPath, "*.vmt", SearchOption.AllDirectories))
                {
                    CleanupVMTFile(file.ToLower(), wr);
                }
            }

            Console.WriteLine("\nFinito!\nPress any key to close the app!\n");
            Console.ReadKey();
        }
    }
}
