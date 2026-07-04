using UnityEngine;

/// <summary>
/// Common interface for all wind sources (PointWind2D, AreaWind2D, etc.).
/// WindSystem2D collects and queries all IWindSource2D instances in the scene.
/// </summary>
public interface IWindSource2D
{
    /// <summary>
    /// Calculates wind force at the given position at the given time.
    /// </summary>
    Vector2 GetForceAt(Vector2 position, float time);
}
