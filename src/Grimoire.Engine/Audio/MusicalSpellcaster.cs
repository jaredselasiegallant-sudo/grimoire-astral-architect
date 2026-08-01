using Grimoire.Core.Enums;

namespace Grimoire.Engine.Audio;

public sealed class MusicalSpellcaster
{
    private float _tempo;
    private float _intensity;

    public void CastSpell(SpellGesture gesture)
    {
        _intensity = gesture switch
        {
            SpellGesture.Circle => 0.4f,
            SpellGesture.Triangle => 0.6f,
            SpellGesture.Line => 0.3f,
            SpellGesture.Zigzag => 0.7f,
            SpellGesture.Spiral => 0.9f,
            _ => 0.5f
        };
    }

    public void Update(float deltaTime)
    {
        if (_intensity > 0f)
            _intensity = Math.Max(0f, _intensity - deltaTime * 0.3f);
    }
}
