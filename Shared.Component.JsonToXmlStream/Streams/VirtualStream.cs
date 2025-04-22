using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Shared.Component.Streams
{
    public class VirtualStream : Stream, ICloneable
    {
        private object SyncRoot = new object();
        private const int THRESHOLD_MAX = 10485760;
        private const int DefaultSize = 10240;
        private Stream WrappedStream;
        private bool IsDisposed;
        private bool IsInMemory;
        private int ThreshholdSize;
        private VirtualStream.MemoryFlag MemoryStatus;

        public VirtualStream()
          : this(10240, VirtualStream.MemoryFlag.AutoOverFlowToDisk, (Stream)new MemoryStream())
        {
        }

        public VirtualStream(int bufferSize)
          : this(bufferSize, VirtualStream.MemoryFlag.AutoOverFlowToDisk, (Stream)new MemoryStream(bufferSize))
        {
        }

        public VirtualStream(int bufferSize, int thresholdSize)
          : this(bufferSize, thresholdSize, VirtualStream.MemoryFlag.AutoOverFlowToDisk, (Stream)new MemoryStream(bufferSize))
        {
        }

        public VirtualStream(VirtualStream.MemoryFlag flag)
          : this(10240, flag, flag == VirtualStream.MemoryFlag.OnlyToDisk ? VirtualStream.CreatePersistentStream(10240) : (Stream)new MemoryStream())
        {
        }

        public VirtualStream(int bufferSize, VirtualStream.MemoryFlag flag)
          : this(bufferSize, flag, flag == VirtualStream.MemoryFlag.OnlyToDisk ? VirtualStream.CreatePersistentStream(bufferSize) : (Stream)new MemoryStream(bufferSize))
        {
        }

        public VirtualStream(Stream dataStream)
          : this(10240, VirtualStream.MemoryFlag.AutoOverFlowToDisk, dataStream)
        {
        }

        private VirtualStream(int bufferSize, VirtualStream.MemoryFlag flag, Stream dataStream)
          : this(bufferSize, bufferSize, flag, dataStream)
        {
        }

        private VirtualStream(int bufferSize, int thresholdSize, VirtualStream.MemoryFlag flag, Stream dataStream)
        {
            if (dataStream == null)
                throw new ArgumentNullException(nameof(dataStream));
            this.IsInMemory = flag != VirtualStream.MemoryFlag.OnlyToDisk;
            this.MemoryStatus = flag;
            bufferSize = Math.Min(bufferSize, 10485760);
            this.ThreshholdSize = thresholdSize;
            this.WrappedStream = !this.IsInMemory ? (Stream)new BufferedStream(dataStream, bufferSize) : dataStream;
            this.IsDisposed = false;
        }

        public object Clone()
        {
            if (this.IsInMemory)
            {
                Stream stream = (Stream)new MemoryStream((int)this.WrappedStream.Length);
                this.CopyStreamContentHelper(this.WrappedStream, stream);
                stream.Position = 0L;
                return (object)new VirtualStream(this.ThreshholdSize, VirtualStream.MemoryFlag.AutoOverFlowToDisk, stream);
            }
            Stream persistentStream = VirtualStream.CreatePersistentStream(this.ThreshholdSize);
            this.CopyStreamContentHelper(this.WrappedStream, persistentStream);
            persistentStream.Position = 0L;
            return (object)new VirtualStream(this.ThreshholdSize, VirtualStream.MemoryFlag.OnlyToDisk, persistentStream);
        }

        public override bool CanRead
        {
            get
            {
                return this.WrappedStream.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.WrappedStream.CanWrite;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return this.WrappedStream.CanSeek;
            }
        }

        public override long Length
        {
            get
            {
                return this.WrappedStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.WrappedStream.Position;
            }
            set
            {
                this.WrappedStream.Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
        {
            this.ThrowIfDisposed();
            this.WrappedStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();
            return this.WrappedStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.ThrowIfDisposed();
            return this.WrappedStream.Seek(offset, origin);
        }

        public override void SetLength(long length)
        {
            this.ThrowIfDisposed();
            if (this.MemoryStatus == VirtualStream.MemoryFlag.AutoOverFlowToDisk && this.IsInMemory && length > (long)this.ThreshholdSize)
            {
                Stream persistentStream = VirtualStream.CreatePersistentStream(this.ThreshholdSize);
                this.CopyStreamContent((MemoryStream)this.WrappedStream, persistentStream);
                this.WrappedStream = persistentStream;
                this.IsInMemory = false;
                this.WrappedStream.SetLength(length);
            }
            else
                this.WrappedStream.SetLength(length);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();
            if (this.MemoryStatus == VirtualStream.MemoryFlag.AutoOverFlowToDisk && this.IsInMemory && (long)count + this.WrappedStream.Position > (long)this.ThreshholdSize)
            {
                Stream persistentStream = VirtualStream.CreatePersistentStream(this.ThreshholdSize);
                this.CopyStreamContent((MemoryStream)this.WrappedStream, persistentStream);
                this.WrappedStream = persistentStream;
                this.IsInMemory = false;
                this.WrappedStream.Write(buffer, offset, count);
            }
            else
                this.WrappedStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing || this.IsDisposed)
                    return;
                this.Cleanup();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public Stream UnderlyingStream
        {
            get
            {
                return this.WrappedStream;
            }
        }

        private void Cleanup()
        {
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;
            if (this.WrappedStream == null)
                return;
            this.WrappedStream.Close();
            this.WrappedStream = (Stream)null;
        }

        private void CopyStreamContent(MemoryStream source, Stream target)
        {
            if (source.Length < (long)int.MaxValue)
            {
                long position = source.Position;
                target.Write(source.GetBuffer(), 0, (int)source.Length);
                target.Position = position;
            }
            else
                this.CopyStreamContentHelper((Stream)source, target);
        }

        private void CopyStreamContentHelper(Stream source, Stream target)
        {
            long position = source.Position;
            source.Position = 0L;
            byte[] buffer = new byte[this.ThreshholdSize];
            int count;
            while ((count = source.Read(buffer, 0, this.ThreshholdSize)) != 0)
                target.Write(buffer, 0, count);
            target.Position = position;
        }

        private void ThrowIfDisposed()
        {
            if (this.IsDisposed || this.WrappedStream == null)
                throw new ObjectDisposedException(nameof(VirtualStream));
        }

        internal static Stream CreatePersistentStream()
        {
            return VirtualStream.CreatePersistentStream(10240);
        }

        internal static Stream CreatePersistentStream(int size)
        {
            StringBuilder stringBuilder = new StringBuilder(261);
            Guid guid = Guid.NewGuid();
            stringBuilder.Append(Path.Combine(Path.GetTempPath(), "VST" + guid.ToString() + ".tmp"));
            return (Stream)new FileStream(new SafeFileHandle(VirtualStream.CreateFile(stringBuilder.ToString(), 3U, 0U, IntPtr.Zero, 2U, 67109120U, IntPtr.Zero), true), FileAccess.ReadWrite, size);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateFile(string name, uint accessMode, uint shareMode, IntPtr security, uint createMode, uint flags, IntPtr template);

        public enum MemoryFlag
        {
            AutoOverFlowToDisk,
            OnlyInMemory,
            OnlyToDisk,
        }
    }
}
