using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MarcinGajda
{
    public class UnmanagedTests : SafeHandle
    {
        public UnmanagedTests(IntPtr invalidHandleValue, bool ownsHandle) : base(invalidHandleValue, ownsHandle)
        {
        }

        public override bool IsInvalid => throw new NotImplementedException();

        protected override bool ReleaseHandle() => throw new NotImplementedException();
    }
}
