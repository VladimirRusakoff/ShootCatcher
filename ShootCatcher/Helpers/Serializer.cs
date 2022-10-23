using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace ShootCatcher.Helpers
{
    public abstract class Serializer<T> where T : Serializer<T>, new()
    {
        public void Serialization(string fileName)
        {
           string serialyzed = JsonConvert.SerializeObject(this, Formatting.Indented);
            if (File.Exists(fileName))
                File.Delete(fileName);
            File.WriteAllText(fileName, serialyzed, Encoding.UTF8);
        }
        public static T Deserialization(string fileName)
        {
            T ans = default;
            try
            {
                string fileData = File.ReadAllText(fileName);
                if (!string.IsNullOrEmpty(fileData) && !string.IsNullOrWhiteSpace(fileData))
                {
                    ans = JsonConvert.DeserializeObject<T>(fileData);
                }
            }
            catch 
            {
            }
            return ans;
        }

        public static T InstanceOrDeserialize(string fileName)
        {
            if (File.Exists(fileName))
            {
                return Deserialization(fileName);
            }
            else
            {
                T ans = new T();
                ans.Serialization(fileName);
                return ans;
            }
        }
    }
}
