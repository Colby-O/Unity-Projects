using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    public bool isBuilding;

    public List<BuildObject> buildObjects = new List<BuildObject>();
    public BuildObject currentObject;
    private Vector3 currentPos;
    public Transform currentpreview;
    public Transform camera;
    public RaycastHit hit;
    public LayerMask layer;
    public float maxDist;

    public float offset = 1.0f;
    public float gridSize= 1.0f;

    void Start()
    {
        currentObject = buildObjects[0];
        PlaceBuildObject();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (isBuilding)
        {
            startPreview();
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            PlaceBuildObject();
        }
    }

    public void PlaceBuildObject()
    {
        GameObject nextpreview = Instantiate(currentObject.preview, currentPos, Quaternion.identity) as GameObject;
        currentpreview = nextpreview.transform;
    }

    public void startPreview()
    {
        if (Physics.Raycast(camera.position, camera.forward, out hit, maxDist, layer))
        {
            if (hit.transform != this.transform)
            {
                showPreview(hit);
            }
        }
    }

    public void showPreview(RaycastHit hit)
    {
        currentPos = hit.point;
        currentPos = (currentPos - Vector3.one * offset) / gridSize;
        currentPos = new Vector3(Mathf.Round(currentPos.x), Mathf.Round(currentPos.y), Mathf.Round(currentPos.z));
        currentPos = currentPos * gridSize + Vector3.one * offset;

        currentpreview.position = currentPos;
    }   
}
[System.Serializable]
public class BuildObject
{
    public string name;
    public GameObject preview;
    public int grids;
}
