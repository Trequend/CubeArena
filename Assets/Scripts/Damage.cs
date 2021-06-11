using System;

public struct Damage
{
    public float Value { get; }

    public Damage(float value)
    {
        if (value <= 0.0f)
        {
            throw new ArgumentException("Damage must be greater than 0");
        }

        Value = value;
    }

    public static Damage FromVariation(float minValue, float variation)
    {
        float value = minValue + UnityEngine.Random.Range(0.0f, variation);
        return new Damage(value);
    }
}
