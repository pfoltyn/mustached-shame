using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssemblyCSharp;

class Line {
	public GameObject gameObject;
	public int blackTilePosition;
	public bool tapped;
}

enum GameState {
	GameStart,
	GameInProgress,
	GameSaveResult,
	GameOver
}

public class CreateRandomSprite : MonoBehaviour {
	const int TILES_PER_LINE = 4;
	const float INITIAL_VELOCITY = -8.0f;
	const int EFFECT_SPEED = 8;

	public GUISkin guiSkin;

	GameState gameState;
	Vector3 respawnDistance;
	Vector3 finishPosition;
	Vector3 gameOverPosition;
	RandomIntSequence rndSeq;
	List<Line> lines;
	int nextToTapIndex;

	void OnGUI() {
		if (gameState == GameState.GameOver) {
			GUI.skin = guiSkin;
			GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

			int width = Screen.width;
			int height = Screen.height / 8;
			int x = Screen.width / 2 - width / 2;
			int y = Screen.height / 3 - height / 2;
			GUI.Label(new Rect(x, y, width, height), "High Score");
			GUI.Label(new Rect(x, y + 75, width, height), PlayerPrefs.GetFloat("highScore").ToString("N2") + " m/s");

			width = Screen.width / 2;
			height = Screen.height / 8;
			x = Screen.width / 2 - width / 2;
			y = (int)(Screen.height * 0.75f) - height / 2;
			if(GUI.Button(new Rect(x, y, width, height), "Again?")) {
				lines.ForEach(delegate(Line obj) {
					obj.gameObject.rigidbody2D.velocity = new Vector2(0.0f, 0.0f);
				});

				for (int lineIndex = 0; lineIndex < lines.Count; ++lineIndex) {
					if (!lines[lineIndex].tapped) {
						GameObject obj = GameObject.FindGameObjectWithTag("StartText");
						obj.transform.position = new Vector3(
							-2.4f + lines[lineIndex].blackTilePosition * 1.6f,
							lines[lineIndex].gameObject.transform.position.y,
							-1.0f);
						obj.renderer.enabled = true;
						break;
					}
				}

				GameObject velocityText = GameObject.FindGameObjectWithTag("VelocityText");
				velocityText.GetComponent<TextMesh>().text = "0.00 m/s";

				gameState = GameState.GameStart;
			}
		}
	}

	// Use this for initialization
	void Start() {
		rndSeq = new RandomIntSequence(TILES_PER_LINE);
		lines = new List<Line>();

		// Calculate by how much we need to move bottom line to put it on top
		GameObject respawnObject = GameObject.FindGameObjectWithTag("Respawn");
		GameObject finishObject = GameObject.FindGameObjectWithTag("Finish");
		respawnDistance = respawnObject.transform.position - finishObject.transform.position;
		finishPosition = finishObject.transform.position;

		GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Tiles");
		System.Array.Sort(gameObjects, delegate (GameObject t0, GameObject t1) {
			return (int)(t0.transform.position.y - t1.transform.position.y);
		});

		foreach (var gameObject in gameObjects) {
			Line line = new Line();
			line.gameObject = gameObject;
			line.blackTilePosition = rndSeq.Next();
			line.tapped = false;
			lines.Add(line);

			Animator animator = gameObject.GetComponent<Animator>();
			animator.SetFloat("Animation", line.blackTilePosition);
		}

		GameObject gameOver = GameObject.FindGameObjectWithTag("GameOver");
		gameOverPosition = gameOver.transform.position;

		GameObject.FindGameObjectWithTag("StartText").transform.position = new Vector3(
			-2.4f + lines[0].blackTilePosition * 1.6f, -3.75f, -1.0f);

		nextToTapIndex = 0;
		gameState = GameState.GameStart;
	}

	delegate void ProcessLineDelegate(Line line, int tappedTileIndex);
	
	void ForEachTouch(ProcessLineDelegate processLine) {
		int touchIndex = 0;
		while (touchIndex < Input.touchCount) {
			Touch touch = Input.GetTouch(touchIndex);
			if (touch.phase == TouchPhase.Began) {
				Ray ray = Camera.main.ScreenPointToRay(touch.position);
				RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
				Line line = lines.Find(delegate(Line obj) {
					return obj.gameObject == hit.collider.gameObject;
				});
				if (line == lines[nextToTapIndex]) {
					int tappedTileIndex = (int)((touch.position.x / Screen.width) * TILES_PER_LINE);
					processLine(line, tappedTileIndex);
				}
			}
			++touchIndex;
		}
	}

	void GameStartUpdate() {
		ForEachTouch(delegate(Line line, int tappedTileIndex) {
			if (line.blackTilePosition == tappedTileIndex) {
				SpriteRenderer spriteRenderer = line.gameObject.GetComponent<SpriteRenderer>();
				Material tempMaterial = new Material(spriteRenderer.material);
				tempMaterial.SetColor("_TintColor", Color.gray);
				tempMaterial.SetFloat("_FieldNumber", 1.0f + tappedTileIndex);
				spriteRenderer.material = tempMaterial;

				line.tapped = true;
				nextToTapIndex = (nextToTapIndex + 1) % lines.Count;

				GameObject.FindGameObjectWithTag("StartText").renderer.enabled = false;
				gameState = GameState.GameInProgress;
			}
		});
	}
	
