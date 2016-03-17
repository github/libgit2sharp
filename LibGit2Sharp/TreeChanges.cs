using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the result of a diff between two trees.
    /// <para>Changes at the granularity of the file can be obtained through the different sub-collections <see cref="Added"/>, <see cref="Deleted"/> and <see cref="Modified"/>.</para>
    /// <para>To obtain the actual patch of the diff, use the <see cref="Patch"/> class when calling Compare.</para>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TreeChanges : IEnumerable<TreeEntryChanges>, IDiffResult
    {
        private readonly DiffSafeHandle diff;
        private readonly Lazy<int> count;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected TreeChanges()
        { }

        internal TreeChanges(DiffSafeHandle diff)
        {
            this.diff = diff;
            this.count = new Lazy<int>(() => Proxy.git_diff_num_deltas(diff));
        }

        /// <summary>
        /// Enumerates the diff and yields deltas with the specified change kind.
        /// </summary>
        /// <param name="changeKind">Change type to filter on.</param>
        private IEnumerable<TreeEntryChanges> GetChangesOfKind(ChangeKind changeKind)
        {
            for (int i = 0; i < Count; i++)
            {
                var delta = Proxy.git_diff_get_delta(diff, i);

                if (TreeEntryChanges.GetStatusFromChangeKind(delta.Status) == changeKind)
                    yield return new TreeEntryChanges(delta);
            }
        }

        private static TreeEntryChanges ToTreeEntryChange(GitDiffDelta delta)
        {
            return new TreeEntryChanges(delta);
        }

        #region IEnumerable<TreeEntryChanges> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<TreeEntryChanges> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return new TreeEntryChanges(Proxy.git_diff_get_delta(diff, i));
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been been added.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Added
        {
            get { return GetChangesOfKind(ChangeKind.Added); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been deleted.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Deleted
        {
            get { return GetChangesOfKind(ChangeKind.Deleted); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been modified.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Modified
        {
            get { return GetChangesOfKind(ChangeKind.Modified); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which type have been changed.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> TypeChanged
        {
            get { return GetChangesOfKind(ChangeKind.TypeChanged); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which have been renamed
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Renamed
        {
            get { return GetChangesOfKind(ChangeKind.Renamed); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which have been copied
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Copied
        {
            get { return GetChangesOfKind(ChangeKind.Copied); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which are unmodified
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Unmodified
        {
            get { return GetChangesOfKind(ChangeKind.Unmodified); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which are conflicted
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Conflicted
        {
            get { return GetChangesOfKind(ChangeKind.Conflicted); }
        }

        /// <summary>
        /// Gets the number of <see cref="TreeEntryChanges"/> in this comparison.
        /// </summary>
        public virtual int Count
        {
            get { return count.Value; }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "+{0} ~{1} -{2} \u00B1{3} R{4} C{5}",
                                     Added.Count(),
                                     Modified.Count(),
                                     Deleted.Count(),
                                     TypeChanged.Count(),
                                     Renamed.Count(),
                                     Copied.Count());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            diff.SafeDispose();
        }
    }
}
