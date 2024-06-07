using System;
using System.Buffers;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SpansAndStuff;
public static class Vectors
{
    public static void EqualsAny()
    {
        var needle = new Vector<int>(7);
        Span<int> haystackSpan = stackalloc int[8];
        for (int i = 0; i < haystackSpan.Length; i++)
        {
            haystackSpan[i] = i;
        }
        //System.Runtime.Intrinsics.X86.Aes.X64.ConvertToInt64(
        var haystack = new Vector<int>(haystackSpan);
        var equalsAny = Vector.EqualsAny(needle, haystack);

        // kinda bad place 
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions()
            {
                // This has bad default of NoCheck and i see default 'Online' here:
                // https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/working-with-certificates#certificate-revocation-list
                CertificateRevocationCheckMode = X509RevocationMode.Online
            },
            PooledConnectionLifetime = TimeSpan.FromMinutes(15) // Recreate every 15 minutes
        };
        var client = new HttpClient(handler);


    }
}
