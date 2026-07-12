using UnityEngine;

public class Minimap : MonoBehaviour
{
    private Transform player;

    void Update()
    {
        // Nếu chưa có player thì tìm
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        // Nếu đã có player thì update vị trí minimap
        if (player != null)
        {
            Vector3 newPosition = player.position;
            newPosition.y = transform.position.y; // giữ y của minimap
            transform.position = newPosition;
        }
    }
}
