using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RoadEditorManager_Base : MonoBehaviour
{
    public float MaxRoadDistance = 100f;
    public float MaxHeightDif = 3f;
    public float HeightCostAdd = 2f;

    public abstract bool Init();

    public abstract void StartRoadEdit();

}
