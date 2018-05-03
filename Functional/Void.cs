using System;

namespace Luger.Functional
{
    public struct Void : IEquatable<Void>
    {
        bool IEquatable<Void>.Equals(Void other) => true;

        public override string ToString() => nameof(Void);        
    }
}