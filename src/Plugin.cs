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
using System.Collections.Concurrent;
using UnityEngine.Assertions.Must;
using System.Runtime.ExceptionServices;
using sd = System.Diagnostics;

namespace QM_BepInExCompatibility
{


    [BepInPlugin("nbk_redspy.QM-BepInExCompatibility", "QM-BepInExCompatibility", "1.4.0")]
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
             sd.Stopwatch stopwatch = sd.Stopwatch.StartNew();

            //Find the workshop directory.
            string modsPath = GetModsPath("2059170");

            Log.LogInfo($"QM_BepInExCompatibility loading all dlls at {modsPath}");

            int assemblyCount = 0;

            try
            {

                //List of dlls
                List<string> dllPaths = Directory.GetFiles(modsPath, "*.dll", SearchOption.AllDirectories).ToList();

                ConcurrentBag<FileHashInfo> hashedInfo = new ConcurrentBag<FileHashInfo>();



                Log.LogInfo("Getting DLL List");

                //Load the files and hashes
                Parallel.ForEach(dllPaths, x =>
                {
                    hashedInfo.Add(new FileHashInfo(x));
                });

                Log.LogInfo($"Hash compute time: {stopwatch.Elapsed}");

                //Order by name for nicety.
                IOrderedEnumerable<IGrouping<string, FileHashInfo>> loadingList
                    = hashedInfo.OrderBy(x => x.FileName)
                    .GroupBy(x => x.Hash)
                    .OrderBy(x => x.First().FileName);

                foreach (var group in loadingList)
                {
                    FileHashInfo firstItem = group.First();

                    try
                    {
                        Log.LogInfo($"Loading {firstItem.FileName} {firstItem.FilePath} [{firstItem.Hash}]");
                        Assembly.LoadFrom(firstItem.FilePath);

                        group.Skip(1).ToList().ForEach(x =>
                        {
                            Log.LogInfo($"\t Already Loaded {x.FilePath}  [{firstItem.Hash}]");
                        });
                    }
                    catch (Exception ex)
                    {

                        Log.LogError($"Error loading assembly {firstItem.FilePath}");
                        Log.LogError(ex);
                    }
                    assemblyCount++;
                }

                Log.LogInfo($"QM_BepInExCompatibility loaded {assemblyCount} unique assemblies");
                Log.LogInfo($"QM_BepInExCompatibility load time: {stopwatch.Elapsed}");

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



        public static string GetModsPath(string steamGameId)
        {
            string modsPath;

            if (!string.IsNullOrWhiteSpace(Plugin.CustomModsPath))
            {
                modsPath = CustomModsPath;

                Debug.Log($"Using custom mod path: '{modsPath}'");
                return modsPath;
            }

            //Try Game's mod directory.  This is only for non Steam version
            modsPath = Path.Combine(Application.dataPath, "..",  "mods");
            if (Directory.Exists(modsPath)) return modsPath;

            //Try for Steam
            modsPath = Path.Combine(GetGameLibraryPath(GetSteamInstallDirectory(), steamGameId), @"steamapps\workshop\content", steamGameId);

            if (Directory.Exists(modsPath)) return modsPath;

            //No path found
            throw new ApplicationException($"Unable to find game's workshop at {modsPath}");
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
