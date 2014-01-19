using System;
using System.Xml.Linq;

namespace WatchThis.Utilities
{
	static public class XElementExtensions
	{
#if false
		static public T Get<T>(this XElement ele, string attributeName, T defaultValue)
		{
			var attr = ele.Attribute(attributeName);
			if (attr == null)
			{
				return defaultValue;
			}
			return (T) attr;
		}
#endif
		static public string GetAttribute(this XElement ele, string attributeName, string defaultValue)
		{
			var attr = ele.Attribute(attributeName);
			if (attr == null)
			{
				return defaultValue;
			}
			return (string) attr;
		}

		static public bool GetAttribute(this XElement ele, string attributeName, bool defaultValue)
		{
			var attr = ele.Attribute(attributeName);
			if (attr == null)
			{
				return defaultValue;
			}
			return (bool) attr;
		}
		
		static public int GetAttribute(this XElement ele, string attributeName, int defaultValue)
		{
			var attr = ele.Attribute(attributeName);
			if (attr == null)
			{
				return defaultValue;
			}
			return (int) attr;
		}

		static public double GetAttribute(this XElement ele, string attributeName, double defaultValue)
		{
			var attr = ele.Attribute(attributeName);
			if (attr == null)
			{
				return defaultValue;
			}
			return (double) attr;
		}

		static public string GetValue(this XDocument doc, string elementName, string defaultValue)
		{
			return (string) doc.Root.Element(elementName) ?? defaultValue;
		}
	}
}

