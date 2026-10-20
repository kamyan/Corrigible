using System;
using System.IO;
using System.Text;
using Corrigible.Ayna.Core;

namespace Corrigible.Ayna.Game
{
    public sealed class AynaProfileStore
    {
        readonly string root;
        public string ProfilePath => Path.Combine(root, "ayna_profile.json");
        public AynaProfileStore(string directory) { root = directory ?? throw new ArgumentNullException(nameof(directory)); }
        public AynaProfileData Load(LearningLevel level)
        {
            // Kapalı düzeyinde dosya var mı diye bile bakılmaz.
            if (level == LearningLevel.Off) return new AynaProfileData();
            return File.Exists(ProfilePath) ? AynaJson.ReadProfile(File.ReadAllText(ProfilePath, Encoding.UTF8)) : new AynaProfileData();
        }
        public bool Commit(AynaProfileData updated, int expectedGeneration, string expectedMemoryId, LearningLevel level)
        {
            if (level == LearningLevel.Off) return false;
            updated.Validate(); Directory.CreateDirectory(root);
            using (var gate = new FileStream(Path.Combine(root, ".ayna_profile.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var current = Load(level);
                if (current.Generation != expectedGeneration || current.MemoryId != expectedMemoryId || updated.Generation != expectedGeneration + 1 || updated.MemoryId != expectedMemoryId) return false;
                AtomicWrite(updated); return true;
            }
        }
        public AynaProfileData EraseMemory()
        {
            Directory.CreateDirectory(root);
            using (var gate = new FileStream(Path.Combine(root, ".ayna_profile.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var empty = new AynaProfileData { MemoryId = Guid.NewGuid().ToString("N") }; AtomicWrite(empty); return empty;
            }
        }
        void AtomicWrite(AynaProfileData data)
        {
            string temp = Path.Combine(root, ".ayna_profile." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { byte[] bytes = new UTF8Encoding(false).GetBytes(AynaJson.Write(data)); file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                if (File.Exists(ProfilePath)) File.Replace(temp, ProfilePath, null); else File.Move(temp, ProfilePath);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
