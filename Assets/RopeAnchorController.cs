using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeAnchorController : MonoBehaviour
{
    public bool isFiring;
    public Vector2 firingPosition;
    public float ropeMaxCastDistance = 20f;

    public Action<Vector2> OnAttachAnchor;
    public Action OnBreakAnchor;

    private Rigidbody2D rBody;

    private void Awake() {
        rBody = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        if (!isFiring) return;

        if (Vector2.Distance(transform.position, firingPosition) > ropeMaxCastDistance) {
            OnBreakAnchor();
            transform.position = firingPosition;
            rBody.velocity = Vector2.zero;

            return;
        }
    }

    void OnCollisionEnter2D(Collision2D collision) {
        if (!isFiring) return;

        isFiring = false;
        OnAttachAnchor(collision.GetContact(0).point);
    }
}
