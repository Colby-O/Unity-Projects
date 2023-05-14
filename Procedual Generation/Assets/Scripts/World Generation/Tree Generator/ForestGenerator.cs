using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestGenerator : MonoBehaviour
{
    public static List<GameObject> GenerateForest(ForestData fosestData, int size, float[,] heightMap, Vector2 chunkPos, TerrianData terrianData, Transform parnet)
    {
        float chunkScale = terrianData.uniformScale;
        List<GameObject> forestObjects = new List<GameObject>();
        for (int i = 0; i < size; i += fosestData.spacing)
        {
            for (int j = 0; j < size; j += fosestData.spacing)
            {
                float height = terrianData.meshHeightCurve.Evaluate(heightMap[i, j]) * terrianData.meshHeightMultiplier;//terrianData.meshHeightCurve.Evaluate(heightMap[i, j]) * terrianData.meshHeightMultiplier;
                for ( int k = 0; k < fosestData.elements.Length; ++k)
                {
                    Element element = fosestData.elements[k];
                    
                    if (heightMap[i, j] > fosestData.maxHeight || heightMap[i, j] < fosestData.minHeight)
                    {
                        break;
                    }
                    else if (element.CanPlace())
                    {
                        
                        Vector3 pos = new Vector3(chunkPos.x - size/2 + i , height, chunkPos.y + size/2 - j) * chunkScale; 
                        Vector3 offset = new Vector3(Random.Range(-fosestData.offsetAmount, fosestData.offsetAmount), 0.0f, Random.Range(-fosestData.offsetAmount, fosestData.offsetAmount)); 
                        Vector3 rotation = new Vector3(Random.Range(0, fosestData.maxRotation), Random.Range(0, fosestData.maxRotation), Random.Range(0, fosestData.maxRotation));
                        Vector3 scale = Vector3.one * Random.Range(fosestData.minScalingFactor, fosestData.maxScalingFactor);

                        GameObject newElement = Instantiate(element.GetRandom());
                        newElement.transform.SetParent(parnet);
                        newElement.transform.position = pos + offset;
                        newElement.transform.eulerAngles = rotation;
                        newElement.transform.localScale = scale;
                        forestObjects.Add(newElement);
                        break;
                    }
                }
            }
        }
        return forestObjects;
    }

    public static void DeactivateForest(List<GameObject> forestObjects)
    {
        foreach (GameObject gameObject in forestObjects)
        {
            gameObject.SetActive(false);
        }
    }

    public static void ReactivateForest(List<GameObject> forestObjects)
    {
        foreach (GameObject gameObject in forestObjects)
        {
            gameObject.SetActive(true);
        }
    }
}
