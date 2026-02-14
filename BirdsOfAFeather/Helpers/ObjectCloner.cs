using System;
using Newtonsoft.Json;

namespace BirdsOfAFeather.Helpers
{
    public static class ObjectCloner
    {
        public static T? Clone<T>(T obj, CloneType cloneType)
        {
            return cloneType switch
            {
                CloneType.Deep => DeepCopy(obj),
                CloneType.Shallow => ShallowCopy(obj),
                _ => throw new ArgumentException("Invalid CloneType", nameof(cloneType)),
            };
        }

        private static T? DeepCopy<T>(T obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private static T? ShallowCopy<T>(T obj)
        {
            if (object.Equals(obj, default(T)))
                throw new ArgumentNullException(nameof(obj));

            string jsonString = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public enum CloneType
        {
            Deep,
            Shallow
        }
    }
}
