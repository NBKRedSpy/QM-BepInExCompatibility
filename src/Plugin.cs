using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx;
using System.Text.RegularExpressions;

namespace QM_BepInExCompatibility
{


    [BepInPlugin("nbk_redspy.QM-BepInExCompatibility", "QM-BepInExCompatibility", "1.3.0")]
    public class Plugin : BaseUnityPlugin
    {

        public static string CustomModsPath { get; set; }

        public static BepInEx.Logging.ManualLogSource Log { get; set; }

        public void Awake()
        {
            CustomModsPath = Config.Bind<string>("General", nameof(Plugin.CustomModsPath), null, @"If set, will be used as the folder to search for mods").Value;

            Log = Logger;
            LoadAllWorkshopDlls();
        }

        public static void LoadAllWorkshopDlls()
        {
            //Find the workshop directory.
            string workshopPath = GetSteamWorkshopPathForGame("2059170");

            Log.LogInfo($"QM_BepInExCompatibility loading all dlls at {workshopPath}");

            int assemblyCount = 0;

            try
            {
                //Load every assembly in every sub folder.
                foreach (string dllPath in Directory.EnumerateFiles(workshopPath, "*.dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        
                        Log.LogInfo($"\t Loading: {dllPath}");
                        Assembly.LoadFrom(dllPath);

                    }
                    catch (Exception ex)
                    {
                        
                        Log.LogError($"Error loading assembly {dllPath}");
                        Log.LogError(ex);
                    }

                    assemblyCount++;
                }

                Log.LogInfo($"QM_BepInExCompatibility loaded {assemblyCount} assemblies");
            }
            catch (Exception ex)
            {
                Log.LogInfo("Error loading DLL's");
                Log.LogError(ex);
            }
        }

        public static string GetSteamInstallDirectory()
        {
            object installDir = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null);

            if (installDir is null)
            {
                throw new ApplicationException("Unable to find the Steam Directory from the Windows registry");
            }

            return (string)installDir;
        }


        public static string GetSteamWorkshopPathForGame(string gameId)
        {
            string modsPath;

            if (!string.IsNullOrWhiteSpace(Plugin.CustomModsPath))
            {
                modsPath = CustomModsPath;

                Debug.Log($"Using custom mod path: '{modsPath}'");

            }
            else
            {
                modsPath = Path.Combine(GetGameLibraryPath(GetSteamInstallDirectory(), gameId), @"steamapps\workshop\content", gameId);
            }

            if (!Directory.Exists(modsPath))
            {
                throw new ApplicationException($"Unable to find game's workshop at {modsPath}");
            }

            return modsPath;
        }


        /// <summary>
        /// Returns the path to the library that the game is installed in.
        /// </summary>
        /// <remarks>
        /// Steam allows multiple "libraries" which are folders that games are install in. 
        /// Generally these are different hard drives.
        /// By default, Steam creates on library in the Steam install directory.
        /// </remarks>
        /// <param name="gameId"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public static string GetGameLibraryPath(string steamInstallDir, string gameId)
        {
            //VDF partial example.
            //"libraryfolders"
            //{
            //	"0"
            //	{
            //		"path"		"D:\\Games\\Steam"
            //		"label"		""
            //		"contentid"		"3171027898365237378"
            //		"totalsize"		"0"
            //		"update_clean_bytes_tally"		"33254275411"
            //		"time_last_update_corruption"		"0"
            //		"apps"
            //		{
            //			"228980"		"432102418"

            List<string> text = File.ReadAllLines(Path.Combine(steamInstallDir, @"steamapps\libraryfolders.vdf")).ToList();

            int index = text.FindIndex(x => x.StartsWith($"\t\t\t\"{gameId}\""));

            for (int i = index; i > 0; i--)
            {
                string line = text[i];

                Match match = Regex.Match(line, @"^\t\t\""path\""\t\t\""(.+)\""$");
                if (match.Success)
                {
                    //The paths are escaped.
                    string libraryPath = match.Groups[1].Value.Replace(@"\\", @"\");
                    return libraryPath;
                }
            }

            throw new ApplicationException("Unable to find the steam library from the libraryfolders.vdf");
        }
    }
}
