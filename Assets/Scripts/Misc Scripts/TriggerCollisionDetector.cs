using UnityEngine;

public class TriggerCollisionDetector : MonoBehaviour
{
    public bool IsTouching { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IsTouching = true;
      
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IsTouching = false;
       
    }
}
