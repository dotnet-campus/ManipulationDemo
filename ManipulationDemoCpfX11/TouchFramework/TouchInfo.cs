using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManipulationDemoCpfX11.TouchFramework;

public record TouchInfo(int Id, double X, double Y, double TouchMajor, double TouchMinor, TouchStatus TouchStatus)
{
}

public enum TouchStatus
{
    Down,
    Move,
    Up,
}