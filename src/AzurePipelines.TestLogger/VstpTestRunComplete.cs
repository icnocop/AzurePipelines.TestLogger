using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace AzurePipelines.TestLogger
{
    internal class VstpTestRunComplete
    {
        public VstpTestRunComplete(bool aborted, ICollection<AttachmentSet> attachmentSets)
        {
            Aborted = aborted;
            Attachments = attachmentSets;
        }

        public bool Aborted { get; }
        public ICollection<AttachmentSet> Attachments { get; set; }
    }
}