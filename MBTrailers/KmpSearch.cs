using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebProxy {
    public static class KmpSearch {

        public static int IndexOf(byte[] data, byte[] pattern) {
            int[] failure = ComputeFailure(pattern);

            int j = 0;
            if (data.Length == 0) return -1;

            for (int i = 0; i < data.Length; i++) {
                while (j > 0 && pattern[j] != data[i]) {
                    j = failure[j - 1];
                }
                if (pattern[j] == data[i]) { j++; }
                if (j == pattern.Length) {
                    return i - pattern.Length + 1;
                }
            }
            return -1;
        }


        private static int[] ComputeFailure(byte[] pattern) {
            int[] failure = new int[pattern.Length];

            int j = 0;
            for (int i = 1; i < pattern.Length; i++) {
                while (j > 0 && pattern[j] != pattern[i]) {
                    j = failure[j - 1];
                }
                if (pattern[j] == pattern[i]) {
                    j++;
                }
                failure[i] = j;
            }

            return failure;
        }
    }
}
