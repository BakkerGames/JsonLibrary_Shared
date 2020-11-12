﻿// Purpose: Provide a JSON Object class
// Author : Scott Bakker
// Created: 09/13/2019
// LastMod: 11/12/2020

// Notes  : The keys in this JObject implementation are case sensitive, so "abc" <> "ABC".
//        : Keys cannot be blank: null, empty, or contain only whitespace.
//        : All other key strings are valid if allowed by the .NET String type, including
//          strings with leading and/or trailing whitespace, which is significant.
//        : The items in this JObject are NOT ordered in any way. Specifically, successive
//          calls to ToString() may not return the same results.
//        : The function ToStringSorted() may be used to return a sorted list, but will be
//          somewhat slower due to overhead. The ordering is not specified here but it
//          should be consistent across calls.
//        : The function ToStringFormatted() will return a string representation with
//          whitespace added. Two spaces are used for indenting, and CRLF between lines.
//        : this[] allows missing keys. Get returns Nothing and Set does an Add.
//        : A path to a specific value can be accessed using multiple key strings, as in
//              jo["key1", "key2", "key3"] = 123
//              value = jo["key1", "key2", "key3"]
//          All sub-objects must be JObjects. At the first non-JObject, an error will be
//          thrown. The result is an untyped Object and must be cast.
//        : This is similar to "$.key1.key2.key3" used by some JSON implementations
//          except it doesn't limit the key characters or force quoting. Path splitting
//          can always be implemented in the calling application with the desired syntax.
//        : GetArray("key1", "key2", "key3") can be used to get a JArray at a specific
//          path within a JObject. An error is thrown if the position doesn't contain
//          a JArray, or Nothing will be returned if the value doesn't exist.
//        : Literal strings in C# collapse, so "a\\b" and "a\u005Cb" are equal. If used
//          for keys, they will create one entry. In VB.NET, they would not collapse and
//          would not be equal, and thus would create two entries.
//          This difference is in the calling application language, not the language of
//          this class library, and only matters with compiled literal string values.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JsonLibrary
{
    public class JObject : IEnumerable<string>
    {

        private const string JsonKeyError = "JSON Error: Key cannot be blank";

        private readonly Dictionary<string, object> _data;

        public JObject()
        {
            // Purpose: Initialize the internal structures of a new JObject
            // Author : Scott Bakker
            // Created: 09/13/2019
            _data = new Dictionary<string, object>();
        }

        public JObject(JObject jo)
        {
            // Purpose: Initialize the internal structures of a new JObject and copy values
            // Author : Scott Bakker
            // Created: 09/13/2019
            _data = new Dictionary<string, object>();
            Merge(jo);
        }

        public JObject(IDictionary<string, object> dict)
        {
            // Purpose: Initialize the internal structures of a new JObject and copy values
            // Author : Scott Bakker
            // Created: 09/13/2019
            _data = new Dictionary<string, object>();
            Merge(dict);
        }

        public IEnumerator<string> GetEnumerator()
        {
            // Purpose: Provide IEnumerable access directly to _data.Keys
            // Author : Scott Bakker
            // Created: 09/13/2019
            return _data.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // Purpose: Provide IEnumerable access directly to _data.Keys
            // Author : Scott Bakker
            // Created: 09/13/2019
            return _data.Keys.GetEnumerator();
        }

        public void Clear()
        {
            // Purpose: Clears all items from the current JObject
            // Author : Scott Bakker
            // Created: 09/13/2019
            _data.Clear();
        }

        public bool Contains(string key)
        {
            // Purpose: Identifies whether a key exists in the current JObject
            // Author : Scott Bakker
            // Created: 09/13/2019
            if (JsonRoutines.IsWhitespaceString(key))
            {
                throw new ArgumentNullException(nameof(key), JsonKeyError);
            }
            return _data.ContainsKey(key);
        }

        public int Count()
        {
            // Purpose: Return the count of items in the JObject
            // Author : Scott Bakker
            // Created: 09/13/2019
            return _data.Count;
        }

        public object this[params string[] keys]
        {
            // Purpose: Give access to item values by key
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 08/17/2020
            get
            {
                if (keys == null || keys.Length == 0)
                {
                    throw new ArgumentNullException(nameof(keys), JsonKeyError);
                }
                JObject jo = this;
                int i;
                // Follow the path specified in keys
                for (i = 0; i < keys.Length - 1; i++)
                {
                    if (JsonRoutines.IsWhitespaceString(keys[i]))
                    {
                        throw new ArgumentNullException(nameof(keys), JsonKeyError);
                    }
                    if (!jo._data.ContainsKey(keys[i]))
                    {
                        return null;
                    }
                    if (jo._data[keys[i]].GetType() != typeof(JObject))
                    {
                        throw new ArgumentException($"Type is not JObject for \"{keys[i]}\": {jo._data[keys[i]].GetType()}");
                    }
                    jo = (JObject)jo._data[keys[i]];
                }
                // Get the value
                i = keys.Length - 1;
                if (JsonRoutines.IsWhitespaceString(keys[i]))
                {
                    throw new ArgumentNullException(nameof(keys), JsonKeyError);
                }
                if (!jo._data.ContainsKey(keys[i]))
                {
                    return null;
                }
                return jo._data[keys[i]];
            }
            set
            {
                if (keys == null || keys.Length == 0)
                {
                    throw new ArgumentNullException(nameof(keys), JsonKeyError);
                }
                JObject jo = this;
                int i;
                // Follow the path specified in keys
                for (i = 0; i < keys.Length - 1; i++)
                {
                    if (JsonRoutines.IsWhitespaceString(keys[i]))
                    {
                        throw new ArgumentNullException(nameof(keys), JsonKeyError);
                    }
                    if (!jo._data.ContainsKey(keys[i]))
                    {
                        jo._data.Add(keys[i], new JObject());
                    }
                    else if (jo._data[keys[i]].GetType() != typeof(JObject))
                    {
                        throw new ArgumentException($"Type is not JObject for \"{keys[i]}\": {jo._data[keys[i]].GetType()}");
                    }
                    jo = (JObject)jo._data[keys[i]];
                }
                // Add/update value
                i = keys.Length - 1;
                if (JsonRoutines.IsWhitespaceString(keys[i]))
                {
                    throw new ArgumentNullException(nameof(keys), JsonKeyError);
                }
                if (!jo._data.ContainsKey(keys[i]))
                {
                    jo._data.Add(keys[i], value);
                }
                else
                {
                    jo._data[keys[i]] = value;
                }
            }
        }

        public Dictionary<string, object> Items()
        {
            // Purpose: Return a dictionary of all key:value items
            // Author : Scott Bakker
            // Created: 05/21/2020
            return new Dictionary<string, object>(_data);
        }

        public JArray GetArray(params string[] keys)
        {
            // Purpose: Return a JArray at a specific path
            // Author : Scott Bakker
            // Created: 09/16/2020
            // LastMod: 10/30/2020
            // Note   : This makes it easier to retrieve arrays of items
            object result = this[keys];
            if (result == null)
            {
                return null;
            }
            if (result.GetType() != typeof(JArray))
            {
                throw new ArgumentException("Type is not JArray");
            }
            return (JArray)result;
        }

        public void Merge(JObject jo)
        {
            // Purpose: Merge a new JObject onto the current one
            // Author : Scott Bakker
            // Created: 09/17/2019
            // LastMod: 08/11/2020
            // Notes  : If any keys are duplicated, the new value overwrites the current value
            if (jo == null || jo.Count() == 0)
            {
                return;
            }
            foreach (string key in jo)
            {
                if (JsonRoutines.IsWhitespaceString(key))
                {
                    throw new ArgumentNullException(nameof(key), JsonKeyError);
                }
                this[key] = jo[key];
            }
        }

        public void Merge(IDictionary<string, object> dict)
        {
            // Purpose: Merge a dictionary into the current JObject
            // Author : Scott Bakker
            // Created: 02/11/2020
            // LastMod: 08/11/2020
            // Notes  : If any keys are duplicated, the new value overwrites the current value
            //        : This is processed one key/value at a time to trap errors.
            if (dict == null || dict.Count == 0)
            {
                return;
            }
            foreach (KeyValuePair<string, object> kv in dict)
            {
                if (JsonRoutines.IsWhitespaceString(kv.Key))
                {
                    throw new SystemException(JsonKeyError);
                }
                this[kv.Key] = kv.Value;
            }
        }

        public void Remove(string key)
        {
            // Purpose: Remove an item from a JObject
            // Author : Scott Bakker
            // Created: 09/13/2019
            if (JsonRoutines.IsWhitespaceString(key))
            {
                throw new ArgumentNullException(nameof(key), JsonKeyError);
            }
            if (_data.ContainsKey(key))
            {
                _data.Remove(key);
            }
        }

        #region ToString

        public override string ToString()
        {
            // Purpose: Convert a JObject into a string
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 08/11/2020
            StringBuilder result = new StringBuilder();
            result.Append('{');
            bool addComma = false;
            foreach (KeyValuePair<string, object> kv in _data)
            {
                if (addComma)
                {
                    result.Append(',');
                }
                else
                {
                    addComma = true;
                }
                result.Append(JsonRoutines.ValueToString(kv.Key));
                result.Append(':');
                result.Append(JsonRoutines.ValueToString(kv.Value));
            }
            result.Append('}');
            return result.ToString();
        }

        public string ToStringSorted()
        {
            // Purpose: Sort the keys before returning as a string
            // Author : Scott Bakker
            // Created: 10/17/2019
            // LastMod: 11/12/2020
            StringBuilder result = new StringBuilder();
            result.Append('{');
            bool addComma = false;
            SortedList<string, object> sorted = new SortedList<string, object>(_data);
            foreach (KeyValuePair<string, object> kv in sorted)
            {
                if (addComma)
                {
                    result.Append(',');
                }
                else
                {
                    addComma = true;
                }
                result.Append(JsonRoutines.ValueToString(kv.Key));
                result.Append(':');
                result.Append(JsonRoutines.ValueToString(kv.Value));
            }
            result.Append('}');
            return result.ToString();
        }

        public string ToStringFormatted()
        {
            // Purpose: Convert this JObject into a string with formatting
            // Author : Scott Bakker
            // Created: 10/17/2019
            int indentLevel = 0;
            return ToStringFormatted(ref indentLevel);
        }

        internal string ToStringFormatted(ref int indentLevel)
        {
            // Purpose: Convert this JObject into a string with formatting
            // Author : Scott Bakker
            // Created: 10/17/2019
            // LastMod: 08/11/2020
            if (_data.Count == 0)
            {
                return "{}"; // avoid indent errors
            }
            StringBuilder result = new StringBuilder();
            result.Append('{');
            if (indentLevel >= 0)
            {
                result.AppendLine();
                indentLevel++;
            }
            bool addComma = false;
            foreach (KeyValuePair<string, object> kv in _data)
            {
                if (addComma)
                {
                    result.Append(',');
                    if (indentLevel >= 0)
                    {
                        result.AppendLine();
                    }
                }
                else
                {
                    addComma = true;
                }
                if (indentLevel > 0)
                {
                    result.Append(JsonRoutines.IndentSpace(indentLevel));
                }
                result.Append(JsonRoutines.ValueToString(kv.Key));
                result.Append(':');
                if (indentLevel >= 0)
                {
                    result.Append(' ');
                }
                result.Append(JsonRoutines.ValueToString(kv.Value, ref indentLevel));
            }
            if (indentLevel >= 0)
            {
                result.AppendLine();
                if (indentLevel > 0)
                {
                    indentLevel--;
                }
                result.Append(JsonRoutines.IndentSpace(indentLevel));
            }
            result.Append('}');
            return result.ToString();
        }

        #endregion

        #region Parse

        public static JObject Parse(string value)
        {
            // Purpose: Convert a string into a JObject
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 04/17/2020
            return Parse(new CharReader(value));
        }

        public static JObject Parse(TextReader textReader)
        {
            // Purpose: Convert a TextReader stream into a JObject
            // Author : Scott Bakker
            // Created: 04/17/2020
            return Parse(new CharReader(textReader));
        }

        internal static JObject Parse(CharReader reader)
        {
            // Purpose: Convert a partial string into a JObject
            // Author : Scott Bakker
            // Created: 09/13/2019
            // LastMod: 08/11/2020
            if (reader == null || reader.Peek() == -1)
            {
                return null;
            }
            JObject result = new JObject();
            JsonRoutines.SkipBOM(reader);
            JsonRoutines.SkipWhitespace(reader);
            if (reader.Peek() != '{')
            {
                throw new SystemException(
                    $"JSON Error: Unexpected token to start JObject: {reader.Peek()}");
            }
            reader.Read();
            do
            {
                JsonRoutines.SkipWhitespace(reader);
                // check for symbols
                if (reader.Peek() == '}')
                {
                    reader.Read();
                    break; // done building JObject
                }
                if (reader.Peek() == ',')
                {
                    // this logic ignores extra commas, but is ok
                    reader.Read();
                    continue; // Next key/value
                }
                string tempKey = JsonRoutines.GetToken(reader);
                if (JsonRoutines.IsWhitespaceString(tempKey))
                {
                    throw new SystemException(JsonKeyError);
                }
                if (tempKey.Length <= 2 || !tempKey.StartsWith("\"") || !tempKey.EndsWith("\""))
                {
                    throw new SystemException($"JSON Error: Invalid key format: {tempKey}");
                }
                // Convert to usable key
                tempKey = JsonRoutines.JsonValueToObject(tempKey).ToString();
                if (JsonRoutines.IsWhitespaceString(tempKey.Substring(1, tempKey.Length - 2)))
                {
                    throw new SystemException(JsonKeyError);
                }
                // Check for ":" between key and value
                JsonRoutines.SkipWhitespace(reader);
                if (JsonRoutines.GetToken(reader) != ":")
                {
                    throw new SystemException($"JSON Error: Missing colon: {tempKey}");
                }
                // Get value
                JsonRoutines.SkipWhitespace(reader);
                if (reader.Peek() == '{') // JObject
                {
                    JObject jo = JObject.Parse(reader);
                    result[tempKey] = jo;
                    continue;
                }
                if (reader.Peek() == '[') // JArray
                {
                    JArray ja = JArray.Parse(reader);
                    result[tempKey] = ja;
                    continue;
                }
                // Get value as a string, convert to object
                string tempValue = JsonRoutines.GetToken(reader);
                result[tempKey] = JsonRoutines.JsonValueToObject(tempValue);
            } while (true);
            return result;
        }

        public static bool TryParse(string value, ref JObject result)
        {
            // Purpose: Try to convert a string into a JObject
            // Author : Scott Bakker
            // Created: 10/23/2020
            try
            {
                result = Parse(new CharReader(value));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParse(TextReader textReader, ref JObject result)
        {
            // Purpose: Try to convert a TextReader stream into a JObject
            // Author : Scott Bakker
            // Created: 10/23/2020
            try
            {
                result = Parse(new CharReader(textReader));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

    }
}
