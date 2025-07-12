using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chiBot.Files
{
    internal class FileHandling
    {
    }

    public static class FileHandler
    {
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile =
                JsonConvert.SerializeObject(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

        public static byte[] ReadFileBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static void RenameFile(this FileInfo fileInfo, string newName)
        {
            fileInfo.MoveTo(fileInfo.Directory.FullName + "\\" + newName);
        }

        public static string GetExecutingDir()
        {
            if (Directory.Exists("C:\\Users\\wishi\\Documents\\Discord Bots\\chiBot\\Code\\chiBot"))
            {
                return "C:\\Users\\wishi\\Documents\\Discord Bots\\chiBot\\Code\\chiBot\\Dependencies";
            }
            else
            {
                if (Directory.Exists(Path.Combine("C:\\Users\\Administrator\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs", "chiBot", "References")) || Directory.Exists(Path.Combine("C:\\Users\\Administrator\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs", "chiBot", "Steam_Integration")))
                {
                    return Path.Combine("C:\\Users\\Administrator\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs", "chiBot");
                }
                else
                {
                    return System.Reflection.Assembly.GetEntryAssembly().Location.Remove(System.Reflection.Assembly.GetEntryAssembly().Location.Length - 10, 10);
                }
            }
        }


    }
}
