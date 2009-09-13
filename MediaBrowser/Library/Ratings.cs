using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;

namespace MediaBrowser.Library
{
    public class Ratings
    {
        private Dictionary<string, int> ratings = new Dictionary<string, int>();
        private Dictionary<int, string> ratingsStrings = new Dictionary<int, string>();

        public Ratings(bool blockUnrated)
        {
            this.Initialize(blockUnrated);
        }

        public Ratings()
        {
            this.Initialize(false);
        }

        public void Initialize(bool blockUnrated)
        {
            // construct ratings dict
            if (blockUnrated)
            {
                ratings.Add("", 5);
            }
            else
            {
                ratings.Add("", 0);
            }
            ratings.Add("G", 1);
            ratings.Add("TV-G", 1);
            ratings.Add("TV-Y", 1);
            ratings.Add("TV-Y7", 1);
            ratings.Add("PG", 2);
            ratings.Add("TV-PG", 2);
            ratings.Add("PG-13", 3);
            ratings.Add("TV-14", 3);
            ratings.Add("R", 4);
            ratings.Add("TV-MA", 4);
            ratings.Add("NC-17", 5);
            ratings.Add("UR", 5);
            ratings.Add("NR", 5);
            ratings.Add("X", 10);
            ratings.Add("XXX", 100);
            ratings.Add("CS", 1000);
            //and rating reverse lookup dictionary (don't need the unrated or redundant ones)
            ratingsStrings.Add(1, "G");
            ratingsStrings.Add(2, "PG");
            ratingsStrings.Add(3, "PG-13");
            ratingsStrings.Add(4, "R");
            ratingsStrings.Add(5, "NC-17");
            ratingsStrings.Add(999, "CS"); //this is different because we want Custom to be protected, not allowed

            return;
        }

        public void SwitchUnrated(bool block)
        {
            ratings.Remove("");
            if (block)
            {
                ratings.Add("", 5);
            }
            else
            {
                ratings.Add("", 0);
            }
        }
        public int Level(string ratingStr)
        {
            if (ratings.ContainsKey(ratingStr))
                return ratings[ratingStr];
            else return -1;
        }
        public string ToString(int level)
        {
            if (ratingsStrings.ContainsKey(level))
                return ratingsStrings[level];
            else return null;
        }
        public IEnumerable<string> ToString()
        {
            //return the whole list of ratings strings
            return ratingsStrings.Values;
        }

    }
}
