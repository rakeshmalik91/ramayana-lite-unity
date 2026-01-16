using UnityEngine;
using System.Collections;

public class Arrow : MonoBehaviour {

	private bool hasHit = false;
	private PlayerArcher player;
	private ArrowType type;

	private const float DEATH_TIME = 30f;
	private float deathTimer = DEATH_TIME;
	public GameObject sparkParticleSystem;

	void FixedUpdate () {
		if (!hasHit) {
			SetAngle ();
		} else if (deathTimer > 0) {
			deathTimer -= Time.deltaTime;
			if(GetComponent<SpriteRenderer>().color.a > 0) {
				Color color = GetComponent<SpriteRenderer> ().color;
				GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, deathTimer / DEATH_TIME);
			}
		} else {
			Destroy (gameObject);
		}
	}

	void OnCollisionEnter2D(Collision2D collision) {
		if (!hasHit) {
			hasHit = true;
			for(int i = 0; i < transform.childCount; i++) {
				Destroy (transform.GetChild(i).gameObject);
			}
			if (collision.gameObject.layer == LayerMask.NameToLayer (Layers.PLAYER) || collision.gameObject.layer == LayerMask.NameToLayer (Layers.ENEMY)) {
				collision.gameObject.GetComponent<Character> ().Hit (type.damage, collision.contacts[0].point);
				GetComponent<SpriteRenderer> ().color = new Color (1, 1, 1, 0);
			} else {
				GameObject ps = GameObject.Instantiate (sparkParticleSystem);
				ps.transform.parent = transform;
				ps.transform.position = collision.contacts[0].point;
				GetComponent<Rigidbody2D> ().isKinematic = true;
				GetComponent<Collider2D> ().isTrigger = true;
			}
		}
	}

	//creates and arrow and shoot in given angle
	public static void CreateAndShoot(PlayerArcher player, ArrowType type) {
		float angle = player.GetAimAngle ();
		Vector3 dir = new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle), 0);
		float speed = player.GetAimForce ();

		GameObject arrow = GameObject.Instantiate(type.prefab);
		arrow.transform.position = player.GetArrowLaunchPosition() + dir;
		arrow.GetComponent<Rigidbody2D>().AddForce (dir * Mathf.Pow(speed * Game.ARROW_SPEED, 2), ForceMode2D.Impulse);
		arrow.GetComponent<Arrow>().SetAngle ();
		arrow.GetComponent<Arrow>().player = player;
		arrow.GetComponent<Arrow>().type = type;
	}

	private void SetAngle() {
		Rigidbody2D rigidbody = GetComponent<Rigidbody2D> ();
		if (rigidbody != null) {
			Vector3 dir = rigidbody.linearVelocity;
			float angle = Mathf.Atan2 (dir.y, dir.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.AngleAxis (angle, Vector3.forward);
		}
	}
}
