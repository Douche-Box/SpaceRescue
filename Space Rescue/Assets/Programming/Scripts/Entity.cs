using UnityEngine;

public class Entity : MonoBehaviour
{
    public EntityType entityType;

    public enum EntityType
    {
        SCRAP,
        ROBOT,
        ENEMY,
    }

    [Header("Type")]
    public Type type;

    public enum Type
    {
        NONE,
        FIRE,
        WATER,
    }

    [Header("Info")]

    public float health;
    public float maxHealth;

    public float speed;
    public float rotationSpeed;
    public float damage;

    public virtual void Start() { }

    public virtual void Update() { }

    public virtual void TakeDamage(float damage)
    {
        Debug.Log($"{this.gameObject.name} takes {damage} damage");
        health -= damage;

        if (health <= 0)
        {
            health = 0;

            Death();
        }
    }

    public virtual void Death()
    {
        Destroy(this.gameObject);
    }


}
