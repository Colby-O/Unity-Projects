using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerInventory : MonoBehaviour
{
    public GameObject inventory;        // Variable to Store the players inventory object
    public GameObject slotHolder;       // Variable to store the players inventory slot holder object
    public GameObject itemManger;       // Varable to store the players item manger object          
    private bool inventoryEnabled;      // True if the player's inventory is open. False otherwise

    private int slots;                  // Number of slots in the player's inventory
    private Transform[] slot;           // Store each inventory slot object

    private GameObject itemPickedUp;    // Store an item that is picked up
    private bool itemAdded;             // True of am item was added to the players inventory. False otherwise

    void Start()
    {
        slots = slotHolder.transform.childCount;
        slot = new Transform[slots];
        DetectInventorySlots();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryEnabled = !inventoryEnabled;
		
            if (Cursor.lockState == CursorLockMode.None)
		    Cursor.lockState = CursorLockMode.Locked;
		else
		    Cursor.lockState = CursorLockMode.None;
        }

        if(inventoryEnabled)
        {
            inventory.GetComponent<Canvas>().enabled = true;
        }
        else
        {
            inventory.GetComponent<Canvas>().enabled = false;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Item"))
        {
          print("Colliding");
          itemPickedUp = other.gameObject;
          AddItem(itemPickedUp);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            itemAdded = false;
        }
    }


    public void AddItem(GameObject item)
    {
        for(int i = 0; i < slots; ++i)
        {
            if (slot[i].GetComponent<Slot>().isEmpty && itemAdded == false)
            {
                slot[i].GetComponent<Slot>().item = itemPickedUp;
                slot[i].GetComponent<Slot>().itemIcon = itemPickedUp.GetComponent<Item>().icon;

                item.transform.parent = itemManger.transform;
                item.transform.position = itemManger.transform.position;
                
                if (item.GetComponent<MeshRenderer>())
                {
                    item.GetComponent<MeshRenderer>().enabled = false;
                }

                Destroy(item.GetComponent<Rigidbody>());

                itemAdded = true;
            }
        }
    }

    public void DetectInventorySlots()
    {
        for (int i = 0; i < slots; ++i)
        {
            slot[i] = slotHolder.transform.GetChild(i);
        }
    }
}
