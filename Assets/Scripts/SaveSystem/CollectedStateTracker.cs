using System.Collections.Generic;

namespace SaveSystem
{
    public static class CollectedStateTracker
    {
        private static readonly HashSet<string> collectedObjectIds = new HashSet<string>();

        public static void MarkCollected(string objectId)
        {
            if (!string.IsNullOrWhiteSpace(objectId))
            {
                collectedObjectIds.Add(objectId);
            }
        }

        public static string[] GetCollectedObjectIds()
        {
            string[] ids = new string[collectedObjectIds.Count];
            collectedObjectIds.CopyTo(ids);
            return ids;
        }

        public static void SetCollectedObjectIds(string[] objectIds)
        {
            collectedObjectIds.Clear();
            if (objectIds == null)
            {
                return;
            }

            foreach (string objectId in objectIds)
            {
                MarkCollected(objectId);
            }
        }

        public static void Clear()
        {
            collectedObjectIds.Clear();
        }
    }
}
