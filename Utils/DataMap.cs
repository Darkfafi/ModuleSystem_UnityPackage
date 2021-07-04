using System;
using System.Collections.Generic;
using System.Linq;

namespace ModuleSystem
{
	public class DataMap : IDisposable
	{
		#region Variables

		private readonly Dictionary<string, object> _dataMap = new Dictionary<string, object>();
		private readonly Dictionary<string, List<string>> _marks = new Dictionary<string, List<string>>();
		private readonly List<string> _tags = new List<string>();

		#endregion

		#region Public Methods

		public void Tag(string key, string suffix = "")
		{
			string tag = string.Concat(key, ")", suffix);
			if (!_tags.Contains(tag))
			{
				_tags.Add(tag);
			}
		}

		public bool HasTag(string key, string suffix = "")
		{
			string tag = string.Concat(key, ")", suffix);
			return _tags.Contains(tag);
		}

		public bool RemoveTag(string key, string suffix = "")
		{
			string tag = string.Concat(key, ")", suffix);
			return _tags.Remove(tag);
		}

		public void Mark(string key, string suffix = "Default")
		{
			if (!_marks.TryGetValue(key, out List<string> values))
			{
				_marks[key] = values = new List<string>();
			}

			if (!string.IsNullOrEmpty(suffix) && !values.Contains(suffix))
			{
				values.Add(suffix);
			}
		}

		public bool HasMark(string key, string suffix = "")
		{
			if (_marks.TryGetValue(key, out List<string> values))
			{
				return string.IsNullOrEmpty(suffix) || values.Contains(suffix);
			}
			return false;
		}

		public bool HasMarkSuffix(string key, string suffix)
		{
			if (TryGetMarkSuffixes(key, out string[] suffixes))
			{
				return Array.IndexOf(suffixes, suffix) >= 0;
			}
			return false;
		}

		public bool TryGetMarkSuffixes(string key, out string[] suffixes)
		{
			if (_marks.TryGetValue(key, out List<string> results))
			{
				suffixes = results.ToArray();
				return true;
			}
			suffixes = new string[] { };
			return false;
		}

		public void RemoveMark(string key, string suffix = null)
		{
			if (_marks.TryGetValue(key, out List<string> values))
			{
				if (string.IsNullOrEmpty(suffix))
				{
					_marks.Remove(key);
				}
				else
				{
					values.Remove(suffix);
					if (values.Count == 0)
					{
						_marks.Remove(key);
					}
				}
			}
		}

		public void SetBool(string key, bool value)
		{
			TrySetData(key, value);
		}

		public bool GetBool(string key)
		{
			return GetData(key, false);
		}

		public void SetString(string key, string value)
		{
			TrySetData(key, value);
		}

		public bool TryGetString(string key, out string value)
		{
			return TryGetData(key, out value);
		}

		public string GetString(string key)
		{
			return GetData(key, string.Empty);
		}

		public void Remove(string key)
		{
			_dataMap.Remove(key);
		}

		public bool ContainsData(string key)
		{
			return _dataMap.ContainsKey(key);
		}

		public string[] GetMarkKeys()
		{
			return _marks.Keys.ToArray();
		}

		public string[] GetTags()
		{
			return _tags.ToArray();
		}

		public IDictionary<string, object> GetDataMapInternal()
		{
			return _dataMap;
		}

		public void Dispose()
		{
			_dataMap.Clear();
			_marks.Clear();
		}

		#endregion

		#region Private Methods

		private bool TrySetData(string key, object obj)
		{
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}

			_dataMap[key] = obj;
			return true;
		}

		private bool TryGetData<T>(string key, out T obj)
		{
			if (string.IsNullOrEmpty(key))
			{
				obj = default;
				return false;
			}

			if (_dataMap.TryGetValue(key, out object o) && o is T castedO)
			{
				obj = castedO;
				return true;
			}

			obj = default;
			return false;
		}

		private T GetData<T>(string key, T defaultValue)
		{
			if (TryGetData(key, out T obj))
			{
				return obj;
			}
			return defaultValue;
		}

		#endregion
	}
}
