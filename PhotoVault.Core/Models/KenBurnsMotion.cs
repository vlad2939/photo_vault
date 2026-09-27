namespace PhotoVault.Core.Models;

/// <summary>Direcția de pan Ken Burns: una din cele 4 diagonale (§6.10).</summary>
public enum PanDirection
{
    TopLeftToBottomRight,
    TopRightToBottomLeft,
    BottomLeftToTopRight,
    BottomRightToTopLeft
}

/// <summary>
/// Mișcarea unei poze în slideshow: scalare de la <see cref="StartScale"/> la <see cref="EndScale"/>
/// și deplasare între două puncte, exprimate ca fracțiuni din dimensiunea ecranului (0,05 = 5% din lățime / înălțime).
/// </summary>
public sealed record KenBurnsMotion(
    bool ZoomIn,
    PanDirection Pan,
    double StartScale,
    double EndScale,
    double StartOffsetX,
    double StartOffsetY,
    double EndOffsetX,
    double EndOffsetY);
