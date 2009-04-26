﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Logging {
    public struct LogRow {

        const string TimePattern = "h:mm:ss.fff tt d/M/yyyy";


        public LogSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public int ThreadId { get; set; }
        public string ThreadName { get; set; }
        public DateTime Time { get; set; }

        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            builder.Append(Time.ToString(TimePattern))
                .Append(" , ")
                .Append(Enum.GetName(typeof(LogSeverity), Severity))
                .Append(" , ")
                .Append(Encode(Message))
                .Append(" , ")
                .Append(Encode(Category))
                .Append(" , ")
                .Append(ThreadId)
                .Append(" , ")
                .Append(Encode(ThreadName));
            return builder.ToString();
        }

        private string Encode(string str) {
            return (str ?? "").Replace(",", ",,").Replace(Environment.NewLine, " [n] ");
        }

        public static LogRow FromString(string message) {
            var split = splitString(message);
            return new LogRow()
            {
                Time = DateTime.ParseExact(split[0], TimePattern, null),
                Severity = (LogSeverity)Enum.Parse(typeof(LogSeverity), split[1]),
                Message = split[2],
                Category = split[3],
                ThreadId = int.Parse(split[4]), 
                ThreadName = split[5]
            };
        }

        static string[] splitString(string message) {
            List<string> items = new List<string>();
            bool gotComma = false;

            StringBuilder currentItem = new StringBuilder();

            foreach (var chr in message) {

                if (chr == ',' && gotComma) {
                    gotComma = false;
                    currentItem.Append(',');
                } else if (chr == ',') {
                    gotComma = true;
                } else if (gotComma) {
                    items.Add(currentItem.ToString().Replace(" [n] ", Environment.NewLine).Trim());
                    currentItem = new StringBuilder();
                    gotComma = false;
                } else {
                    currentItem.Append(chr);
                }

            }
            items.Add(currentItem.ToString().Replace("[n]", Environment.NewLine).Trim());
            return items.ToArray();
        }
    }
}
