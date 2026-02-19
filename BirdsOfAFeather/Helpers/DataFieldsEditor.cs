using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using StardewModdingAPI;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Enums;

namespace Mikew.BirdsOfAFeather.Helpers
{
    public static class DataFieldsEditor
    {

        public static string ModifyData(string data, int index, string newValue, ILog log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            var fields = data.Split('/');
            if (index >= fields.Length || index < 0)
            {
                log.Log("Index out of data range!", LogLevel.Error);
                return data;
            }

            fields[index] = newValue;
            return string.Join("/", fields);
        }

        public static string ModifyData(
            string data,
            int index,
            ListEditType editType,
            char delimiter,
            (string Key, string Value) newValue,
            ILog log
        )
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            var fields = data.Split('/');

            var tuples = new List<List<(string Key, string Value)>>();

            foreach (var item in fields)
            {
                var subList = new List<(string Key, string Value)>();

                if (item.Contains(delimiter))
                {
                    var parts = item.Split(delimiter);

                    for (int i = 0; i < parts.Length; i += 2)
                        subList.Add((parts[i].Trim(), parts[i + 1].Trim()));
                }
                else
                {
                    subList.Add(("", ""));
                }

                tuples.Add(subList);
            }

            if (index >= fields.Length || index < 0)
            {
                log.Log("Index out of data range", LogLevel.Error);
                return "";
            }

            if (tuples[index].Exists(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value)))
            {
                switch (editType)
                {
                    case ListEditType.Add:
                        tuples[index].Insert(0, newValue);
                        break;
                    case ListEditType.Remove:
                        tuples[index] = tuples[index]
                            .Where(x => x.Key != newValue.Key || x.Value != newValue.Value)
                            .ToList();
                        break;
                    case ListEditType.Replace:
                        tuples[index] = new List<(string Key, string Value)> { newValue };
                        break;
                }
            }
            else
            {
                log.Log("Index points to an empty tuple", LogLevel.Warn);
                return data;
            }

            var result = string.Join(
                "/",
                fields.Select(
                    (field, i) =>
                    {
                        if (i == index)
                        {
                            return string.Join(
                                delimiter.ToString(),
                                tuples[i].Select(x => $"{x.Key}{delimiter}{x.Value}")
                            );
                        }
                        else
                        {
                            return field;
                        }
                    }
                )
            );

            return result;
        }

        public static string ModifyData(string data, int index, string newValue, IMonitor? monitor = null)
        {
            var fields = data.Split('/');
            if (index >= fields.Length || index < 0)
            {
                monitor?.Log("Index out of data range!", LogLevel.Error);
                return data;
            }
            else
            {
                fields[index] = newValue;
                return string.Join("/", fields);
            }
        }

        public static string ModifyData(
            string data,
            int index,
            ListEditType editType,
            char delimiter,
            (string Key, string Value) newValue,
            IMonitor? monitor = null
        )
        {
            var fields = data.Split('/');

            var tuples = new List<List<(string Key, string Value)>>();

            foreach (var item in fields)
            {
                // Create a new sublist to store tuples for this field
                var subList = new List<(string Key, string Value)>();

                if (item.Contains(delimiter))
                {
                    var parts = item.Split(delimiter);

                    // Iterate through the key-value pairs in steps of 2
                    for (int i = 0; i < parts.Length; i += 2)
                    {
                        // Create a new tuple with the key-value pair and add it to the sublist
                        subList.Add((parts[i].Trim(), parts[i + 1].Trim()));
                    }
                }
                else
                {
                    // If the field does not contain the delimiter, add an empty tuple to the sublist
                    subList.Add(("", ""));
                }

                tuples.Add(subList);
            }

            // Check if the index is within the bounds of the data
            if (index >= fields.Length || index < 0)
            {
                monitor?.Log("Index out of data range", LogLevel.Error);
                return "";
            }

            // Check if the tuple at the specified index is not empty
            if (
                tuples[index]
                    .Exists(x => !string.IsNullOrEmpty(x.Key) && !string.IsNullOrEmpty(x.Value))
            )
            {
                switch (editType)
                {
                    case ListEditType.Add:
                        tuples[index].Insert(0, newValue); // Insert at the start of the list
                        break;
                    case ListEditType.Remove:
                        tuples[index] = tuples[index]
                            .Where(x => x.Key != newValue.Key || x.Value != newValue.Value)
                            .ToList();
                        break;
                    case ListEditType.Replace:
                        tuples[index] = new List<(string Key, string Value)> { newValue };
                        break;
                }
            }
            else
            {
                monitor?.Log("Index points to an empty tuple", LogLevel.Warn);
                return data;
            }

            // Reconstruct the modified data string
            var result = string.Join(
                "/",
                fields.Select(
                    (field, i) =>
                    {
                        if (i == index)
                        {
                            return string.Join(
                                delimiter.ToString(),
                                tuples[i].Select(x => $"{x.Key}{delimiter}{x.Value}")
                            );
                        }
                        else
                        {
                            return field;
                        }
                    }
                )
            );

            return result;
        }
    }
}
