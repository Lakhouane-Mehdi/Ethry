// ─────────────────────────────────────────────────────────────────────────
// Weather overlay — full-screen canvas_item shader driven by WeatherSystem.
// Reads the rendered game-world via screen_texture and applies per-weather
// tints, wetness contrast, storm vignette + desaturation, snow cooling,
// and a fog gradient. All intensities are animated from C#.
// ─────────────────────────────────────────────────────────────────────────
shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

// 0..1 intensities (tweened by WeatherSystem.TransitionTo)
uniform float wetness : hint_range(0.0, 1.0) = 0.0;
uniform float storm   : hint_range(0.0, 1.0) = 0.0;
uniform float snow    : hint_range(0.0, 1.0) = 0.0;
uniform float fog     : hint_range(0.0, 1.0) = 0.0;

// Tint colors (tweaked in WeatherSystem if desired)
uniform vec3 rain_tint  = vec3(0.58, 0.70, 0.88);
uniform vec3 storm_tint = vec3(0.36, 0.42, 0.58);
uniform vec3 snow_tint  = vec3(0.90, 0.94, 1.00);
uniform vec3 fog_color  = vec3(0.72, 0.76, 0.82);

void fragment() {
    vec4 src = texture(screen_texture, SCREEN_UV);
    vec3 col = src.rgb;

    // Wet-ground sheen: cool tint + contrast boost for "damp pixel" feel
    if (wetness > 0.001) {
        vec3 wet = col * rain_tint;
        wet = mix(wet, wet * wet * 1.35, 0.30); // gamma-ish contrast bump
        col = mix(col, wet, wetness * 0.55);
    }

    // Storm: desaturate, darken, radial vignette
    if (storm > 0.001) {
        float luma = dot(col, vec3(0.299, 0.587, 0.114));
        vec3 desat = mix(col, vec3(luma), 0.40);
        col = mix(col, desat * storm_tint, storm * 0.65);

        vec2 v = SCREEN_UV - vec2(0.5);
        float vig = 1.0 - dot(v, v) * 1.9 * storm;
        col *= clamp(vig, 0.0, 1.0);
    }

    // Snow: cool brighten for bounced light
    if (snow > 0.001) {
        vec3 cool = col * snow_tint + vec3(0.06) * snow;
        col = mix(col, cool, snow * 0.55);
    }

    // Fog gradient — stronger toward screen edges, suggests atmospheric depth
    if (fog > 0.001) {
        vec2 v = SCREEN_UV - vec2(0.5);
        float edge = clamp(dot(v, v) * 2.2, 0.0, 1.0);
        float f = fog * (0.20 + 0.55 * edge);
        col = mix(col, fog_color, f);
    }

    COLOR = vec4(col, 1.0);
}
