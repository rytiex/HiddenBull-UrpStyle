using System;

namespace HiddenBull.UrpStyle
{
    [Flags]
    public enum HBStyleProfileSections
    {
        None = 0,
        Brush = 1 << 0,
        Sky = 1 << 1,
        Celestial = 1 << 2,
        Clouds = 1 << 3,
        Atmosphere = 1 << 4,
        Weather = 1 << 5,
        All = Brush | Sky | Celestial | Clouds | Atmosphere | Weather
    }
}
