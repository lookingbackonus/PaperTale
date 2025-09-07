using UnityEngine;

public class FlowerColorChanger : MonoBehaviour
{
    public MazeFlowerColor flowerColor;
    public MazeController mazeController;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("충돌");
            mazeController.AddFlowerColor(flowerColor);
        }
    }

}
