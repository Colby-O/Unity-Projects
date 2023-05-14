using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerStatus : MonoBehaviour
{
    public float maxHealth;                 // Maximum health the player can have
    public float maxThirst;                 // Maximum thrist the player can have before dieing
    public float maxHunger;                 // Maximum hunger the player can have before dieing

    [Range(0.0f, 1.0f)]
    public float thirstIncreaseRate;        // The rate at which the player thirst increases
    [Range(0.0f, 1.0f)]
    public float hungerIncreaseRate;        // The rate at which the player hunger increases

    public float health;                    // The players current health
    public float thirst;                    // The players current thrist
    public float hunger;                    // The players current hunger

    public float damage;                    // How much damage the player can do

    public bool isDead;                     // Variable to store if the player is still alive or not

    public static bool triggeringWithAI;    // True if the player triggered an AI's hitbox. False otherwise
    public static GameObject triggeringAI;  // Stores the Game Object of the AI the player triggered
   
    void Start()
    {
        health = maxHealth;
    }

    private void FixedUpdate()
    {
        // update thrist/hunger
        if (!isDead)
        {
            thirst += thirstIncreaseRate;
            hunger += hungerIncreaseRate;
        }
        // checks if player died from thrist or hunger
        if (thirst >= maxThirst || hunger >= maxHunger)
        {
            Die();
        }

        // AI Interaction
        if(triggeringWithAI)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Attack(triggeringAI);
            }
        }
    }

    public void Die()
    {
        isDead = true;
        print("You have died beacase of thirst or hunger");
    }

    public void Attack(GameObject target)
    {
        if(target.CompareTag("Animal"))
        {
            Animal animal = target.GetComponent<Animal>();
            animal.health -= damage;
        }
    }

    public void Drink(float thirstDecreaseRate)
    {
        thirst -= thirstDecreaseRate;
    }

    public void OnTriggerEnter(Collider other)
    {
	Debug.Log("asdflkj;");
        if(other.CompareTag("Animal")) 
        {
            triggeringAI = other.gameObject;
            triggeringWithAI = true;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            triggeringAI = null ;
            triggeringWithAI = false;
        }
    }
}
