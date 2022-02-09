using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface ISavable
{
    public void Save(BinaryWriter writer);
    public void Load(BinaryReader reader);
}

/// <summary>
/// Handles saving and loading on top level but each ISavable object is responsible of saving itself
/// </summary>
public class DataSaver : MonoBehaviour
{
    private string savePath;
    private string roadFile = "road_save";

    private void Awake() 
    {
        savePath = Application.persistentDataPath;
        savePath = Path.Combine(savePath, roadFile);
    }
    public void Save(ISavable savable)
    {
        using(var writer = new BinaryWriter(File.Open(savePath, FileMode.Create)))
        {
            savable.Save(writer);
        }
    }

    public void Load(ISavable savable)
    {
        using(var reader = new BinaryReader(File.Open(savePath, FileMode.Open)))
        {
            savable.Load(reader);
        }
    }
}
