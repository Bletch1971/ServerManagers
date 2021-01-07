using System.IO;
using System.Runtime.Serialization.Json;

namespace ServerManagerTool.Plugin.Common
{
    public static class JsonUtils
    {
        public static T DeserializeFromFile<T>(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                return default(T);

            StreamReader streamReader = null;

            try
            {
                streamReader = File.OpenText(file);

                Data​Contract​Json​Serializer serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(streamReader.BaseStream);
            }
            catch
            {
                return default(T);
            }
            finally
            {
                if (streamReader != null)
                    streamReader.Close();
            }
        }

        public static bool SerializeToFile<T>(T value, string file)
        {
            if (value == null)
                return false;

            StreamWriter streamWriter = null;

            try
            {
                var folder = Path.GetDirectoryName(file);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                streamWriter = File.CreateText(file);

                Data​Contract​Json​Serializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(streamWriter.BaseStream, value);

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (streamWriter != null)
                    streamWriter.Close();
            }
        }
    }
}
