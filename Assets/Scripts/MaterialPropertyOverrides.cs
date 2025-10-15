using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "StructurePropertyOverride", menuName = "Advanced Structure Skins/Overrides")]
public class MaterialPropertyOverrides : ScriptableObject
{
    public List<MaterialPropertyOverride> overrides = new();
}

[Serializable]
public class MaterialPropertyOverride
{
    public string propertyName;
    public ShaderPropertyType propertyType;
    public Color colorValue;
    public float floatValue;
    public int intValue;
    public Vector4 vectorValue;
    //public Texture textureValue;
    public List<StructureType> targetStructures = new();
}

public enum StructureType
{
    Disc, Pillar, Ball, Cube, Wall, SmallRock, LargeRock
}
