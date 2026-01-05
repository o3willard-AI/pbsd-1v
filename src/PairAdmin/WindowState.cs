using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace PairAdmin;

/// <summary>
/// Window state model for persistence
/// </summary>
public class WindowState
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public bool IsMaximized { get; set; }
}
