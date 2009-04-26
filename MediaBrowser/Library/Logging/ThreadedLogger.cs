﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MediaBrowser.Library.Logging {
    public abstract class ThreadedLogger : Logger {


        Thread loggingThread;
        Queue<Action> queue = new Queue<Action>();
        AutoResetEvent hasNewItems = new AutoResetEvent(false);
        bool waiting = false;

        public ThreadedLogger() : base() {
            loggingThread = new Thread(new ThreadStart(ProcessQueue));
            loggingThread.IsBackground = true;
            loggingThread.Start();
        }


        void ProcessQueue() {
            while (true) {
                waiting = true;
                hasNewItems.WaitOne(10000);
                waiting = false;

                Queue<Action> queueCopy;
                lock (queue) {
                    queueCopy = new Queue<Action>(queue);
                    queue.Clear();
                }

                foreach (var log in queueCopy) {
                    log();
                }
            }
        }

        public override void LogMessage(LogRow row) {
            lock (queue) {
                queue.Enqueue(() => AsyncLogMessage(row));
            }
            hasNewItems.Set();
        }

        protected abstract void AsyncLogMessage(LogRow row);
        

        public override void Flush() {
            while (!waiting) {
                Thread.Sleep(1);
            }
        }
    }
}
