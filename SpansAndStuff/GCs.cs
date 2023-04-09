using System;
using System.Runtime;

namespace SpansAndStuff;
internal class GCs
{
    public static void NoGCRegionTests()
    {
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency; // settable 
        GC.TryStartNoGCRegion(1, 2, false);
        // Do work
        if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
            GC.EndNoGCRegion();
    }
}
