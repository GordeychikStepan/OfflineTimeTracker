using System;

namespace OfflineTimeTracker
{
    public class TaskEntry
    {
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }

        public override string ToString()
        {
            return $"{Description} | {StartTime:G} - {EndTime:G} | Длительность: {Duration}";
        }
    }
}
