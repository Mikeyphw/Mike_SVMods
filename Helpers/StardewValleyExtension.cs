using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BirdsOfAFeather.Helpers;
using Newtonsoft.Json;

namespace StardewValley.Extensions
{
    public static class StardewValleyExtensions
    {
        public static T? CloneOrDeepClone<T>(this T source, bool deepClone) where T : class
        {
            return ObjectCloner.Clone(source, deepClone ? ObjectCloner.CloneType.Deep : ObjectCloner.CloneType.Shallow);
        }
    }
}