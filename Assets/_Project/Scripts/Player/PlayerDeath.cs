
using UnityEngine;
using Inanna.Combat;

public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    private bool hasDied;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Died += Die;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= Die;
    }

    
private void Die()
{
    if (hasDied) return;
    hasDied = true;

    Debug.Log("INANNA MORREU!");

    CharacterController controller = GetComponent<CharacterController>();

    if (controller != null)
        controller.enabled = false;
    // Desativa movimento, combate e outros controles
    foreach (MonoBehaviour script in scriptsToDisable)
    {
        if (script != null)
            script.enabled = false;
    }

    // Para a física
    Rigidbody rb = GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    // Desativa os colisores
    Collider[] colliders = GetComponentsInChildren<Collider>();
    foreach (Collider col in colliders)
        col.enabled = false;

    // Escurece a personagem para indicar morte
    Renderer[] renderers = GetComponentsInChildren<Renderer>();
    foreach (Renderer rend in renderers)
        rend.material.color = Color.gray;
}

}
