using UnityEngine;
using System.Collections.Generic;

public class StructureBuilder : MonoBehaviour
{
    public static StructureBuilder Instance { get; private set; }

    [Header("Structure Settings")]
    public GameObject beamPrefab;
    public GameObject columnPrefab;
    public GameObject diagonalPrefab;
    public GameObject characterPrefab;
    public GameObject bedrockPrefab;

    [Header("Joint Settings")]
    public float normalBreakForce = 200f;
    public float anchorBreakForce = 99999f;

    private List<GameObject> structureParts = new List<GameObject>();
    private List<FixedJoint2D> joints = new List<FixedJoint2D>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void BuildStructure(LevelData levelData)
    {
        ClearStructure();

        foreach (var part in levelData.structureParts)
        {
            GameObject prefab = GetPrefabForType(part.type);
            if (prefab != null)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.transform.position = part.position;
                obj.transform.rotation = Quaternion.Euler(0, 0, part.rotation);

                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.bodyType = part.isStatic ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic;
                }

                structureParts.Add(obj);
            }
        }

        CreateJoints(levelData.joints);
        SpawnCharacter(levelData.characterPosition);
        CreateBedrock(levelData.bedrockPosition);
    }

    private GameObject GetPrefabForType(PartType type)
    {
        switch (type)
        {
            case PartType.Beam: return beamPrefab;
            case PartType.Column: return columnPrefab;
            case PartType.Diagonal: return diagonalPrefab;
            default: return null;
        }
    }

    private void CreateJoints(List<JointData> jointDataList)
    {
        foreach (var jointData in jointDataList)
        {
            if (jointData.partA < structureParts.Count && jointData.partB < structureParts.Count)
            {
                GameObject partA = structureParts[jointData.partA];
                GameObject partB = structureParts[jointData.partB];

                FixedJoint2D joint = partA.AddComponent<FixedJoint2D>();
                joint.connectedBody = partB.GetComponent<Rigidbody2D>();
                joint.breakForce = normalBreakForce;

                joints.Add(joint);
            }
        }
    }

    public void UpgradeJointToAnchor(FixedJoint2D joint)
    {
        if (joint != null)
        {
            joint.breakForce = anchorBreakForce;
        }
    }

    private void SpawnCharacter(Vector2 position)
    {
        if (characterPrefab != null)
        {
            GameObject character = Instantiate(characterPrefab, transform);
            character.transform.position = position;
            structureParts.Add(character);
        }
    }

    private void CreateBedrock(Vector2 position)
    {
        if (bedrockPrefab != null)
        {
            GameObject bedrock = Instantiate(bedrockPrefab, transform);
            bedrock.transform.position = position;
        }
    }

    public void ClearStructure()
    {
        foreach (var part in structureParts)
        {
            if (part != null)
            {
                Destroy(part);
            }
        }
        structureParts.Clear();
        joints.Clear();
    }

    public List<GameObject> GetStructureParts()
    {
        return structureParts;
    }
}

public enum PartType
{
    Beam,
    Column,
    Diagonal,
    Character,
    Bedrock
}

[System.Serializable]
public class PartData
{
    public PartType type;
    public Vector2 position;
    public float rotation;
    public bool isStatic;
}

[System.Serializable]
public class JointData
{
    public int partA;
    public int partB;
}

[System.Serializable]
public class LevelData
{
    public List<PartData> structureParts = new List<PartData>();
    public List<JointData> joints = new List<JointData>();
    public Vector2 characterPosition;
    public Vector2 bedrockPosition;
    public int maxAnchors;
}
