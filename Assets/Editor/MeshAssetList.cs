using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CreateAssetMenu(fileName = "Mesh Asset List", menuName = "Advanced Structure Skins/Mesh Asset List")]
public class MeshAssetList : ScriptableObject
{
    public List<Object> objects = new List<Object>();
    public string outputPath = "";
}
