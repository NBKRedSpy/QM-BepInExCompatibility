using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QM_BepInExCompatibility
{
    internal class FileHashInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Hash { get; set; }


        public FileHashInfo()
        {
                
        }

        public FileHashInfo(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Hash = GetFileHash(filePath);
        }

        private string GetFileHash(string filePath)
        {
            using (Stream reader = File.OpenRead(filePath))
            {

                var sha256hasher = System.Security.Cryptography.SHA256.Create();
                byte[] hash = sha256hasher.ComputeHash(reader);

                return Convert.ToBase64String(hash);
            }
        }

    }
}