	void GameInProgressUpdate() {
		ForEachTouch(delegate(Line line, int tappedTileIndex) {
			SpriteRenderer spriteRenderer = line.gameObject.GetComponent<SpriteRenderer>();
			Material tempMaterial = new Material(spriteRenderer.material);
			if (line.blackTilePosition == tappedTileIndex) {
				tempMaterial.SetColor("_TintColor", Color.gray);
			} else {
				tempMaterial.SetColor("_TintColor", Color.red);
				gameState = GameState.GameSaveResult;
			}
			tempMaterial.SetFloat("_FieldNumber", 1.0f + tappedTileIndex);
			spriteRenderer.material = tempMaterial;

			line.tapped = true;
			nextToTapIndex = (nextToTapIndex + 1) % lines.Count;
		});
	}

	void Update() {
		switch (gameState) {
		case GameState.GameStart:
			GameStartUpdate();
			break;
		case GameState.GameInProgress:
			GameInProgressUpdate();
			break;
		default:
			break;
		}
	}

	void MoveLineToTheTop() {
		Line line = lines[0];
		// Move line to the top of the screen.
		line.gameObject.transform.Translate(respawnDistance);
		
		// Reset color of tapped tile.
		SpriteRenderer spriteRenderer = line.gameObject.GetComponent<SpriteRenderer>();
		Material tempMaterial = new Material(spriteRenderer.material);
		tempMaterial.SetFloat("_FieldNumber", 0.0f);
		spriteRenderer.material = tempMaterial;
		
		// Select new black tile possition.
		int blackTilePosition = rndSeq.Next();
		Animator animator = line.gameObject.GetComponent<Animator>();
		animator.SetFloat("Animation", blackTilePosition);
		
		if (nextToTapIndex > 0) {
			--nextToTapIndex;
		}
		
		line.tapped = false;
		line.blackTilePosition = blackTilePosition;
		lines.Add(line);
		lines.RemoveAt(0);
	}

	void GameInProgressFixedUpdate() {
		Line line = lines[0];
		if (line.gameObject.transform.position.y <= finishPosition.y) {
			if (line.tapped) {
				MoveLineToTheTop();
			} else {
				gameState = GameState.GameSaveResult;
			}
		}

		if (line.gameObject.rigidbody2D.velocity.y > INITIAL_VELOCITY) {
			lines.ForEach(delegate(Line obj) {
				obj.gameObject.rigidbody2D.gravityScale = Mathf.Lerp(
					obj.gameObject.rigidbody2D.gravityScale, 1.0f, 2 * Time.deltaTime);
				obj.gameObject.rigidbody2D.velocity = new Vector2(
					0.0f, Mathf.Lerp(obj.gameObject.rigidbody2D.velocity.y, INITIAL_VELOCITY, 2 * Time.deltaTime));
			});
		}

		// Display current speed
		float speed = -1.0f * line.gameObject.rigidbody2D.velocity.y;
		GameObject.FindGameObjectWithTag("VelocityText").GetComponent<TextMesh>().text = speed.ToString("N2") + " m/s";
	}

	void GameOverFixedUpdate() {
		lines.ForEach(delegate(Line obj) {
			obj.gameObject.rigidbody2D.gravityScale = Mathf.Lerp(
				obj.gameObject.rigidbody2D.gravityScale, 0.0f, EFFECT_SPEED * Time.deltaTime);
			obj.gameObject.rigidbody2D.velocity = new Vector2(
				0.0f, Mathf.Lerp(obj.gameObject.rigidbody2D.velocity.y, 0.0f, EFFECT_SPEED * Time.deltaTime));
		});

		Line line = lines[0];
		for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++) {
			if (!lines[lineIndex].tapped) {
				line = lines[lineIndex];
				break;
			}
		}
		Vector3 previousPosition = line.gameObject.transform.position;
		line.gameObject.transform.position = Vector3.Lerp(
			previousPosition, gameOverPosition, EFFECT_SPEED * Time.deltaTime);
		Vector3 distance = line.gameObject.transform.position - previousPosition;
		lines.ForEach(delegate(Line obj) {
			if (obj != line) {
				obj.gameObject.transform.Translate(distance);
			}
		});
		if (lines[0].gameObject.transform.position.y <= finishPosition.y) {
			MoveLineToTheTop();
		}

	}

	void GameSaveResult() {
		float score = -1.0f * lines[0].gameObject.rigidbody2D.velocity.y;
		float highScore = PlayerPrefs.GetFloat("highScore");
		if (highScore < score) {
			PlayerPrefs.SetFloat("highScore", score);
		}
		PlayerPrefs.Save();
		gameState = GameState.GameOver;
	}

	void FixedUpdate() {
		switch (gameState) {
		case GameState.GameInProgress:
			GameInProgressFixedUpdate();
			break;
		case GameState.GameSaveResult:
			GameSaveResult();
			break;
		case GameState.GameOver:
			GameOverFixedUpdate();
			break;
		default:
			break;
		}
	}
}
