using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter is a way to execute code against a file as it moves to and from the git
    /// repository and into the working directory. 
    /// </summary>
    public abstract class Filter : IEquatable<Filter>
    {
        private static readonly LambdaEqualityHelper<Filter> equalityHelper =
        new LambdaEqualityHelper<Filter>(x => x.Name, x => x.Attributes);

        private readonly string name;
        private readonly IEnumerable<FilterAttributeEntry> attributes;

        private readonly GitFilter gitFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively.
        /// <param name="name">The unique name with which this filtered is registered with</param>
        /// <param name="attributes">A list of attributes which this filter applies to</param>
        /// </summary>
        protected Filter(string name, IEnumerable<FilterAttributeEntry> attributes)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(attributes, "attributes");

            this.name = name;
            this.attributes = attributes;
            var attributesAsString = string.Join(",", this.attributes.Select(attr => attr.FilterDefinition));

            gitFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributesAsString),
                init = InitializeCallback,
                stream = StreamCallback,
            };
        }

        private GitWriteStream thisStream;
        private GitWriteStream nextStream;
        private IntPtr thisPtr;
        private IntPtr nextPtr;
        private FilterSource filterSource;

        /// <summary>
        /// The name that this filter was registered with
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// The filter filterForAttributes.
        /// </summary>
        public IEnumerable<FilterAttributeEntry> Attributes
        {
            get { return attributes; }
        }

        /// <summary>
        /// The marshalled filter
        /// </summary>
        internal GitFilter GitFilter
        {
            get { return gitFilter; }
        }

        /// <summary>
        /// Complete callback on filter
        /// 
        /// This optional callback will be invoked when the upstream filter is
        /// closed. Gives the filter a change to perform any final actions or
        /// necissary clean up.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="output">Output to the downstream filter or output writer</param>
        /// <returns></returns>
        protected virtual int Complete(string path, string root, Stream output)
        {
            return 0;
        }

        /// <summary>
        /// Initialize callback on filter
        ///
        /// Specified as `filter.initialize`, this is an optional callback invoked
        /// before a filter is first used.  It will be called once at most.
        ///
        /// If non-NULL, the filter's `initialize` callback will be invoked right
        /// before the first use of the filter, so you can defer expensive
        /// initialization operations (in case the library is being used in a way
        /// that doesn't need the filter.
        /// </summary>
        protected virtual int Initialize()
        {
            return 0;
        }

        /// <summary>
        /// Clean the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="input">Input from the upstream filter or input reader</param>
        /// <param name="output">Output to the downstream filter or output writer</param>
        /// <returns>0 if successful and <see cref="GitErrorCode.PassThrough"/> to skip and pass through</returns>
        protected virtual int Clean(string path, string root, Stream input, Stream output)
        {
            return (int)GitErrorCode.PassThrough;
        }

        /// <summary>
        /// Smudge the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="input">Input from the upstream filter or input reader</param>
        /// <param name="output">Output to the downstream filter or output writer</param>
        /// <returns>0 if successful and <see cref="GitErrorCode.PassThrough"/> to skip and pass through</returns>
        protected virtual int Smudge(string path, string root, Stream input, Stream output)
        {
            return (int)GitErrorCode.PassThrough;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Filter"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Filter"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="Filter"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Filter);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Filter"/> is equal to the current <see cref="Filter"/>.
        /// </summary>
        /// <param name="other">The <see cref="Filter"/> to compare with the current <see cref="Filter"/>.</param>
        /// <returns>True if the specified <see cref="Filter"/> is equal to the current <see cref="Filter"/>; otherwise, false.</returns>
        public bool Equals(Filter other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Filter"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Filter"/> to compare.</param>
        /// <param name="right">Second <see cref="Filter"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Filter left, Filter right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Filter"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Filter"/> to compare.</param>
        /// <param name="right">Second <see cref="Filter"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Filter left, Filter right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Initialize callback on filter
        ///
        /// Specified as `filter.initialize`, this is an optional callback invoked
        /// before a filter is first used.  It will be called once at most.
        ///
        /// If non-NULL, the filter's `initialize` callback will be invoked right
        /// before the first use of the filter, so you can defer expensive
        /// initialization operations (in case libgit2 is being used in a way that doesn't need the filter).
        /// </summary>
        int InitializeCallback(IntPtr filterPointer)
        {
            return Initialize();
        }

        unsafe int StreamCallback(out IntPtr git_writestream_out, GitFilter self, IntPtr payload, IntPtr filterSourcePtr, IntPtr git_writestream_next)
        {
            if (filterSourcePtr == IntPtr.Zero)
            {
                throw new ArgumentNullException("filterSourcePtr");
            }
            if (git_writestream_next == IntPtr.Zero)
            {
                throw new ArgumentNullException("git_writestream_next");
            }

            thisStream = new GitWriteStream();
            thisStream.close = StreamCloseCallback;
            thisStream.write = StreamWriteCallback;
            thisStream.free = StreamFreeCallback;
            thisPtr = Marshal.AllocHGlobal(Marshal.SizeOf(thisStream));
            Marshal.StructureToPtr(thisStream, thisPtr, false);
            nextPtr = git_writestream_next;
            nextStream = new GitWriteStream();
            Marshal.PtrToStructure(nextPtr, nextStream);
            filterSource = FilterSource.FromNativePtr(filterSourcePtr);

            git_writestream_out = thisPtr;

            return 0;
        }

        unsafe int StreamCloseCallback(IntPtr stream)
        {
            int result = 0;

            if (stream == IntPtr.Zero)
            {
                throw new ArgumentNullException("stream");
            }
            if (stream != thisPtr)
            {
                throw new ArgumentException("Unexpected stream pointer", "stream");
            }

            using (MemoryStream output = new MemoryStream())
            {
                Ensure.ZeroResult(result = this.Complete(filterSource.Path, filterSource.Root, output));
                result = WriteToNextFilter(output);
            }

            return nextStream.close(nextPtr);
        }

        unsafe void StreamFreeCallback(IntPtr stream)
        {
            if (stream == IntPtr.Zero)
                throw new ArgumentNullException("stream");
            if (stream != thisPtr)
                throw new ArgumentException("unexpected stream ptr");

            Marshal.FreeHGlobal(thisPtr);
        }

        unsafe int StreamWriteCallback(IntPtr stream, IntPtr buffer, uint len)
        {
            int result = 0;

            if (stream == IntPtr.Zero)
            {
                throw new ArgumentNullException("stream");
            }
            if (buffer == IntPtr.Zero)
            {
                throw new ArgumentNullException("buffer");
            }
            if (stream != thisPtr)
            {
                throw new ArgumentException("Unexpected GitWriteStream", "stream");
            }

            using (UnmanagedMemoryStream input = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), len))
            using (MemoryStream output = new MemoryStream())
            {
                switch (filterSource.SourceMode)
                {
                    case FilterMode.Clean:
                        result = Clean(filterSource.Path, filterSource.Root, input, output);
                        break;
                    case FilterMode.Smudge:
                        result = Smudge(filterSource.Path, filterSource.Root, input, output);
                        break;
                    default:
                        Proxy.giterr_set_str(GitErrorCategory.Filter, "Unexpected filter mode.");
                        return (int)GitErrorCode.Ambiguous;
                }

                if (result == (int)GitErrorCode.PassThrough)
                {
                    input.CopyTo(output);
                }
                else if (result < 0)
                {
                    return result;
                }

                result = WriteToNextFilter(output);
            }

            return result;
        }

        private unsafe int WriteToNextFilter(MemoryStream output)
        {
            const int BufferSize = 64 * 1024; // 64K is optimal buffer size per https://technet.microsoft.com/en-us/library/cc938632.aspx

            int result = 0;
            byte[] bytes = new byte[BufferSize];
            IntPtr bytesPtr = Marshal.AllocHGlobal(BufferSize);
            try
            {
                output.Seek(0, SeekOrigin.Begin);

                int read = 0;
                while ((read = output.Read(bytes, 0, bytes.Length)) > 0)
                {
                    Marshal.Copy(bytes, 0, bytesPtr, read);
                    if ((result = nextStream.write(nextPtr, bytesPtr, (uint)read)) < 0)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Filter, "Filter write to next stream failed");
                        break;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bytesPtr);
            }

            return result;
        }
    }
}
