using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;

    [Header("Behavior")]
    [SerializeField] private bool isAutomatic = false;
    [SerializeField, Tooltip("Shots per second")] private float fireRate = 6f;
    [SerializeField, Tooltip("Degrees of random spread")]
    private float spread = 2f;
    [SerializeField] private float bulletSpeed = 20f;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 10;
    [SerializeField] private float reloadTime = 1.2f;

    [Header("Hitscan (optional)")]
    [SerializeField] private bool useHitscan = false;
    [SerializeField] private float hitscanRange = 50f;
    [SerializeField] private LayerMask hitMask = ~0;

    private int currentAmmo;
    private bool isReloading = false;
    private float fireCooldown = 0f;

    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;

    void Awake()
    {
        currentAmmo = magazineSize;
    }

    void Update()
    {
        fireCooldown -= Time.deltaTime;

        RotateToMouse();

        if (isReloading) return;

        bool fireInput = isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
        if (fireInput) TryFire();

        if (Input.GetKeyDown(KeyCode.R)) StartCoroutine(Reload());
    }

    public void TryFire()
    {
        if (isReloading) return;
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }
        if (fireCooldown > 0f) return;
        Fire();
    }

    private void Fire()
    {
        currentAmmo--;
        fireCooldown = 1f / Mathf.Max(0.0001f, fireRate);

        if (muzzleFlash) muzzleFlash.Play();
        if (audioSource && fireSound) audioSource.PlayOneShot(fireSound);

        if (useHitscan) DoHitscan();
        else SpawnProjectile();
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null || muzzlePoint == null) return;

        float angle = Random.Range(-spread, spread);
        Quaternion rot = Quaternion.Euler(0f, 0f, angle) * muzzlePoint.rotation;

        GameObject proj = Instantiate(projectilePrefab, muzzlePoint.position, rot);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = rot * Vector3.right * bulletSpeed;
        }
    }

    private void DoHitscan()
    {
        if (muzzlePoint == null) return;
        // Base direction comes from the gun's world rotation (right vector)
        Vector2 baseDir = muzzlePoint.right;
        float angleOffset = Random.Range(-spread, spread);
        Vector2 dir = (Quaternion.Euler(0f, 0f, angleOffset) * baseDir).normalized;
        Vector2 origin = muzzlePoint.position;
        Debug.DrawRay(origin, dir * hitscanRange, Color.red, 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, hitscanRange, hitMask);
        if (hit.collider != null)
        {
            hit.collider.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void RotateToMouse()
    {
        if (muzzlePoint == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 dir = (mouseWorld - transform.position);
        if (dir.sqrMagnitude <= 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // If object (or any parent) has an overall negative X scale, rotation appears mirrored.
        // Compensate by adding 180 degrees when the combined X scale is negative.
        if (Mathf.Sign(transform.lossyScale.x) < 0f) angle += 180f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator Reload()
    {
        if (isReloading) yield break;
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magazineSize;
        isReloading = false;
    }

    void OnValidate()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        fireRate = Mathf.Max(0.001f, fireRate);
        bulletSpeed = Mathf.Max(0f, bulletSpeed);
    }
}
