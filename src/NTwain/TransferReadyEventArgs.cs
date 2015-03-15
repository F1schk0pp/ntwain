﻿using NTwain.Data;
using System;

namespace NTwain
{
    /// <summary>
    /// Contains event data when a data transfer is ready to be processed.
    /// </summary>
    public class TransferReadyEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransferReadyEventArgs"/> class.
        /// </summary>
        /// <param name="pendingCount">The pending count.</param>
        /// <param name="endOfJob">if set to <c>true</c> [end of job].</param>
        /// <param name="imageInfo">The image information.</param>
        /// <param name="audioInfo">The audio information.</param>
        public TransferReadyEventArgs(int pendingCount, bool endOfJob, TWImageInfo imageInfo, TWAudioInfo audioInfo)
        {
            PendingTransferCount = pendingCount;
            EndOfJob = endOfJob;
            PendingImageInfo = imageInfo;
            AudioInfo = audioInfo;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current transfer should be canceled
        /// and continue next transfer if there are more data.
        /// </summary>
        /// <value><c>true</c> to cancel current transfer; otherwise, <c>false</c>.</value>
        public bool CancelCurrent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all transfers should be canceled.
        /// </summary>
        /// <value><c>true</c> to cancel all transfers; otherwise, <c>false</c>.</value>
        public bool CancelAll { get; set; }

        /// <summary>
        /// Gets a value indicating whether current transfer signifies an end of job in TWAIN world.
        /// </summary>
        /// <value><c>true</c> if transfer is end of job; otherwise, <c>false</c>.</value>
        public bool EndOfJob { get; private set; }

        /// <summary>
        /// Gets the known pending transfer count. This may not be appilicable 
        /// for certain scanning modes.
        /// </summary>
        /// <value>The pending count.</value>
        public int PendingTransferCount { get; private set; }

        /// <summary>
        /// Gets the tentative image information for the current transfer if applicable.
        /// This may differ from the final image depending on the transfer mode used (mostly when doing mem xfer).
        /// </summary>
        /// <value>
        /// The image info.
        /// </value>
        public TWImageInfo PendingImageInfo { get; private set; }

        /// <summary>
        /// Gets the audio information for the current transfer if applicable.
        /// </summary>
        /// <value>
        /// The audio information.
        /// </value>
        public TWAudioInfo AudioInfo { get; private set; }

    }
}
