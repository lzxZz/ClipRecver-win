using System;
using System.Collections.Generic;
using System.Linq;

namespace ClipRecver {

    using TomlObject = Dictionary<string, object>;

    public static class Parser {

        public static T Unshift<T>(this List<T> list) {
            var retValue = list[0];
            list.RemoveAt(0);
            return retValue;
        }

        public static T Pop<T>(this List<T> list) {
            var retValue = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return retValue;
        }

        public static IEnumerable<string> LazyMySplit(this string str, char c) {
            int idx;
            while ((idx = str.IndexOf(c, 2)) != -1) {
                var retVal = str.Substring(0, idx).Trim();
                str = str.Substring(idx);
                if (!string.IsNullOrEmpty(retVal)) {
                    yield return retVal;
                }
            }
            yield return str;
        }

        public static IEnumerable<string> LazyMySplit2(this string str, char c) {
            int idx;
            while ((idx = str.IndexOf(c, 2)) != -1) {
                var retVal = str.Substring(0, idx).Trim();
                str = str.Substring(idx + 1);
                if (!string.IsNullOrEmpty(retVal)) {
                    yield return retVal;
                }
            }
            yield return str;
        }

        public static TValue GetOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict,
            TKey key
        ) => dict.TryGetValue(key, out TValue val) ? val : default;

        public static TValue GetOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dict,
            TKey key,
            Func<TKey, TValue> func
        ) {
            if (dict.ContainsKey(key)) {
                return dict[key];
            }
            var retValue = func(key);
            dict.Add(key, retValue);
            return retValue;
        }

        public static TValue GetOrDefault<TValue>(
            this TomlObject dict,
            string key,
            Func<object, bool> judger,
            Func<TValue> func
        ) {
            if (dict.ContainsKey(key)) {
                if (judger(dict[key])) {
                    return (TValue)dict[key];
                } else {
                    dict.Remove(key);
                }
            }
            var retValue = func();
            dict.Add(key, retValue);
            return retValue;
        }

        public static TValue GetOrDefault<TValue>(
            this TomlObject dict,
            string key,
            Func<object, bool> judger,
            TValue val
        ) {
            if (dict.ContainsKey(key)) {
                if (judger(dict[key])) {
                    return (TValue)dict[key];
                } else {
                    dict.Remove(key);
                }
            }
            dict.Add(key, val);
            return val;
        }

        public static TomlObject Parse(string toml) {
            var sections = toml.LazyMySplit('[')
                                .Select(str => str.Trim().LazyMySplit2('\n').ToList())
                                .ToList();
            var root = new TomlObject();
            TomlObject curr;
            foreach (var section in sections) {
                curr = root;
                if (section.First()[0] == '[') {
                    var indicator = section.Unshift();
                    List<string> frags;
                    string lastFrag = null;
                    if (indicator[1] != '[') {
                        frags = indicator.Substring(1, indicator.Length - 2)
                                    .LazyMySplit2('.')
                                    .ToList();
                    } else {
                        frags = indicator.Substring(2, indicator.Length - 4)
                                    .LazyMySplit2('.')
                                    .ToList();
                        lastFrag = frags.Pop();
                    }
                    foreach (var frag in frags) {
                        curr = curr.GetOrDefault(
                            frag, val => val is TomlObject, () => new TomlObject()
                        ) as TomlObject;
                    }
                    if (lastFrag != null) {
                        var temp = curr.GetOrDefault(
                            lastFrag,
                            val => val is List<TomlObject>,
                            () => new List<TomlObject>()
                        ) as List<TomlObject>;
                        curr = new TomlObject();
                        temp.Add(curr);
                    }
                }
                foreach (var item in section) {
                    var idx = item.IndexOf('=');
                    var key = item.Substring(0, idx).Trim();
                    var value = item.Substring(idx + 1).Trim();
                    if (value[0] == '"') {
                        value = value.Substring(1, value.Length - 2);
                    }
                    curr.Add(key, value);
                }
            }
            return root;
        }
    }
}
