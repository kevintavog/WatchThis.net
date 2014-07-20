using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WatchThis.Utilities
{
	static public class FileEnumerator
	{
		static public void AddFilenames(Queue<string> fileQueue, string path, Func<string,bool> accept = null)
		{
            AddFilenames(
                (s) => fileQueue.Enqueue(s),
                path,
                accept);
        }

        static public void AddFilenames(Action<string> add, string path, Func<string,bool> accept = null)
        {
			if (!Directory.Exists(path))
			{
				throw new ArgumentException(string.Format("{0} does not exist", path));
			}

			var queue = new Queue<string>();
			queue.Enqueue(path);
			while (queue.Count > 0)
			{
				path = queue.Dequeue();
				string[] files = null;
				try
				{
					files = Directory.GetFiles(path);
				}
				catch (Exception)
				{
					// Purposefully eat, do what we can
				}

				if (files != null)
				{
					foreach (var f in files)
					{
						if (accept == null || accept(f))
						{
                            add(f);
						}
					}
				}

				try
				{
					foreach (var dir in Directory.GetDirectories(path))
					{
						queue.Enqueue(dir);
					}
				}
				catch (Exception)
				{
					// Purposefully eat, do what we can
				}
			}
		}
	}
}

