using System.IO;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Road : ISavable
{
    private List<Junction> junctions = new List<Junction>();
    private List<Section> sections = new List<Section>();

    public List<Junction> Junctions {get {return junctions;}}
    public List<Section> Sections {get {return sections;}}

    // Pairs of index and junction/section for fast searching
    private Dictionary<int, Junction> junctionIndexes = new Dictionary<int, Junction>(); 
    private Dictionary<int, Section> sectionIndexes = new Dictionary<int, Section>();

    private RoadEditorManager_Task roadManager; // A bit of spaghetti -> If I have time I will move responsibility of creating sections and junctions here instead

    public Road(RoadEditorManager_Task roadManager)
    {
        this.roadManager = roadManager;
    }

    public Junction GetJunction(int index)
    {
        if(junctionIndexes.ContainsKey(index))
            return junctionIndexes[index];

        foreach(Junction j in junctions)
            if(j.index == index)
            {
                junctionIndexes.Add(j.index, j); // Next time no need to loop
                return j;
            }

        return null;
    }

    public Section GetSection(int index)
    {
        if(sectionIndexes.ContainsKey(index))
            return sectionIndexes[index];

        foreach(Section s in sections)
            if(s.index == index)
            {
                sectionIndexes.Add(s.index, s); // Next time no need to loop
                return s;
            }

        return null;
    }

    public int GetIndexOfGameObject(GameObject gameObject)
    {
        foreach(Junction j in junctions) // I didn't see a reason to cache this too in a dictionary since we call this function less
            if(j.worldObject == gameObject)
                return j.index;

        return -1;
    }

    public int GetJunctionsNum()
    {
        return junctions.Count;
    }

    public int GetLastJuncionIndex()
    {
        if(junctions.Count < 1) 
            return -1;
        return junctions[junctions.Count-1].index;
    }

    public int GetSectionsNum()
    {
        return sections.Count;
    }

    public void AddJunction(Junction j)
    {
        junctions.Add(j);
    }

    public void AddSection(Section s)
    {
        sections.Add(s);
    }

    public void RemoveJunction(int index)
    {
        Junction j = GetJunction(index);
        junctions.Remove(j);
        junctionIndexes.Remove(j.index);
        j.DestroyJunction();
    }

    public void RemoveSection(int index)
    {
        Section s = GetSection(index);
        sections.Remove(s);
        sectionIndexes.Remove(s.index);
        s.DestroySection();
    }

    public void ClearJunctions()
    {
        foreach(Junction j in junctions)
            j.DestroyJunction();

        junctions.Clear();
    }

    public void ClearSections()
    {
        foreach(Section s in sections)
            s.DestroySection();

        sections.Clear();
    }

    public void Save(BinaryWriter writer)
    {
        Debug.Log("Saving");
        
        // Save number of junctions
        writer.Write(junctions.Count);

        // Save all junctions: Position x, Position y, Position z, Index, # of neighbors, neighbor indexes
        foreach(Junction j in junctions)
        {
            writer.Write(j.position.x);
            writer.Write(j.position.y);
            writer.Write(j.position.z);
            writer.Write(j.index);
            writer.Write(j.neighbors.Count);
            foreach(Junction n in j.neighbors)
                writer.Write(n.index);
        }
    }

    public void Load(BinaryReader reader)
    {
        Debug.Log("Loading");

        // Clear existing data
        ClearJunctions();
        ClearSections();

        List<Junction> loadedJunctions = new List<Junction>();
        Dictionary<int, Junction> indexTable = new Dictionary<int, Junction>();
        
        int junctionsCount = reader.ReadInt32();
        // Knowing how many junctions there are, we can read the information correctly
        for (var i = 0; i < junctionsCount; i++)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            int index = reader.ReadInt32();

            // Load neighbor indexes - later need to reference acoordingly to Junction objects
            int neighborsCount = reader.ReadInt32();
            List<int> neighborIndexes = new List<int>();

            for (var k = 0; k < neighborsCount; k++)
                neighborIndexes.Add(reader.ReadInt32());

            // Create Junction object
            Vector3 position = new Vector3(x, y, z);
            Junction j = new Junction(null, index, position, neighborIndexes: neighborIndexes);
            loadedJunctions.Add(j);
            indexTable.Add(index, j);
        }

        // Resolve neighbor references
        foreach(Junction j in loadedJunctions)
        {
            foreach(int i in j.neighborIndexes)
                j.neighbors.Add(indexTable[i]);
        }

        roadManager.CreateRoadFromLoaded(loadedJunctions);
    }
}
