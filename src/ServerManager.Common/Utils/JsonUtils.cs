using Newtonsoft.Json;

namespace ServerManagerTool.Common.Utils
{
    public static class JsonUtils
    {
        public static string Serialize<T>(T value, JsonSerializerSettings settings = null)
        {
            if (value == null)
                return string.Empty;

            try
            {
                if (settings != null)
                    return JsonConvert.SerializeObject(value, Formatting.Indented, settings);
                return JsonConvert.SerializeObject(value, Formatting.Indented);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool SerializeToFile<T>(T value, string filename, JsonSerializerSettings settings = null)
        {
            if (value == null)
                return false;

            try
            {
                var jsonString = Serialize(value, settings);
                System.IO.File.WriteAllText(filename, jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static T Deserialize<T>(string jsonString, JsonSerializerSettings settings = null)
        {
            if (string.IsNullOrEmpty(jsonString))
                return default(T);

            try
            {
                if (settings != null)
                    return JsonConvert.DeserializeObject<T>(jsonString, settings);
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch
            {
                return default(T);
            }
        }

        public static T DeserializeAnonymousType<T>(string jsonString, T anonTypeObject, JsonSerializerSettings settings = null)
        {
            if (string.IsNullOrEmpty(jsonString))
                return anonTypeObject;

            try
            {
                if (settings != null)
                    return JsonConvert.DeserializeAnonymousType<T>(jsonString, anonTypeObject, settings);
                return JsonConvert.DeserializeAnonymousType<T>(jsonString, anonTypeObject);
            }
            catch
            {
                return anonTypeObject;
            }
        }

        public static T DeserializeFromFile<T>(string file, JsonSerializerSettings settings = null)
        {
            if (string.IsNullOrEmpty(file) || !System.IO.File.Exists(file))
                return default(T);

            try
            {
                return Deserialize<T>(System.IO.File.ReadAllText(file), settings);
            }
            catch
            {
                return default(T);
            }
        }

        public static T DeserializeFromFile<T>(string file, T anonTypeObject, JsonSerializerSettings settings = null)
        {
            if (string.IsNullOrEmpty(file) || !System.IO.File.Exists(file))
                return anonTypeObject;

            try
            {
                return DeserializeAnonymousType<T>(System.IO.File.ReadAllText(file), anonTypeObject, settings);
            }
            catch
            {
                return anonTypeObject;
            }
        }

        public static void Populate(string jsonString, object target, JsonSerializerSettings settings = null)
        {
            if (string.IsNullOrEmpty(jsonString) || target == null)
                return;

            try
            {
                if (settings != null)
                    JsonConvert.PopulateObject(jsonString, target, settings);
                else
                    JsonConvert.PopulateObject(jsonString, target);
            }
            catch
            {
                return;
            }
        }

        public static void PopulateFromFile(string file, object target, JsonSerializerSettings settings = null)
        {
            if (string.IsNullOrEmpty(file) || !System.IO.File.Exists(file) || target == null)
                return;

            try
            {
                Populate(System.IO.File.ReadAllText(file), target, settings);
            }
            catch
            {
                return;
            }
        }
    }
}
