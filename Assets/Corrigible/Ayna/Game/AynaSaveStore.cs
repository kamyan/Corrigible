using System;
using System.IO;
using System.Text;
namespace Corrigible.Ayna.Game
{
    public sealed class AynaSaveStore
    {
        readonly string root;
        public string SavePath => Path.Combine(root, "ayna_savegame.json");
        public AynaSaveStore(string directory) { root = directory ?? throw new ArgumentNullException(nameof(directory)); }
        public bool Exists => File.Exists(SavePath);
        public string Load() => File.ReadAllText(SavePath, Encoding.UTF8);
        public void Save(string json)
        {
            Directory.CreateDirectory(root); string temp = Path.Combine(root, ".ayna_save." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var f = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { var bytes = new UTF8Encoding(false).GetBytes(json); f.Write(bytes, 0, bytes.Length); f.Flush(true); }
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, null); else File.Move(temp, SavePath);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
