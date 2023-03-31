using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float bulletForce = 20f;
    public float fireRate = 0.5f;
    public float nextFireTime = 0f;
    public float bulletRange = 10f;
    public float bulletVelocity = 10f;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }

void Shoot()
{
    GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    Rigidbody2D rb2d = bullet.GetComponent<Rigidbody2D>();
    float bulletRange = 10f; // set your desired maximum range here
    Vector2 direction = (firePoint.right + (Vector3.up * Random.Range(-0.05f, 0.05f))).normalized; // add a slight random vertical offset
    rb2d.velocity = direction * bulletForce;
    Destroy(bullet, bulletRange / bulletForce); // destroy the bullet after it has traveled its maximum range
}

}
