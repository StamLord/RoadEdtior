using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoadEditorManager_Task : RoadEditorManager_Base
{
    #region Variables

    private bool isEditing;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [Header("Saver")]
    [SerializeField] private DataSaver dataSaver;

    [Header("Cost Text")]
    [SerializeField] private Transform costCanvas;
    [SerializeField] private Text costText;
    [SerializeField] private Vector3 costOffset = new Vector3(0,5,0);

    [Header("Prefabs")]
    [SerializeField] private GameObject junctionPrefab;
    [SerializeField] private GameObject sectionPrefab;
    [SerializeField] private GameObject constructionPrefab;

    [Header("Debug View")]
    [SerializeField] private bool debug;
    [SerializeField] private Vector3 junctionSize = Vector3.one;
    [SerializeField] private Color junctionColor = Color.green;
    [SerializeField] private float cursorRadius = 1f;
    [SerializeField] private Color cursorColor = Color.yellow;

    [Header("Road Data")]
    [SerializeField] private int junctionIndex = 0;
    [SerializeField] private StackPlus<int> lastJunctionIndex = new StackPlus<int>();
    [SerializeField] private Road road;
    
    // Cache
    private Vector3 mousePos; // Current mouse position
    private GameObject hitObject; // Object the mouse is hovering on
    private float distance; // Distance from current mouse position to last junction

    // Flags for invalid mouse position
    [SerializeField] private bool isValid;
    [SerializeField] private bool isTooFar;
    [SerializeField] private bool isTooHigh;
    [SerializeField] private bool isJunction;

    // Instantiated "Under Construction" object
    private GameObject constructionObj; 
    
    // Index Generators
    private IndexGenerator juncIndexGen;
    private IndexGenerator sectIndexGen;

    #endregion

    struct SectionStruct
    {
        public int from;
        public int to;

        public SectionStruct(int from, int to)
        {
            this.from = from;
            this.to = to;
        }
    }
    
    public override bool Init()
    {
        if(mainCamera == null)
            mainCamera = Camera.main; // Not efficient, better to reference from editor
        
        if(mainCamera == null)
        {
            Debug.LogWarning("No camera referenced or found.");
            return false;
        }

        if(junctionPrefab == null)
        {
            Debug.LogWarning("No junction prefab referenced.");
            return false;
        }

        if(sectionPrefab == null)
        {
            Debug.LogWarning("No section prefab referenced.");
            return false;
        }

        if(constructionPrefab == null)
        {
            Debug.LogWarning("No construction prefab referenced.");
            return false;
        }

        if(dataSaver == null)
            Debug.LogWarning("No DataSaver referenced. Saving and loading will not work.");

        if(constructionObj == null)
            constructionObj = Instantiate(constructionPrefab, Vector3.zero, Quaternion.identity, transform);
        
        juncIndexGen = new IndexGenerator();
        sectIndexGen = new IndexGenerator();

        road = new Road(this);
        return true;
    }

    public override void StartRoadEdit()
    {
        isEditing = true;

        // First junction is added as per documentation
        AddJunction(new Vector3(250, 0, -200));
    }

    public void StopRoadEdit()
    {
        isEditing = false;
    }

    private void Update()
    {
        if(isEditing == false)
            return;

        ProcessInput();
        UpdateCursorPosition(mousePos);
        UpdateCostText(mousePos);
    }

    private void ProcessInput()
    {
        KeysInput(); // Handle save/load buttons
        MouseInput(); // Handle mouse targeting and clicking
    }

    private void KeysInput()
    {
        if(dataSaver)
        {
            if(Input.GetKeyDown(KeyCode.F12))
                dataSaver.Save(road);
            else if(Input.GetKeyDown(KeyCode.F9))
                dataSaver.Load(road);
        }

        if(Input.GetKeyDown(KeyCode.Delete))
            if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                road.ClearJunctions();
                road.ClearSections();
                StartRoadEdit(); // Initialized first junction
            }
            else
                DeleteSelectedJunction();
    }

    private void MouseInput()
    {
        // If raycast from mouse not hitting anything we don't process anything further
        if(GetMouseOnTerrain(out mousePos, out hitObject) == false)
            return;

        // Validate the mouse position
        Junction j = road.GetJunction(junctionIndex);
        if(j == null)
        {
            Debug.LogError("No junciton with index " + junctionIndex); // Happened a few times , hard to replicate
            junctionIndex = road.GetLastJuncionIndex(); // suboptimal patch until resolved
        }

        isValid = ValidateNextPosition(
            j.position,  
            mousePos, 
            out isTooFar, 
            out isTooHigh,
            out isJunction,
            hitObject);
        
        // Left Click
        if(Input.GetMouseButtonDown(0))
        {
            if(isValid)
            {
                // Clicking on a junction connects to it
                if(isJunction)
                {
                    // Find junction index
                    int index = road.GetIndexOfGameObject(hitObject);
                    ConnectSection(index);
                }
                else
                {
                    if(Input.GetMouseButtonDown(0) && isValid)
                    AddJunction(mousePos);
                }
            }
        }
        
        // Right Click
        else if(Input.GetMouseButtonDown(1))
        {
            // Clicking on a junction sets current selection to it
            if(hitObject.CompareTag("Junction"))
            {
                // Find junction index
                int index = road.GetIndexOfGameObject(hitObject);
                SetCurrentIndex(index);
            }
        }
    }

    private bool GetMouseOnTerrain(out Vector3 position, out GameObject hitObject)
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out hit))
        {
            position = hit.point;
            hitObject = hit.transform.gameObject;
            return true;
        }

        position = Vector3.zero;
        hitObject = null;
        return false;
    }

    private bool ValidateNextPosition(Vector3 lastPos, Vector3 newPos, out bool tooFar, out bool tooHigh, out bool junction, GameObject hitObject = null)
    {
        /* Note: We calculate real distance (instead of comparig squared values which is more optimal) 
        and cache it so other functions that are called later in the Update cycle don't have to do it again */
        // Check distance
        distance = Vector3.Distance(lastPos, newPos);
        tooFar = (distance > MaxRoadDistance);
        
        // Check height
        float heightDiff = Mathf.Abs(GetHeightDifference(lastPos, newPos));
        tooHigh = (heightDiff > MaxHeightDif);
        
        if(hitObject)
            junction = (hitObject.CompareTag("Junction"));
        else
            junction = false;

        if(tooFar || tooHigh)
            return false;

        return true;
    }

    private float GetHeightDifference(Vector3 a, Vector3 b)
    {
        return b.y - a.y;
    }

    private int CalculateCost(Vector3 a, Vector3 b)
    {
        // Difference should be positive for cost calculation
        float heightDiff = Mathf.Abs(GetHeightDifference(a, b)); 
        // Rounding back since the screenshots show int. Using FloorToInt over RoundToInt for predictability
        int cost = Mathf.FloorToInt(heightDiff * HeightCostAdd);  
        return cost;
    }

    private void SetCurrentIndex(int index)
    {
        lastJunctionIndex.Push(junctionIndex);
        junctionIndex = index;
    }
    
    public void AddJunction(Vector3 position)
    {   
        GameObject j = CreateJunction(position); // Visual
        Junction newJunction = new Junction(this, juncIndexGen.GetNewIndex(), position, worldObject: j);
        road.AddJunction(newJunction);
        SetCurrentIndex(newJunction.index); // Set index to last member added

        // Add Section
        if(road.GetJunctionsNum() > 1)
            AddSection(lastJunctionIndex.Peek(), junctionIndex);
    }

    private void AddSection(int from, int to)
    {
        // Visual
        GameObject sectionObj = CreateSection(
            road.GetJunction(lastJunctionIndex.Peek()).position, 
            road.GetJunction(junctionIndex).position, 
            sectionPrefab);
        
        // Add to list of sections
        Section s = new Section(
            from, 
            to, 
            sectIndexGen.GetNewIndex(), 
            sectionObj);
        road.AddSection(s);
        
        // Update neighbors for last and new junctions
        road.GetJunction(from).neighbors.Add(road.GetJunction(to));
        road.GetJunction(to).neighbors.Add(road.GetJunction(from));
    }

    /// <summary>
    /// Use this when creating section when loading data.
    /// </summary>
    private void AddSection(int from, int to, Vector3 fromPos, Vector3 toPos)
    {
        // Visual
        GameObject sectionObj = CreateSection(fromPos, toPos, sectionPrefab);
        
        // Add to list of sections
        Section s = new Section(
            from, 
            to, 
            sectIndexGen.GetNewIndex(), 
            sectionObj);
        road.AddSection(s);
    }

    private void ConnectSection(int index)
    {
        int lastIndex = junctionIndex;
        SetCurrentIndex(index); // Update selection to node we are connecting

        // Make sure selected section is not already connected to previous one
        Junction lastJunction = road.GetJunction(lastIndex);
        foreach(Junction n in lastJunction.neighbors)
        {
            if(n.index == index)
                return; 
        }

        // If none of the neighbors  match, we add new section
        AddSection(lastIndex, junctionIndex);
    }

    public int GetNumOfSections()
    {
        return road.GetSectionsNum();
    }

    private GameObject CreateSection(Vector3 from, Vector3 to, GameObject prefab)
    {
        // Instantiate section object
        Vector3 midPoint = (from + to) / 2;
        Quaternion rotation = Quaternion.LookRotation((to - from).normalized, Vector3.up);
        GameObject section = Instantiate(prefab, midPoint, rotation, transform);
        float distance = Vector3.Distance(from, to); // We don't use cached distance since this function can be called from CreateJunctionsFromLoaded()
        
        // Scale section
        section.transform.localScale = new Vector3(
            section.transform.localScale.x, 
            section.transform.localScale.y, 
            distance);
        
        return section;
    }

    private GameObject CreateJunction(Vector3 position)
    {
        // Instantiate junction object
        GameObject junction = Instantiate(junctionPrefab, position, Quaternion.identity, transform); // Can be optimized by pooling
        return junction;
    }
    
    private void DeleteSelectedJunction()
    {
        if(road.GetJunctionsNum() < 2)
            return;
        
        // Remove junction object and references
        road.RemoveJunction(junctionIndex);

        // Find all sections connected to this junction
        List<int> sectionsToRemove = new List<int>();
        
        foreach(Section s in road.Sections)
            if (s.from == junctionIndex || s.to == junctionIndex)
                sectionsToRemove.Add(s.index);
    
        // Remove all relevant sections
        foreach(int i in sectionsToRemove)
            road.RemoveSection(i);

        // Make sure to remove leftovers of removed junctions
        // This can happen when removing a junction with multiple connections
        lastJunctionIndex.Remove(junctionIndex);

        // Set indexes to last selection
        int last = lastJunctionIndex.Pop();
        junctionIndex = last;
    }

    private void UpdateCursorPosition(Vector3 position)
    {
        if(constructionObj == null)
            return;

        Vector3 lastPosition = road.GetJunction(junctionIndex).position;
        
        // // Clamp line if bigger than MaxRoadDistance
        float dist = (distance > MaxRoadDistance)? MaxRoadDistance : distance;
        Vector3 lookVector = position - lastPosition;

        if(lookVector == Vector3.zero)
            return;
            
        Quaternion rotation = Quaternion.LookRotation(lookVector, Vector3.up);

        constructionObj.transform.position = lastPosition;
        constructionObj.transform.rotation = rotation;
        constructionObj.transform.localScale = new Vector3(
            constructionObj.transform.localScale.x, 
            constructionObj.transform.localScale.y, 
            dist);
    }

    private void UpdateCostText(Vector3 position)
    {
        // Update world postion
        costCanvas.transform.position = position + costOffset;
        
        if(isJunction)
        {
            costText.text = ""; // Hide text
            return;
        }
        else if(isTooHigh || isTooFar)
        {
            costText.text = "No Access"; // Hide text
            return;
        }

        // Calculate cost and update text
        Junction j = road.GetJunction(junctionIndex);
        if(j == null)
        {
            Debug.LogWarning("No junction with index found");
            return;
        }

        Vector3 lastPosition = j.position;
        int cost = CalculateCost(lastPosition, position);
        costText.text = "" + cost;
    }

    public void CreateRoadFromLoaded(List<Junction> loadedJunctions)
    {
        int highestIndex = int.MinValue;
        // Note: We need to keep track of added sections so we don't add twice
        // This is because each junction instance contains all it's neighbors and neighbors contain eachother
        // Possible fix: We can treat sections as directional so each junction only contains sections that go "from it"
        // key, value = index a, index b
        
        List<SectionStruct> sections = new List<SectionStruct>(); 

        // Create all Junctions
        foreach(Junction j in loadedJunctions)
        {
            // Visual Object
            GameObject jo = CreateJunction(j.position);
            j.SetWorldObject(jo);
            road.AddJunction(j);
            
            // Create all Sections
            foreach(Junction neighbor in j.neighbors)
            {
                // If we already have this section, skip iteration
                // Maybe there is a better way of keeping track of this, need to research
                foreach(SectionStruct s in sections)
                {
                    if(s.from == j.index && s.to == neighbor.index ||
                        s.to == j.index && s.from == neighbor.index)
                        continue;
                }
                    
                AddSection(j.index, neighbor.index, j.position, neighbor.position);
                sections.Add(new SectionStruct(j.index, neighbor.index));
            }

            if(j.index > highestIndex)
                highestIndex = j.index;            
        }

        // Set current selection to highest value
        lastJunctionIndex.Clear();
        SetCurrentIndex(highestIndex);
        
        // Update index generator so we don't get duplicate indexes after loading
        juncIndexGen.SetIndex(highestIndex);
    }

    private void OnDrawGizmos() 
    {
        if(debug == false) return;
        
        int jNum = road.GetJunctionsNum();
        int sNum = road.GetSectionsNum();

        if(jNum < 1) return;

        Gizmos.color = junctionColor;

        // Draw all junctions
        for (var i = 0; i < jNum; i++)
            Gizmos.DrawCube(road.Junctions[i].position, junctionSize);

        // Draw all sections
        for (var i = 0; i < sNum; i++)
            Gizmos.DrawLine(road.Junctions[road.Sections[i].from].position, road.Junctions[road.Sections[i].to].position);
        
        // Draw current mouse position
        Gizmos.color = cursorColor;
        if (GetMouseOnTerrain(out mousePos, out hitObject))
            Gizmos.DrawSphere(mousePos, cursorRadius);
    }
}