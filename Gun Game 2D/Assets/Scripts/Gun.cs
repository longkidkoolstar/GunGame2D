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
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Ammo")]
    [SerializeField] private int magazineSize = 10;
    [SerializeField] private float reloadTime = 1.2f;

    [Header("Hitscan (optional)")]
    [SerializeField] private bool useHitscan = false;
    [SerializeField] private float hitscanRange = 50f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private LineRenderer hitscanLine;

    private int currentAmmo;
    private bool isReloading = false;
    private float fireCooldown = 0f;

    public int CurrentAmmo => currentAmmo;
    public bool IsReloading => isReloading;

    void Awake()
    {
        currentAmmo = magazineSize;
        if (useHitscan && hitscanLine == null)
        {
            GameObject lineObj = new GameObject("HitscanTrail");
            lineObj.transform.SetParent(transform);
            hitscanLine = lineObj.AddComponent<LineRenderer>();
            hitscanLine.startWidth = 0.05f;
            hitscanLine.endWidth = 0.05f;
            hitscanLine.material = new Material(Shader.Find("Sprites/Default")); // Basic white material
            hitscanLine.startColor = Color.yellow;
            hitscanLine.endColor = new Color(1, 1, 0, 0); // Fade out
            hitscanLine.enabled = false;
        }
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

        // Calculate direction directly from mouse position ensures accuracy regardless of scale/rotation
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 direction = (mouseWorld - muzzlePoint.position).normalized;

        float angleOffset = Random.Range(-spread, spread);
        // Apply spread rotation to the base direction
        direction = Quaternion.Euler(0, 0, angleOffset) * direction;

        // Determine rotation for the bullet sprite
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion bulletRot = Quaternion.Euler(0, 0, angle);

        GameObject proj = Instantiate(projectilePrefab, muzzlePoint.position, bulletRot);
        Destroy(proj, projectileLifetime);
        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * bulletSpeed;
        }
    }

    private void DoHitscan()
    {
        if (muzzlePoint == null) return;
        
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 direction = (mouseWorld - muzzlePoint.position).normalized;

        float angleOffset = Random.Range(-spread, spread);
        direction = Quaternion.Euler(0, 0, angleOffset) * direction;

        Vector2 origin = muzzlePoint.position;
        Vector2 hitPoint = origin + direction * hitscanRange;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, hitscanRange, hitMask);
        if (hit.collider != null)
        {
            hitPoint = hit.point;
            hit.collider.SendMessage("TakeDamage", 1, SendMessageOptions.DontRequireReceiver);
        }

        if (hitscanLine != null)
        {
            StartCoroutine(ShowTrail(origin, hitPoint));
        }
    }

    private IEnumerator ShowTrail(Vector3 start, Vector3 end)
    {
        hitscanLine.enabled = true;
        hitscanLine.SetPosition(0, start);
        hitscanLine.SetPosition(1, end);
        yield return new WaitForSeconds(0.05f);
        hitscanLine.enabled = false;
    }

    private void RotateToMouse()
    {
        if (muzzlePoint == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 dir = (mouseWorld - transform.position);
        
        // If the parent is flipped (negative X scale), we need to invert the X direction
        // for local calculation so the gun points correctly relative to the flipped parent.
        if (transform.lossyScale.x < 0)
        {
            dir.x = -dir.x;
        }

        if (dir.sqrMagnitude <= 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
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
