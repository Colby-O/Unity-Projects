using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public bool isHovered;                // Stores if the players mouse is hovering over a slot or not
    public bool isEmpty;                  // Stores if the slot contains an item or not   

    public GameObject item;               // Item in the slot if any
    public Texture itemIcon;              // Texture of the item in the slot if any

    private GameObject player;            // Variable to store the players GameObject

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        isHovered = false;

    }

    void Update()
    {
        if (item)
        {
            isEmpty = false;

            itemIcon = item.GetComponent<Item>().icon;
            GetComponent<RawImage>().texture = itemIcon;
        }
        else
        {
            isEmpty = true;
            itemIcon = null;
            this.GetComponent<RawImage>().texture = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (item)
        {
            Item thisItem = item.GetComponent<Item>();

            if (thisItem.type == "Water")
            {
                player.GetComponent<playerStatus>().Drink(thisItem.statDecraseRate);
                Destroy(item);
            }
        }
    }
}
