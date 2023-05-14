using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Animal : MonoBehaviour
{
    public float health;            // Stores the animal's current health
    private bool isDead;            // True if the animal has died

    public int amountOfItems;       // Stores the number of items the animal can drop
    public GameObject[] items;      // Array of items that the animal can drop upon death

    public float radius;            // Store how far the animal can see to find a new destination 
    public float timer;             // Stores how long the animal will travel before changing directions

    private Transform target;       // Stores the animals current taarget destination 
    private NavMeshAgent agent;     // Manger for unitys Nav Mesh
    private float currentTimer;     // Stores how long the animal was traveling in a given direction 

    private bool isIdle;            // True if the animal is stationary. False otherwise
    public float idleTimer;         // Stores how long untill the animal will stop moving for 3 seconds
    private float currentIdleTimer; //  Stores how long the animal was traveling sice it's last rest

    IEnumerator switchIdle()
    {
        isIdle = true;
        yield return new WaitForSeconds(3);
        currentIdleTimer = 0;
        isIdle = false;
    }

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        currentTimer = timer;
        currentIdleTimer = idleTimer;

        items = new GameObject[amountOfItems];
    }

    void Update()
    {
        currentTimer += Time.deltaTime;
        currentIdleTimer += Time.deltaTime;

        if (currentIdleTimer >= idleTimer)
        {
            StartCoroutine("switchIdle");
        }

        if (currentTimer >= timer && !isIdle)
        {
            Vector3 newPosition = RandomNavSphere(transform.position, radius, -1);
            agent.SetDestination(newPosition);
            currentTimer = 0;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    public void DropItems()
    {
        for (int i = 0; i < items.Length; ++i)
        {
            GameObject droppedItem = Instantiate(items[i], transform.position, Quaternion.identity);
        }
    }

    public void Die()
    {
        DropItems();
        Destroy(this.gameObject);
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float distance, int layerMask)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, distance, layerMask);

        return navHit.position;
    }
}
