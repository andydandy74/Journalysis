using System;
using System.Collections.Generic;
using System.Text;

namespace Journalysis.Core.Models
{
    /// <summary>
    /// Represents a pair of overlapping synchronize-with-central operations and their owning sessions.
    /// </summary>
    /// <remarks>
    /// This model stores an identified pair; it does not detect overlaps or validate session ownership.
    /// </remarks>
    public sealed class SyncEventOverlap
    {
        /// <summary>
        /// Initializes a sync-event overlap.
        /// </summary>
        /// <param name="firstSession">Session that owns the first sync event.</param>
        /// <param name="firstEvent">First sync event in the pair.</param>
        /// <param name="secondSession">Session that owns the second sync event.</param>
        /// <param name="secondEvent">Second sync event in the pair.</param>
        /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
        public SyncEventOverlap(
            WorksharingSession firstSession,
            SyncEvent firstEvent,
            WorksharingSession secondSession,
            SyncEvent secondEvent)
        {
            ArgumentNullException.ThrowIfNull(firstSession);
            ArgumentNullException.ThrowIfNull(firstEvent);
            ArgumentNullException.ThrowIfNull(secondSession);
            ArgumentNullException.ThrowIfNull(secondEvent);

            FirstSession = firstSession;
            FirstEvent = firstEvent;
            SecondSession = secondSession;
            SecondEvent = secondEvent;
        }

        /// <summary>
        /// Gets the first sync event in the pair.
        /// </summary>
        public SyncEvent FirstEvent { get; }

        /// <summary>
        /// Gets the session that owns <see cref="FirstEvent"/>.
        /// </summary>
        public WorksharingSession FirstSession { get; }

        /// <summary>
        /// Gets the second sync event in the pair.
        /// </summary>
        public SyncEvent SecondEvent { get; }

        /// <summary>
        /// Gets the session that owns <see cref="SecondEvent"/>.
        /// </summary>
        public WorksharingSession SecondSession { get; }

        public override string ToString() => nameof(SyncEventOverlap);
    }
}
