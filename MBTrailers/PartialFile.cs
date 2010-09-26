using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WebProxy {
    public class PartialFile : IDisposable {
        
        struct PartialChunk {
            public int StartPos { get; set; }
            public byte[] Data { get; set; }
        }

        static readonly byte[] BITMAP_MARKER = new Byte[] { 0x49, 0x53, 0x10, 0x5e, 0xb5, 0xf1, 0x49, 0x48, 0x95, 0xd6, 0xca, 0xc3, 0x24, 0xa7, 0x35, 0x1e};

        public const int CHUNK_SIZE = 50 * 1024;
        public const int DEFAULT_SIZE = 10 * 1000 * 1024; 

        List<PartialChunk> partialData = new List<PartialChunk>();
        System.Collections.BitArray completedChunks;
        int targetSize; 
        bool isCompleted = false;
        string path;

        Stream stream; 
        
        public PartialFile(string path) : this(path, -1) { }

        public PartialFile(string path, int targetSize) {
            this.path = path;

            if (File.Exists(path)) {
                isCompleted = true;

                if (targetSize != -1 && new FileInfo(path).Length != targetSize) {
                    throw new ApplicationException("Attempting to initialize the size with an invalid size");
                }
                stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);


            } else if (File.Exists(PartialFileName)) {

                stream = File.Open(PartialFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                ReadBitmapInfo(); 

            } else { 
                if (targetSize == -1) targetSize = DEFAULT_SIZE;
                var chunks = targetSize / CHUNK_SIZE;
                
                if (targetSize % CHUNK_SIZE > 0) chunks++;

                completedChunks = new System.Collections.BitArray(chunks, false);

                stream = File.Open(PartialFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                WriteBitmapInfo();
            }
        }

        private void WriteBitmapInfo() {
            byte[] buffer = new byte[(completedChunks.Length / 8) + (completedChunks.Length % 8 > 0 ? 1 : 0)];
            completedChunks.CopyTo(buffer, 0); 
            stream.SetLength(targetSize + BITMAP_MARKER.Length + buffer.Length);
            stream.Seek(-(BITMAP_MARKER.Length + buffer.Length), SeekOrigin.End);
            stream.Write(BITMAP_MARKER, 0, BITMAP_MARKER.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        private void ReadBitmapInfo() {

            // read last chunk_size and seek to marker
            // may fail if chunks are tiny

            stream.Seek(-CHUNK_SIZE, SeekOrigin.End);
            byte[] buffer = new byte[CHUNK_SIZE];

            int pos = 0, bytes_read = 0;
            do {
                bytes_read = stream.Read(buffer, pos, CHUNK_SIZE);
                pos += bytes_read;
            } while (bytes_read > 0);

            var markerPos = KmpSearch.IndexOf(buffer, BITMAP_MARKER);
            if (markerPos == -1) {
                throw new ApplicationException("Missing bitmap header from partial file");
            }

            markerPos += BITMAP_MARKER.Length;
            completedChunks = new System.Collections.BitArray(buffer.Skip(markerPos).ToArray()); 
        }


        public void Write(byte[] data, int startPos) {
            if (isCompleted) {
                throw new ApplicationException("File is already fully downloaded!"); 
            }

            partialData.Add(new PartialChunk() {StartPos = startPos, Data = data.ToArray()});
            CommitChunks();
        }

        
        public int Read(int startPos, int size, byte[] buffer) {
            int[] chunks = LocateChunks(startPos, size);

            if (!isCompleted) {
                // naive for now 
                if (chunks.Any( c => !completedChunks[c])) {
                    throw new ApplicationException("Asking for data that is not fully downloaded");
                } 
            }

            stream.Seek(startPos, SeekOrigin.Begin);
            var bytes_read = 0;
            do {
                bytes_read = stream.Read(buffer, 0, size);
                size -= bytes_read;
            } while (bytes_read > 0 && size > 0);

            return size;
        }

        private int[] LocateChunks(int pos, int size) {
            var first_chunk = pos / CHUNK_SIZE;
            var last_chunk = (pos + size) / CHUNK_SIZE;
            return Enumerable.Range(first_chunk, (last_chunk - first_chunk) + 1).ToArray();
        }

        private void CommitChunks() {

            // compress, join all ranges ... 


            int current_chunk = -1;
            var chunks_to_join = new List<PartialChunk>(); 
            foreach (var chunk in partialData.OrderBy(_ => _.StartPos)) {
                if (current_chunk == -1) {
                    current_chunk = chunk.StartPos / CHUNK_SIZE;
                    if (chunk.StartPos != current_chunk * CHUNK_SIZE) {
                        if (chunk.StartPos + chunk.Data.Length > CHUNK_SIZE * (current_chunk + 1)) {
                            current_chunk++;
                        } else {
                            current_chunk = -1;
                        }
                    }

                    if (current_chunk != -1) {
                        chunks_to_join.Add(chunk);
                    }
                } else {

                    throw new NotImplementedException();
                
                }


            }
        }


        public bool IsCompleted {
            get {
                return isCompleted;
            }
        }

        bool disposed; 
        public void Dispose() {
            if (!disposed) {
                stream.Dispose();
                disposed = true;
            }
        }


        private void InitPartialFile() {

        }

        private string PartialFileName {
            get {
                return path + ".partial";
            }
        }

    }
}
