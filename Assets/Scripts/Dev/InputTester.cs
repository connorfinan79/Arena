using UnityEngine;

public class InputTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
            Debug.Log("W pressed!");
        if (Input.GetKey(KeyCode.A))
            Debug.Log("A pressed!");
        if (Input.GetKey(KeyCode.S))
            Debug.Log("S pressed!");
        if (Input.GetKey(KeyCode.D))
            Debug.Log("D pressed!");
        
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        if (h != 0 || v != 0)
        {
            Debug.Log($"Axis input: H={h}, V={v}");
        }
    }
}