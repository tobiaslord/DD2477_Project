
using System.Collections.Concurrent;

namespace Crawler;
    class Tracker {
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private List<DateTime> requests = new List<DateTime>();
        private DateTime? firstRequest;
        private int totalCount = 0;
        public async void OnRequest() {
            await this.semaphore.WaitAsync();
            try {
                if (this.requests.Count == 300)
                    this.requests.RemoveAt(299);

                var date = DateTime.UtcNow;
                if (this.firstRequest == null)
                    this.firstRequest = date;

                this.totalCount++;
                this.requests.Insert(0, date);
            }
            finally {
                this.semaphore.Release();
            }
        }
        public double PrintRollingAverage(int seconds) {
            if (this.firstRequest == null)
                return 0;

            var until = DateTime.UtcNow.AddSeconds(seconds);

            int count = 0;
            DateTime lastInSeq = until;
            foreach (var requestTime in this.requests) {
                if (requestTime > until) break;

                lastInSeq = requestTime;
                count++;
            }
            var windowDiff = getSecondsDiff(DateTime.UtcNow, lastInSeq);
            var totalDiff = getSecondsDiff(DateTime.UtcNow, (DateTime)this.firstRequest);
            var avg = totalDiff / this.totalCount;
            Console.WriteLine($"{count} last {windowDiff} seconds, avg {windowDiff / 300}");
            Console.WriteLine($"{this.totalCount} last {totalDiff} seconds, avg {avg}");

            return avg;
        }
        private double getSecondsDiff(DateTime dt1, DateTime dt2) {
            var span = dt1 - dt2;
            return span.TotalSeconds;
        }
        public void PrintStatus(ConcurrentQueue<string> queue, int nth) {
            if (queue.Count % nth == 0) {
                var queueCount = queue.Count;
                Console.WriteLine(queueCount + " nodes left");
                var avg = this.PrintRollingAverage(30);
                var timeLeft = avg * queueCount;
                Console.WriteLine($"{timeLeft} seconds left");
            }
        }
    }