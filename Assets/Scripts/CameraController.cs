using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    private Vector3 offset;
    //Velocidad de reposicionamiento de la camara
    public float smoothSpeed = 0.01f;
    //Tamaño de la pantalla
    public Vector2 screenBounds;
    private Vector2 playerBounds;
    private Vector3 playerOffset;

    private void Start()
    {
        offset = player.transform.position - transform.position;
        Vector3 bounds = player.GetComponent<SpriteRenderer>().bounds.size;
        playerBounds = new Vector2(bounds.x,bounds.y)/2.0f;
        playerOffset = transform.position - player.position;
    }

    private void FixedUpdate()
    {
        //float initialX = transform.position.x;
        Vector3 desired = player.position + playerOffset; //- offset;

        //Deplazamos la cámara en el eje X con un movimiento suave y progresivo
        Vector3 smoothPos = Vector3.Lerp(transform.position, desired, smoothSpeed);
        transform.position = new Vector3(smoothPos.x, smoothPos.y, transform.position.z);

        // //Bloqueamos al jugador para que no salga de la pantalla en el eje X
        // screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(0,0, Camera.main.transform.position.z));
        // float leftBorder = screenBounds.x + playerBounds.x;
        // Vector3 pos = player.transform.position;
        // pos.x = Mathf.Max (pos.x, leftBorder);
        // player.transform.position = pos;
    }
}
