using Godot;
using System.Collections.Generic;

/// <summary>
/// Controls the master CanvasModulate for the world, combining 
/// DaySystem (time color) and WeatherSystem (cloudy/rain tint).
/// </summary>
public partial class DayNightCycle : CanvasModulate
{
    private struct ColorKey
    {
        public float Hour;
        public Color Color;
        public ColorKey(float h, Color c) { Hour = h; Color = c; }
    }

    private readonly List<ColorKey> _keys = new()
    {
        new ColorKey(6.0f,  new Color(1.0f, 0.85f, 0.7f)),  // Sunrise (Warm pink/orange)
        new ColorKey(9.0f,  new Color(1.0f, 1.0f,  1.0f)),  // Morning (Bright/Neutral)
        new ColorKey(16.0f, new Color(1.0f, 0.95f, 0.85f)), // Afternoon (Soft)
        new ColorKey(18.5f, new Color(1.0f, 0.65f, 0.45f)), // Sunset (Golden Orange)
        new ColorKey(20.5f, new Color(0.45f, 0.45f, 0.75f)), // Twilight (Blueish)
        new ColorKey(22.0f, new Color(0.12f, 0.15f, 0.35f))  // Night (Deep Indigo)
    };

    public override void _Ready()
    {
        // Initial sync
        UpdateLighting(DaySystem.Instance?.Hour ?? 6.0f);
    }

    public override void _Process(double delta)
    {
        if (DaySystem.Instance == null) return;
        UpdateLighting(DaySystem.Instance.Hour);
    }

    private void UpdateLighting(float hour)
    {
        // 1. Calculate Base Time Tint via interpolation
        Color baseColor = GetInterpolatedColor(hour);

        // 2. Multiply by Weather Tint (if available)
        if (WeatherSystem.Instance != null)
        {
            Color weatherTint = WeatherSystem.Instance.GetWeatherTint();
            baseColor *= weatherTint;
            
            // 3. Sync clouds with the current ambient light so they aren't "glowing" white at night
            WeatherSystem.Instance.SyncWithLighting(baseColor);
        }

        Color = baseColor;
    }

    private Color GetInterpolatedColor(float hour)
    {
        if (hour <= _keys[0].Hour) return _keys[0].Color;
        if (hour >= _keys[^1].Hour) return _keys[^1].Color;

        for (int i = 0; i < _keys.Count - 1; i++)
        {
            if (hour >= _keys[i].Hour && hour <= _keys[i + 1].Hour)
            {
                float t = (hour - _keys[i].Hour) / (_keys[i + 1].Hour - _keys[i].Hour);
                return _keys[i].Color.Lerp(_keys[i + 1].Color, t);
            }
        }

        return Colors.White;
    }
}
