using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRespawn : MonoBehaviour

{
    public float waitTime = 1.0f;
    public GameObject spawnPoint;
    public float bulletForce = 20f;
    private bool isHit = false;

void OnTriggerEnter2D(Collider2D other) {
    if (other.CompareTag("Bullet")) {
        isHit = true;
        Destroy(other.gameObject);
        Vector2 direction = (transform.position - other.transform.position).normalized;
        GetComponent<Rigidbody2D>().AddForce(direction * bulletForce, ForceMode2D.Impulse);
        Invoke("TeleportToSpawnPoint", waitTime);
    }
}


    private void TeleportToSpawnPoint()
    {
        if (isHit)
        {
            transform.position = spawnPoint.transform.position;
            isHit = false;
        }
    }
}
