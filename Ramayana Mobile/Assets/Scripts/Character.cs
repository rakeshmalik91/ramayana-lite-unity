using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Collider2D), typeof (Rigidbody2D))]
[RequireComponent (typeof (CharacterAnimation))]

public abstract class Character : MonoBehaviour {

	protected CharacterAnimation sprite;
	protected Rigidbody2D rigidbody2d;
	protected Collider2D collider2d;

	protected virtual void Start () {
		sprite = GetComponent<CharacterAnimation>();

		rigidbody2d = GetComponent<Rigidbody2D>();
		collider2d = GetComponent<Collider2D>();
		rigidbody2d.freezeRotation = true;

		initScale = new Vector3 (Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

		currentJumpSpeed = jumpSpeed.min;
	}



	//******************************************** Movement ************************************************

	private bool onGround = false;
	private bool jumping = false;

	public float speed = 6f;
	public float runSmoothness = 0.05f;
	public Range jumpSpeed = new Range(3f, 6f);
	public float jumpSpeedIncreaseRate = 15f;

	protected virtual void OnCollisionStay2D(Collision2D collision) {
		if (collision.gameObject.CompareTag (Tags.GROUND)) {
			onGround = true;
		}
	}
	protected virtual void OnCollisionExit2D(Collision2D collision) {
		if (collision.gameObject.CompareTag (Tags.GROUND)) {
			onGround = false;
		}
	}
	protected virtual void FixedUpdate () {
		if (!dead) {
			HandleRun ();
			HandleJump ();
		}
		ControlAnimation();
	}



	//******************************************** Face Direction ************************************************

	private Vector3 initScale;

	public void SetFaceDirection(float direction) {
		transform.localScale = new Vector3 (Mathf.Sign(direction) * initScale.x, initScale.y, initScale.z);
	}

	public float GetFaceDirection() {
		return Mathf.Sign (transform.localScale.x);
	}



	//******************************************** Run ************************************************

	protected abstract float GetRunVelocity ();

	protected bool IsFacingWall(float direction) {
		int layerMask = LayerMask.GetMask(new string[] {Layers.GROUND});
		float xOffset = 0.01f; //used to ignore raycast to own collider
		float yOffset = 0.1f; //used to ignore slopes
		float dist = 0.01f;
		if (direction > 0) { 
			Vector2 origin = new Vector2 (collider2d.bounds.max.x + xOffset, collider2d.bounds.min.y + yOffset);
			return Physics2D.Raycast (origin, Vector2.right, dist, layerMask);
		} else if (direction < 0) {
			Vector2 origin = new Vector2 (collider2d.bounds.min.x - xOffset, collider2d.bounds.min.y + yOffset);
			return Physics2D.Raycast (origin, Vector2.left, dist, layerMask);
		}
		return false;
	}

	protected virtual void HandleRun() {
		if (isJumping()) {
			return;
		}

		float newVelocity = GetRunVelocity ();

		//dont try to run into a wall
		if (IsFacingWall(newVelocity)) {
			newVelocity = 0;
		}

		//set max run velocity
		newVelocity *= speed; 	

		//smoothen
		newVelocity = Mathf.Lerp (rigidbody2d.linearVelocity.x, newVelocity, runSmoothness);

		//get velocity diff for force
		float runForce = newVelocity - rigidbody2d.linearVelocity.x;

		//apply run force
		if (runForce != 0) {
			rigidbody2d.AddForce (new Vector2 (runForce * Time.deltaTime * 60, 0f), ForceMode2D.Impulse);
		}
	}



	//******************************************** Jump ************************************************

	private float currentJumpSpeed;
	private float aiJumpWaitTimer = 0f;

	protected virtual bool isPreparingForJump() {
		return aiJumpWaitTimer > 0f;
	}

	protected virtual void HandleJump() {
		//check for jump trigger
		if(activeJumpTrigger != null) {
			InvokeJump (Mathf.Sign (activeJumpTrigger.direction), activeJumpTrigger.force);
		}

		//jump
		if (isOnGround()) {
			//preparing for jump
			if (isPreparingForJump()) {
				jumping = true;
				currentJumpSpeed = Mathf.Clamp (currentJumpSpeed + Time.deltaTime * jumpSpeedIncreaseRate, jumpSpeed.min, jumpSpeed.max);
				//stop running when reached half max jump speed
				if (currentJumpSpeed >= (jumpSpeed.max + jumpSpeed.min) / 2) {
					rigidbody2d.linearVelocity = Vector3.zero;
				}
				//update ai variables
				aiJumpWaitTimer -= Time.deltaTime;
			}
			//has prepared to jump
			if (isJumping() && !isPreparingForJump()) {
				float jumpForce = currentJumpSpeed;
				jumping = false;
				currentJumpSpeed = jumpSpeed.min;
				//apply jump force
				rigidbody2d.AddForce (new Vector2 (0f, jumpForce), ForceMode2D.Impulse);
				//reset ai variables
				aiJumpWaitTimer = 0f;
			}
		} else {
			jumping = false;
			currentJumpSpeed = jumpSpeed.min;
		}
	}

	protected virtual void InvokeJump(float direction, float force) {
		if (isOnGround () && !isPreparingForJump ()) {
			if (direction == GetFaceDirection()) {
				aiJumpWaitTimer = force;
			}
		}
	}



	//******************************************** Triggers ************************************************

	protected JumpTrigger activeJumpTrigger = null;

	protected abstract bool TriggerAffects(Trigger trigger);

	protected virtual void OnTriggerEnter2D(Collider2D collider) {
		if (collider.isTrigger) {
			Trigger trigger = collider.GetComponent<Trigger> ();
			if (trigger == null) {
				return;
			} else if (this is Player && trigger.affectPlayer || this is Enemy && trigger.affectEnemy) {
				if (trigger.type == Trigger.JUMP_TRIGGER) {
					activeJumpTrigger = (JumpTrigger)trigger;
				}
			}
		}
	}

	protected virtual void OnTriggerExit2D(Collider2D collider) {
		if (collider.isTrigger) {
			Trigger trigger = collider.GetComponent<Trigger> ();
			if (trigger == null) {
				return;
			} else if (TriggerAffects(trigger)) {
				if (trigger.type == Trigger.JUMP_TRIGGER) {
					activeJumpTrigger = null;
				}
			}
		}
	}



	//******************************************** Animation ************************************************

	protected virtual void ControlAnimation() {
		if (IsDead ()) {
			sprite.Die ();
		} else if(flinchTime > 0) {
			sprite.Flinch();
		} else if(IsAttacking()) {
			sprite.Attack();
		} else if (isJumping ()) {
			sprite.Jump ();
		} else if (isOnGround ()) {
			if (isRunning ()) {
				sprite.Walk (rigidbody2d.linearVelocity.x / 4f);
			} else {
				sprite.Idle ();
			}
		} else {
			sprite.Glide ();
		}
	}



	//******************************************** Fighting ************************************************

	public int health = 10;
	private bool dead = false;
	private float flinchTime = 0f;

	public GameObject bloodParticleSystem;

	protected abstract void HandleAttack();

	protected virtual void Update() {
		if (flinchTime > 0) {
			flinchTime -= Time.deltaTime;
		} else {
			HandleAttack ();
		}
	}

	public virtual void Hit(int damage, Vector2 collisionPoint) {
		if (dead) {
			return;
		}

		if (collisionPoint != Vector2.zero) {
			GameObject blood = GameObject.Instantiate (bloodParticleSystem);
			blood.transform.parent = transform;
			blood.transform.position = collisionPoint;
		}

		flinchTime = 0.1f;
		rigidbody2d.AddForce ((new Vector2(GetBounds().center.x, GetBounds().center.y) - collisionPoint) * 5, ForceMode2D.Impulse);

		health -= damage;
		if (health <= 0) {
			health = 0;
			dead = true;
			rigidbody2d.isKinematic = true;
			collider2d.isTrigger = true;
		}
	}



	//******************************************** Utility ************************************************

	public virtual bool isJumping () {
		return jumping;
	}

	public virtual bool isOnGround () {
		return onGround; //&& Mathf.Abs (rigidbody2d.velocity.y) <= 0.1f;
	}

	public virtual bool isRunning () {
		return Mathf.Abs (rigidbody2d.linearVelocity.x) >= 0.2f;
	}

	public virtual Bounds GetBounds() {
		return collider2d.bounds;
	}

	public virtual bool IsDead() {
		return dead;
	}

	public virtual bool IsAttacking() {
		return false;
	}

	public float HorDist(Transform source) {
		return Mathf.Abs (transform.position.x - source.position.x);
	}

	public static Character Nearest(Transform source) {
		float minDist = Mathf.Infinity;
		Character nearest = null;
		foreach(Character character in GameObject.FindObjectsOfType<Character> ()) {
			float dist = character.HorDist (source);
			if (!character.IsDead() && dist < minDist) {
				minDist = dist;
				nearest = character;
			}
		}
		return nearest;
	}

}

