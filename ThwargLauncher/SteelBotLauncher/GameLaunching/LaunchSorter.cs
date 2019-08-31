﻿using System.Collections.Generic;
using System.Linq;

namespace SteelBotLauncher
{
    class LaunchSorter
    {
        /// <summary>
        /// List of launch items sorted for performance
        /// </summary>
        public class LaunchList
        {
            private readonly List<LaunchItem> _launchItems;
            public LaunchList() { _launchItems = new List<LaunchItem>(); }
            public void Add(LaunchItem item) { _launchItems.Add(item); }
            public IEnumerable<LaunchItem> GetLaunchList() { return _launchItems; }
            public int GetLaunchItemCount() { return _launchItems.Count; }
            public LaunchItem PopTop()
            {
                if (_launchItems.Count == 0)
                {
                    return null;
                }
                var poppedItem = _launchItems[0];
                _launchItems.RemoveAt(0);
                return poppedItem;
            }
            public void PushBottom(LaunchItem item) { Add(item); }
        }
        /// <summary>
        /// Construct a launch list from the model account info, and sort it for optimal performance
        /// </summary>
        public LaunchList SortLaunchList(LaunchList launchList)
        {
            var optimizedList = GetOptimizedLaunchList(launchList);
            return optimizedList;
        }
        /// <summary>
        /// Reorder launch items by starting with account with most launches
        /// and then rotating through accounts from most launches to least launches
        /// </summary>
        private LaunchList GetOptimizedLaunchList(LaunchList originalList)
        {
            // Bin all items by account
            Dictionary<string, List<LaunchItem>> launchItemsByAccountName = new Dictionary<string, List<LaunchItem>>();
            foreach (LaunchItem item in originalList.GetLaunchList())
            {
                string key = item.AccountName;
                if (!launchItemsByAccountName.ContainsKey(key))
                {
                    launchItemsByAccountName[key] = new List<LaunchItem>();
                }
                launchItemsByAccountName[key].Add(item);
            }
            // Sort bins first by priority and then by #items
            // (so if they all have same priority, it starts with most populous bins, as they involve most 5 minute delays)
            var sortedLists = launchItemsByAccountName.Values.OrderBy(x => x.First().Priority).ThenByDescending(x => x.Count).ToList();
            // Build list starting with most priority bin, and taking one per bin until all taken
            int i = 0;
            var sortedLaunchList = new LaunchList();
            while (sortedLists.Count > 0)
            {
                if (sortedLists[i].Count > 0)
                {
                    sortedLaunchList.Add(PopFirst(sortedLists[i]));
                }
                if (sortedLists[i].Count == 0)
                {
                    sortedLists.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
                if (i >= sortedLists.Count)
                {
                    i = 0;
                }
            }
            return sortedLaunchList;
        }
        /// <summary>
        /// Remove & return first item on list
        /// </summary>
        private LaunchItem PopFirst(List<LaunchItem> list)
        {
            var item = list[0];
            list.RemoveAt(0);
            return item;
        }
    }
}
