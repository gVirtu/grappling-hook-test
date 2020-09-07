using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RopeSystem : MonoBehaviour
{
    public GameObject ropeHingeAnchor;
    public DistanceJoint2D ropeJoint;
    public Transform crosshair;
    public SpriteRenderer crosshairSprite;
    public PlayerMovement playerMovement;
    private bool ropeAttached;
    private bool isFiring;
    private Vector2 playerPosition;
    private Vector2 firingPosition;
    private Vector2 anchorShotPosition;
    private Vector2 anchorShotVelocity;
    private Rigidbody2D ropeHingeAnchorRb;
    private SpriteRenderer ropeHingeAnchorSprite;

    public LineRenderer ropeRenderer;
    public LayerMask ropeLayerMask;
    private List<Vector2> ropePositions = new List<Vector2>();

    private bool distanceSet;

    private Dictionary<Vector2, int> wrapPointsLookup = new Dictionary<Vector2, int>();

    public float ropeMaxCastDistance = 20f;
    public float climbSpeed = 3f;
    public float firingSpeed = 0.2f;
    public bool isColliding;

    void Awake() {
        ropeJoint.enabled = false;
        playerPosition = transform.position;
        ropeHingeAnchorRb = ropeHingeAnchor.GetComponent<Rigidbody2D>();
        ropeHingeAnchorSprite = ropeHingeAnchor.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        var worldMousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));
        var facingDirection = worldMousePosition - transform.position;
        var aimAngle = Mathf.Atan2(facingDirection.y, facingDirection.x);

        if (aimAngle < 0f) {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }

        var aimDirection = Quaternion.Euler(0, 0, aimAngle * Mathf.Rad2Deg) * Vector2.right;

        playerPosition = transform.position;

        if (ropeAttached) {
            crosshairSprite.enabled = false;

            playerMovement.isSwinging = true;
            playerMovement.ropeHook = ropePositions.Last();

            if (ropePositions.Count > 0) {
                var lastRopePoint = ropePositions.Last();
                var playerToCurrentNextHit = Physics2D.Raycast(
                    playerPosition,
                    (lastRopePoint - playerPosition).normalized,
                    Vector2.Distance(playerPosition, lastRopePoint) - 0.1f,
                    ropeLayerMask
                );

                if (playerToCurrentNextHit) {
                    var colliderWithVertices = playerToCurrentNextHit.collider as PolygonCollider2D;

                    if (colliderWithVertices != null) {
                        var closestPointToHit = GetClosestColliderPointFromRaycastHit(playerToCurrentNextHit, colliderWithVertices);

                        if (wrapPointsLookup.ContainsKey(closestPointToHit)) {
                            ResetRope();
                            return;
                        }

                        ropePositions.Add(closestPointToHit);
                        wrapPointsLookup.Add(closestPointToHit, 0);
                        distanceSet = false;
                    }
                }
            }
        } else {
            playerMovement.isSwinging = false;
            SetCrosshairPosition(aimAngle);
        }

        HandleInput(aimDirection);
        UpdateAnchorShot();
        UpdateRopePositions();
        HandleRopeLength();
        HandleRopeUnwrap();
    }

    private void SetCrosshairPosition(float aimAngle) {
        if (!crosshairSprite.enabled) {
            crosshairSprite.enabled = true;
        }

        var x = transform.position.x + 1f * Mathf.Cos(aimAngle);
        var y = transform.position.y + 1f * Mathf.Sin(aimAngle);

        var crossHairPosition = new Vector3(x, y, 0);
        crosshair.transform.position = crossHairPosition;
    }

    private void HandleInput(Vector2 aimDirection) {
        if (Input.GetMouseButtonDown(0)) {
            if (ropeAttached)
                ResetRope();

            isFiring = true;
            firingPosition = transform.position;
            anchorShotPosition = firingPosition;
            anchorShotVelocity = aimDirection.normalized * firingSpeed;

            ropeRenderer.enabled = true;

            //var hit = Physics2D.Raycast(playerPosition, aimDirection, ropeMaxCastDistance, ropeLayerMask);

            //if (hit.collider != null) {
            //    ropeAttached = true;
            //    if (!ropePositions.Contains(hit.point)) {
            //        // Jump slightly to distance the player a little from the ground
            //        // after grappling to something.
            //        transform.GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, 2f), ForceMode2D.Impulse);
            //        ropePositions.Add(hit.point);
            //        ropeJoint.distance = Vector2.Distance(playerPosition, hit.point);
            //        ropeJoint.enabled = true;
            //        ropeHingeAnchorSprite.enabled = true;
            //    }
            //} else {
            //    ropeRenderer.enabled = false;
            //    ropeAttached = false;
            //    ropeJoint.enabled = false;
            //}
        }

        if (ropeAttached && Input.GetButton("Jump")) {
            ResetRope();
        }
    }

    private void HandleRopeConnect(Vector2 point) {
        ropeAttached = true;
        if (!ropePositions.Contains(point)) {
            // Jump slightly to distance the player a little from the ground
            // after grappling to something.
            transform.GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, 2f), ForceMode2D.Impulse);
            ropePositions.Add(point);
            ropeJoint.distance = Vector2.Distance(playerPosition, point);
            ropeJoint.enabled = true;
            ropeHingeAnchorSprite.enabled = true;
        }
    }

    private void ResetRope() {
        ropeJoint.enabled = false;
        ropeAttached = false;
        playerMovement.isSwinging = false;
        isFiring = false;
        ropeRenderer.enabled = false;
        ropeRenderer.positionCount = 2;
        ropeRenderer.SetPosition(0, transform.position);
        ropeRenderer.SetPosition(1, transform.position);
        ropePositions.Clear();
        ropeHingeAnchorSprite.enabled = false;
        wrapPointsLookup.Clear();
    }

    private void UpdateAnchorShot() {
        if (!isFiring) return;

        anchorShotPosition += anchorShotVelocity;

        if (Vector2.Distance(anchorShotPosition, firingPosition) > ropeMaxCastDistance) {
            ResetRope();
        }

        var aim = anchorShotPosition - firingPosition;
        var aimDirection = aim.normalized;
        var aimDistance = aim.magnitude;

        var hit = Physics2D.Raycast(firingPosition, aimDirection, aimDistance, ropeLayerMask);

        if (hit.collider != null && !ropePositions.Contains(hit.point)) {
            isFiring = false;
            ropeAttached = true;

            // Jump slightly to distance the player a little from the ground
            // after grappling to something.
            transform.GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, 2f), ForceMode2D.Impulse);
            ropePositions.Add(hit.point);
            ropeJoint.distance = Vector2.Distance(playerPosition, hit.point);
            ropeJoint.enabled = true;
            ropeHingeAnchorSprite.enabled = true;
        }
    }

    private void UpdateRopePositions() {
        if (!ropeAttached) {
            if (isFiring) {
                ropeRenderer.SetPositions(new[] {(Vector3) anchorShotPosition, transform.position});
            }

            return;
        }

        ropeRenderer.positionCount = ropePositions.Count + 1;

        for (var i = ropeRenderer.positionCount - 1; i >= 0; --i) {
            if (i == ropeRenderer.positionCount - 1) {
                // Last line point is player
                ropeRenderer.SetPosition(i, transform.position);
            } else {
                ropeRenderer.SetPosition(i, ropePositions[i]);
            }
        }

        var ropePosition = ropePositions.Last();

        ropeHingeAnchorRb.transform.position = ropePosition;

        if (!distanceSet) {
            ropeJoint.distance = Vector2.Distance(transform.position, ropePosition);
            distanceSet = true;
        }
    }

    private Vector2 GetClosestColliderPointFromRaycastHit(RaycastHit2D hit, PolygonCollider2D polyCollider) {
        var distanceDictionary = polyCollider.points.ToDictionary<Vector2, float, Vector2>(
                position => Vector2.Distance(hit.point, polyCollider.transform.TransformPoint(position)),
                position => polyCollider.transform.TransformPoint(position)
        );

        var orderedDictionary = distanceDictionary.OrderBy(e => e.Key);
        return orderedDictionary.Any() ? orderedDictionary.First().Value : Vector2.zero;    
    }

    private void HandleRopeLength() {
        if (!ropeAttached) return;

        float verticalInput = Input.GetAxisRaw("Vertical");

        if (verticalInput > 0f && !isColliding) {
            ropeJoint.distance -= Time.deltaTime * climbSpeed;
        } else if (verticalInput < 0f) {
            ropeJoint.distance += Time.deltaTime * climbSpeed;
        }
    }

    // TODO: Fix this, there are no Triggers.
    void OnTriggerStay2D(Collider2D colliderStay) {
        isColliding = true;
    }

    void OnTriggerExit2D(Collider2D colliderStay) {
        isColliding = false;
    }

    private void HandleRopeUnwrap() {
        if (ropePositions.Count <= 1) return;

        var anchorIndex = ropePositions.Count - 2;
        var hingeIndex = ropePositions.Count - 1;
        var anchorPosition = ropePositions[anchorIndex];
        var hingePosition = ropePositions[hingeIndex];
        var hingeDir = hingePosition - anchorPosition;
        var playerDir = playerPosition - hingePosition;
        var angleDirSign = (int) Mathf.Sign(AngleDir(hingeDir, playerDir));

        if (!wrapPointsLookup.ContainsKey(hingePosition)) {
            Debug.LogError("We were not tracking hingePosition (" + hingePosition + ") in the look up dictionary.");
            return;
        }

        if (wrapPointsLookup[hingePosition] != 0 && wrapPointsLookup[hingePosition] != angleDirSign) {
            UnwrapRopePosition(anchorIndex, hingeIndex);
            return;
        }

        wrapPointsLookup[hingePosition] = angleDirSign;
    }

    // This returns a negative number if B is left of A, positive if right of A, or 0 if they are perfectly aligned.
    private float AngleDir(Vector2 A, Vector2 B) {
        return -A.x * B.y + A.y * B.x;
    }

    private void UnwrapRopePosition(int anchorIndex, int hingeIndex) {
        var newAnchorPosition = ropePositions[anchorIndex];
        wrapPointsLookup.Remove(ropePositions[hingeIndex]);
        ropePositions.RemoveAt(hingeIndex);

        ropeHingeAnchorRb.transform.position = newAnchorPosition;
        distanceSet = false;

        if (distanceSet) return;

        ropeJoint.distance = Vector2.Distance(transform.position, newAnchorPosition);
        distanceSet = true;
    }
}
 