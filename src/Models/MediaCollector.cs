using System;
using System.Collections.Generic;
using NLog;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace WatchThis.Models
{
    public class MediaCollector
    {
        public Queue<MediaItem> Queue { get; private set; }
        public IList<MediaItem> MediaList { get; private set; }
        public Action ItemsAvailable { get; set; }

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Task[] _tasks;
        private bool _waiting = false;
        private IDictionary<string,MediaItem> _signatures = new ConcurrentDictionary<string, MediaItem>();
        private IDictionary<long,MediaItem> _lengthItem = new Dictionary<long, MediaItem>();
        private long _duplicatesRemoved;
        private long _questionableDuplicates;
        private long _primarySignaturesCalculated;
        private long _secondarySignaturesCalculated;

        public MediaCollector(List<MediaItem> mediaList)
        {
            Queue = new Queue<MediaItem>();
            MediaList = mediaList;
        }

        public void AddItem(MediaItem item)
        {
            Queue.Enqueue(item);
        }

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
                        while (!_waiting || Queue.Count > 0)
                        {
                            MediaItem item = null;
                            lock (Queue)
                            {
                                if (Queue.Count > 0)
                                {
                                    item = Queue.Dequeue();
                                }
                            }

                            if (null != item)
                            {
                                HandleItem(item);
                            }
                            else
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception MediaCollector: {0}", ex);
                    }
                })
                .ContinueWith( (t) => logger.Info("Completed media collector task {0}", t.IsCompleted) );
            }
        }

        private void HandleItem(MediaItem item)
        {
            var fileLength = item.Length;

            MediaItem prior = null;
            lock(_lengthItem)
            {
                if (_lengthItem.ContainsKey(fileLength))
                {
                    prior = _lengthItem[fileLength];
                }
                else
                {
                    _lengthItem[fileLength] = item;
                }
            }

            // If there's a possible match, calculate & compare signatures
            if (prior != null)
            {
                if (!prior.HasSignature)
                {
                    Interlocked.Increment(ref _primarySignaturesCalculated);
                    _signatures[prior.Signature] = prior;
                }

                Interlocked.Increment(ref _secondarySignaturesCalculated);
                if (_signatures.ContainsKey(item.Signature))
                {
                    Interlocked.Increment(ref _duplicatesRemoved);

                    var priorItem = _signatures[item.Signature];
                    if (!item.Identifier.Equals(prior.Identifier) || !item.CreatedDate.Equals(prior.CreatedDate))
                    {
                        Interlocked.Increment(ref _questionableDuplicates);
                    }
                    return;
                }
                else
                {
                    _signatures[item.Signature] = item;
                }
            }

            bool wasEmpty = false;
            lock (MediaList)
            {
                wasEmpty = MediaList.Count < 1;
                MediaList.Add(item);
            }

            if (wasEmpty && ItemsAvailable != null)
            {
                ItemsAvailable();
            }
        }
    }
}
