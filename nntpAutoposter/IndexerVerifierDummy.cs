using System;
using Util.Configuration;

namespace nntpAutoposter
{
    /// <summary>
    /// Simple verifier that always returns true, used to automatically clean the backup folder at the first verification attempt.
    /// </summary>
    public class IndexerVerifierDummy : IndexerVerifierBase
    {
        internal IndexerVerifierDummy(Settings configuration) : base(configuration)
        {
        }

        protected override Boolean UploadIsOnIndexer(UploadEntry upload)
        {
            return true;
        }        
    }
}
