using System;
using System.Collections.Generic;
using NLog;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace WatchThis.Models
{
	class InfoBuilder
	{
		public Queue<string> FilenameQueue { get; set; }
		public IList<ImageInformation> ImageList { get; set; }
		public Action ItemsAvailable { get; set; }


		private static Logger logger = LogManager.GetCurrentClassLogger();
		private Task[] _tasks;
		private bool _waiting = false;
		private IDictionary<string,string> _signatures = new ConcurrentDictionary<string, string>();
		private IDictionary<long,FileSignature> _lengthPath = new Dictionary<long, FileSignature>();
		private long _duplicatesRemoved;
		private long _questionableDuplicates;
		private long _primarySignaturesCalculated;
		private long _secondarySignaturesCalculated;


		public void WaitForCompletion()
		{
			_waiting = true;
			Task.WaitAll(_tasks);
			logger.Info(
				"Enumeration completed; {0} duplicates removed; {1} primary calculated; {2} secondary calculated; {3} questionable",
				_duplicatesRemoved,
				_primarySignaturesCalculated,
				_secondarySignaturesCalculated,
				_questionableDuplicates);
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
												Interlocked.Increment(ref _questionableDuplicates);
											}
											continue;
										}
										else
										{
											_signatures[fileSignature.Signature()] = filename;
										}
									}

									var info = ImageInformation.Get(filename);
									bool wasEmpty = false;
									lock (ImageList)
									{
										wasEmpty = ImageList.Count < 1;
										ImageList.Add(info);
									}

									if (wasEmpty && ItemsAvailable != null)
									{
										logger.Info("Some items available: {0} ...", ImageList.Count);
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
							logger.Error("Exception InfoBuilder: {0}", ex);
						}
					})
					.ContinueWith( (t) => logger.Info("Completed info builder task {0}", t.IsCompleted) );
			}
		}		
	}

}

