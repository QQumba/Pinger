using System;

namespace Pinger
{
    [Flags]
    public enum OutputFormats
    {
        None = 0,
        Txt = 1,
        Xml = 2,
        Html = 4,
        Console = 8
    }
}