using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace FileHash
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var start = Environment.TickCount;
			var filenames = new Queue<string>();
			var builder = new InfoBuilder { FilenameQueue = filenames, FileList = new List<string>(), ItemsAvailable = {} };
			builder.Start(1);
			FileEnumerator.AddFilenames(filenames, "/Users/goatboy/Pictures/Master/2012", (s) => SupportedExtension(Path.GetExtension(s)));

			builder.WaitForCompletion();
			var duration = Environment.TickCount - start;
			Console.WriteLine("Duration: {0}", duration);
		}

		static bool SupportedExtension(string extension)
		{
			return extension.Equals(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
					extension.Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
					extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase);
		}
	}
	class InfoBuilder
	{
		public Queue<string> FilenameQueue { get; set; }
		public IList<string> FileList { get; set; }
		public Action ItemsAvailable { get; set; }


		private Task[] _tasks;
		private bool _waiting = false;
		private IDictionary<string,string> _signatures = new Dictionary<string, string>();
		private IDictionary<long,FileSignature> _lengthPath = new Dictionary<long, FileSignature>();
		private long _duplicatesRemoved;
		private long _primarySignaturesCalculated;
		private long _secondarySignaturesCalculated;


		public void WaitForCompletion()
		{
			_waiting = true;
			Task.WaitAll(_tasks);
			Console.WriteLine(
				"Enumeration completed; {0} duplicates removed; {1} primary calculated; {2} secondary calculated",
				 _duplicatesRemoved,
				 _primarySignaturesCalculated,
				 _secondarySignaturesCalculated);
		}

		public void Start(int workers)
		{
			_tasks = new Task[workers];
			for (int idx = 0; idx < workers; ++idx)
			{
				_tasks[idx] = Task.Factory.StartNew( () =>
					{
						try
						{
							while (!_waiting || FilenameQueue.Count > 0)
							{
								string filename = null;
								lock (FilenameQueue)
								{
									if (FilenameQueue.Count > 0)
									{
										filename = FilenameQueue.Dequeue();
									}
								}

								if (null != filename)
								{
									var fileLength = new FileInfo(filename).Length;

									FileSignature prior = null;
									lock(_lengthPath)
									{
										if (_lengthPath.ContainsKey(fileLength))
										{
											prior = _lengthPath[fileLength];
										}
										else
										{
											_lengthPath[fileLength] = new FileSignature { Filename = filename };
										}
									}

									// If there's a possible match, calculate & compare signatures
									if (prior != null)
									{
										if (!prior.HasSignature)
										{
											Interlocked.Increment(ref _primarySignaturesCalculated);
											_signatures[prior.Signature()] = prior.Filename;
										}

										Interlocked.Increment(ref _secondarySignaturesCalculated);
										var fileSignature = new FileSignature { Filename = filename };
										if (_signatures.ContainsKey(fileSignature.Signature()))
										{
											Interlocked.Increment(ref _duplicatesRemoved);

											var priorFile = new FileInfo(_signatures[fileSignature.Signature()]);
											var curFile = new FileInfo(filename);
											if (!curFile.Name.Equals(priorFile.Name) || !curFile.CreationTime.Equals(priorFile.CreationTime))
											{
												Console.WriteLine("Duplicate: {0} (org: {1}); {2}={3}", 
													filename, priorFile.FullName, 
													fileSignature.Signature(), new FileSignature { Filename = priorFile.FullName}.Signature());
											}
//											Console.WriteLine("Duplicate: {0} (of {1})", filename, priorFile.FullName);
											continue;
										}
										else
										{
											_signatures[fileSignature.Signature()] = filename;
										}
									}

									bool wasEmpty = false;
									lock (FileList)
									{
										wasEmpty = FileList.Count < 1;
										FileList.Add(filename);
									}

									if (wasEmpty && ItemsAvailable != null)
									{
										Console.WriteLine("Some items available: {0} ...", FileList.Count);
										ItemsAvailable();
									}
								}
								else
								{
									Thread.Sleep(10);
								}
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine("Exception InfoBuilder: {0}", ex);
						}
					})
					.ContinueWith( (t) => Console.WriteLine("Completed info builder task {0}", t.IsCompleted) );
			}
		}		
	}


	class FileSignature
	{
		public bool HasSignature { get { return _signature != null; } }
		public string Filename { get; set; }
		private string _signature;

		public string Signature()
		{
			lock(this)
			{
				if (_signature == null)
				{
#if true
					var fi = new FileInfo(Filename);
					_signature = fi.Name + fi.Length;
#else
	#if true
					// Yes, if a file is less than the data read, we calculate the signature with a bunch
					// of zero byte values. It seems better than re-allocating the byte array to the proper
					// size in that case; the affect is the same, I claim.
					byte[] data = new byte[8 * 1024];
					using (var stream = File.OpenRead(Filename))
					{
						stream.Read(data, 0, data.Length);
					}
					_signature = BitConverter.ToString(SHA256.Create().ComputeHash(data));
	#else
					using (var stream = File.OpenRead(Filename))
					{
						_signature = BitConverter.ToString(SHA256.Create().ComputeHash(stream));
					}
	#endif
#endif
				}
				return _signature;
			}
		}
	}
}
