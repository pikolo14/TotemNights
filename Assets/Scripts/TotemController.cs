using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TotemController : MonoBehaviour
{
    public Image endScreen;
    public GameObject endTitle;


    //Singleton referencia
    public static TotemController totem;
    public SpriteRenderer sprite;
    public Rigidbody2D rb;
    public BoxCollider2D coll, groundColl;
    public HingeJoint2D baseRightJoint, baseLeftJoint, auxiliaryJoint, currentJoint;
    public Transform basePoint, topPoint;
    public GameObject piecePrefab;
    public bool falling = false;
    public float fallForce = 1;

    //Anclajes
    private GameObject currentAnchor;
    private bool anchored = false;
    public float anchorForce = 1;
    public float maxAnchorVel = 20;

    //Movimiento y salto
    public bool moving = true;
    public bool grounded = false;
    public float moveSpeed = 4;
    public float brakeSpeed = 1;
    public float maxMoveSpeed = 4;
    public float jumpForce = 5;

    //Construir totem
    public List<PieceController> pieces;
    public List<PieceController> stackedPieces;
    public int nPieces = 4;
    public int totemPieces = 1;
    public float stackDelay = 0.2f;
    public float currStackTime;
    public PieceController stackingPiece;
    
    private float pieceHeight;

    //Vamos almacenando el camino que descibe el agente lider
    public static LinkedList<Vector2> path;
    //Distancia mínima entre los puntos del path
    public float pointThres = 0.5f;

    //CHECKPOINT
    //Esta en zona para hacer checkpoints?
    public bool onCheckZone = false;
    //Posicion checkpoint actual en el que reaparecer
    public Vector3 checkPointPos;
    public GameObject checkPiecePrefab;
    public bool dead = false;

    public bool collectDelay = false;

    public List<GameObject> collectablePieces, deletedPieces, checkDeletedPieces;

    public Sprite[] totemSprites;

    public int pieceCounter = 0;
    public TextMeshProUGUI counterText;
    

    void Awake()
    {
        //Singleton
        if(totem == null)
            totem = this;
        else
            Destroy(this);
    }

    void Start()
    {
        //Inicializar variables objeto
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<BoxCollider2D>();

        //Crear piezas iniciales asignandoles el orden (contamos al totem como una pieza)
        for(int i = 1; i < nPieces; i++)
        {
            GameObject piece = Instantiate(piecePrefab);
            PieceController contr = piece.GetComponent<PieceController>();
            contr.orderNumber = i;
            pieces.Add(contr);
        }

        //Obtener altura de pieza
        pieceHeight = coll.bounds.size.y;

        //Init piezas checkpoint
        collectablePieces = new List<GameObject>();
        deletedPieces = new List<GameObject>();
        checkDeletedPieces = new List<GameObject>();
        foreach(GameObject go in GameObject.FindGameObjectsWithTag(Globals.tagCollectable))
        {
            collectablePieces.Add(go);
        }

        //Registrar posicion inicial como checkpoint
        checkPointPos = transform.position;

        //musica fondo
        AudioManager.audioManager.Play("Music");
    }

    void Update()
    {
        if(!dead)
        {
            //Escape salir
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

            //Reset en la R
            if(Input.GetKeyDown(KeyCode.R))
            {
                StartCoroutine(Death());
            }

            //Cambiar numero de piezas del totem
            if(!falling && grounded)
            {
                //Mantener arriba para construir totem
                if(Input.GetKey(KeyCode.UpArrow) && stackingPiece==null && totemPieces<nPieces)
                {
                    Stack();
                }

                //Si se estaba apilando una pieza y se suelta la tecla, vuelve a su sitio
                if(stackingPiece!= null && Input.GetKeyUp(KeyCode.UpArrow))
                {
                    stackingPiece.Unstack(stackDelay);
                }

                //Pulsar abajo para quitar piezas del totem
                if(Input.GetKey(KeyCode.DownArrow) && stackingPiece==null && totemPieces>1 && !onCheckZone)
                {
                    Unstack(stackDelay);
                }
            }

            if(!onCheckZone)
            {
                //MODO PIEZA UNICA
                if(totemPieces == 1)
                {
                    //Desplazarse
                    float xAxis = Input.GetAxis("Horizontal");
                    if(xAxis!=0)
                        Move(xAxis);
                    //Decelerar    
                    else
                    {
                        float velDamping = 10f;
                        if(rb.velocity.x > 0)
                            rb.velocity = new Vector2(Mathf.Max(rb.velocity.x-velDamping*Time.deltaTime, 0), rb.velocity.y);
                        else if(rb.velocity.x < 0)
                            rb.velocity = new Vector2(Mathf.Min(rb.velocity.x+velDamping*Time.deltaTime, 0), rb.velocity.y);
                    }

                    //Saltar
                    if(Input.GetKeyDown(KeyCode.Space) && grounded)
                    {
                        Jump();
                    }
                }
                //MODO TOTEM
                else
                {
                    //Separar totem con espacio
                    if(Input.GetKeyDown(KeyCode.Space) && falling)
                    {
                        SeparatePieces();
                    }

                    //Desplomar con las flechas horizontales si es totem, si esta en el suelo y si no esta cayendo ya
                    if(!falling && grounded && !anchored && stackingPiece==null)
                    {
                        float xAxis = Input.GetAxis("Horizontal");
                        if(xAxis>0)
                            StartFall(-1);
                        else if(xAxis<0)
                            StartFall(1);
                    }

                    //Mover en anclaje con las flechas
                    if(anchored)
                    {
                        float xAxis = Input.GetAxis("Horizontal");
                        if(xAxis!=0)
                            RotateInAnchor(-xAxis);
                    }
                }

                //DEBUG: Mostrar camino que describe el lider con lineas de debug
                if(path!= null && path.Count!= 0)
                {
                    LinkedListNode<Vector2> node = path.First.Next;
                    for (int i = 1; i < path.Count; i++, node = node.Next)
                    {
                        Debug.DrawLine(node.Value, node.Previous.Value, new Color(1, 0, 0, 1));
                    }
                }
            }
        }
    }

    void FixedUpdate()
    {
        //Generación de puntos del path
        if(path == null || path.Count == 0)
        {
            path = new LinkedList<Vector2>();
            path.AddLast(Vec3ToVec2(transform.position));
        }
        else
        {
            float dist = Vector2.Distance(Vec3ToVec2(transform.position), path.Last.Value);
            //Se añade punto al path cuando se supera una distancia desde el ultimo punto registrado
            if(dist > pointThres)
            {
                path.AddLast(Vec3ToVec2(transform.position));
                //Debug.Log(path.Count);
            }
        }

        //Mantener erguido siempre
        if(totemPieces == 1)
        {
            rb.rotation = 0;
            rb.freezeRotation = true;
            //rb.angularVelocity = 0;
        }
    }

    void OnTriggerEnter2D(Collider2D coll)
    {
        //Choque con anclaje
        if(coll.tag == Globals.tagAnchor && falling && currentAnchor != coll.gameObject)
        {
            //Desactivamos temporalmente el anclaje anterior para evitar problemas
            if(anchored)
                StartCoroutine(TemporaryDisableAnchor(currentAnchor));
            
            currentAnchor = coll.gameObject;
            Anchor(coll.transform.position);
        }

        //Si toca la zona de checkpoint, colocar en su posicion para comenzar a construir totem y registrar nuevo checkpoint
        if((coll.tag == Globals.tagCheckpoint || coll.tag == Globals.tagEnd) && !onCheckZone && nPieces>1)
        {  
            pieceCounter += nPieces-1;

            //Crear hasta 100 piezas para el checkpoint final
            if(coll.tag == Globals.tagEnd)
            {
                for(int i = nPieces; i < 100; i++)
                {
                    GameObject piece = Instantiate(piecePrefab);
                    piece.transform.position = coll.transform.position;
                    PieceController contr = piece.GetComponent<PieceController>();
                    contr.SetSprite(totemSprites[Random.Range(0,3)]);
                    contr.orderNumber = i;
                    pieces.Add(contr);
                }

                nPieces = 100;

                //Actualizar contador
                counterText.text = pieceCounter + "/30";

                AudioManager.audioManager.Stop("Music");
            }

            onCheckZone = true;
            checkPointPos = coll.transform.position;
            coll.gameObject.SetActive(false);
            transform.position = checkPointPos;

            //Paramos totem
            rb.velocity = Vector3.zero;
            rb.angularVelocity = 0;

            if(falling)
            {
                SeparatePieces();
            }
        }

        //Final
        if(coll.tag == Globals.tagMoon)
        {
            StartCoroutine(EndSequece());
        }
    }

    IEnumerator EndSequece()
    {
        //Evitar que se mueva
        dead = true;
        AudioManager.audioManager.Play("FinalMusic");
        float time = 0f;
        float speed = 1f/8f;

        //Fundido a blanco
        while(time < 8)
        {
            Color col = Color.white;
            col.a = speed*time;
            endScreen.color = col;
            time += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(3);
        //Titulo del juego
        endTitle.SetActive(true);
        yield return new WaitForSeconds(11);
        Application.Quit();
    }

    void OnTriggerStay2D(Collider2D coll)
    {   
        //Recoger una nueva pieza
        if(coll.tag == Globals.tagCollectable && !collectDelay)
        {
            CollectPiece(coll.transform);
        }
    }

    //Añadimos velocidad hasta alcanzar la velocidad de movimiento máxima
    void Move(float move)
    {
        float newXVel = rb.velocity.x + (moveSpeed * move * Time.fixedDeltaTime);
        newXVel = Mathf.Clamp(newXVel, -maxMoveSpeed, maxMoveSpeed);
        rb.velocity = new Vector2(newXVel, rb.velocity.y);
        if(rb.velocity.x < 0)
            sprite.flipX = true;
        else
            sprite.flipX = false;
    }

    //Comenzar a derrumbar
    void StartFall(int dir)
    {
        falling = true;

        AudioManager.audioManager.Play("Fall");
        
        //Activar joint correspondiente a la base
        if(dir>0)
            currentJoint = baseLeftJoint;
        else
            currentJoint = baseRightJoint;

        currentJoint.enabled = true;

        //Comunicar impulso en la joint hacia la direccion elegida
        rb.freezeRotation = false;
        rb.AddTorque(dir*fallForce, ForceMode2D.Impulse);

        StartCoroutine(FallingEnd());
    }

    //Detectar si se ha caido al suelo para desmontarse automaticamente
    IEnumerator FallingEnd()
    {
        float threshold = 0.1f;
        float maxTime = 0.15f;
        float currTime = 0;
        Quaternion lastRot = transform.rotation;

        while (!anchored && falling)
        {
            yield return null;
            //Si se ha movido poco desde la ultima iteracion
            if(Mathf.Abs(Quaternion.Angle(transform.rotation, lastRot)) < threshold)
            {
                currTime += Time.deltaTime;
                //Si se ha estado x tiempo sin moverse apenas, separar automáticamente
                if(currTime > maxTime)
                {
                    SeparatePieces();
                    yield break;
                }
            }
            else
            {
                currTime = 0;
                lastRot = transform.rotation;
            }
        }
    }

    //Cambiar si se toca o no el suelo estando de pie
    public void SetGrounded(bool _grounded)
    {
        grounded = _grounded;
    }

    //Saltar con la pieza lider del totem
    void Jump()
    {
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        //TODO: Animacion y sonido salto
        AudioManager.audioManager.Play("Jump");
    }

    //Comenzar a apilar una pieza
    void Stack()
    { 
        //Paramos totem
        rb.velocity = Vector3.zero;
        rb.angularVelocity = 0;

        //Guardamos la pieza que se esta apilando
        stackingPiece = pieces[totemPieces-1];
        //Calcular posicion de la próxima pieza
        //stackingPiece.Stack(Vec3ToVec2(transform.position) + Vector2.up*pieceHeight*totemPieces, stackDelay);
        stackingPiece.Stack(Vec3ToVec2(transform.position + Vector3.down*pieceHeight*(totemPieces-1)), stackDelay);
    }

    //Cuando una pieza se ha terminado de apilar se añade a la lista de piezas
    public void StackingFinished(PieceController piece)
    {
        stackedPieces.Add(piece);
        totemPieces++;

        AudioManager.audioManager.Play("Stack");

        //Mover hacia arriba totem
        transform.position += Vector3.up*pieceHeight;

        //Cambiar tamaño collider
        UpdateSize();
        
        //Vaciamos la pieza que estaba en proceso de apilar
        stackingPiece = null;

        //Obligamos a poner todas las piezas en el checkpoint
        if(onCheckZone && nPieces == totemPieces)
        {
            SetCheckPoint();
        }
    }

    //Desapilar una pieza devolviendola a su sitio en x tiempo
    void Unstack(float time)
    {
        stackingPiece = stackedPieces[stackedPieces.Count-1];
        stackingPiece.Unstack(time);
        stackedPieces.RemoveAt(stackedPieces.Count-1);
        totemPieces--;
        AudioManager.audioManager.Play("Unstack");

        //Cambiar tamaño collider
        UpdateSize();
    }

    //Desapilar inmediatamente todas las piezas dejandolas en su posicion actual
    void UnstackAll()
    {
        while(stackedPieces.Count>0)
        {
            stackingPiece = stackedPieces[stackedPieces.Count-1];
            stackingPiece.prevPos = stackingPiece.transform.position;
            Unstack(0);
        }

        stackingPiece = null;
    }

    public void UnstackingFinished(PieceController piece)
    {
        //Vaciamos la pieza que estaba en proceso de desapilar
        stackingPiece = null;
    }

    //Cambiar tamaño y posicion colider y posicion top
    void UpdateSize()
    {
        // //Cambiar tamaño collider
        // coll.size = new Vector2(coll.size.x, pieceHeight*totemPieces/transform.localScale.y);
        // coll.offset = Vector2.up*pieceHeight*0.5f/transform.localScale.y*(totemPieces-1);
        // //Cambiar posicion top
        // topPoint.localPosition = Vector2.up*coll.offset.y*2;
        //TODO: Fix posicion anclaje 

        //Cambiar tamaño collider
        coll.size = new Vector2(coll.size.x, pieceHeight*totemPieces/transform.localScale.y);
        coll.offset = Vector2.down*pieceHeight*0.5f/transform.localScale.y*(totemPieces-1);
        
        //Cambiar posicion top
        basePoint.localPosition = Vector2.down*(totemPieces-0.5f)*(pieceHeight/transform.transform.localScale.y);
        //(coll.offset.y*2 +pieceHeight/transform.localScale.y*0.5f);
        //basePoint.position = transform.position + Vector3.down*pieceHeight*(totemPieces-0.5f);

        //Colocar joints
        baseRightJoint.anchor = new Vector2(coll.size.x/2f, -(coll.size.y -pieceHeight*0.5f/transform.localScale.y) );
        baseLeftJoint.anchor = new Vector2(-baseRightJoint.anchor.x, baseRightJoint.anchor.y);
    }


    //Separar totem y desactivar joints
    void SeparatePieces()
    {
        AudioManager.audioManager.Play("Separate");

        //Guardar la posicion de la punta del totem mas lejana del joint
        Vector3 tip = transform.position;
        // if(anchored)
        // {
        //     float topLenght = Vector3.Distance(topPoint.position, currentAnchor.transform.position);               
        //     float botLenght = Vector3.Distance(basePoint.position, currentAnchor.transform.position);
        //     if(topLenght<botLenght)
        //         tip = basePoint.position;
        // }

        //Desapilar todas las piezas
        UnstackAll();

        //Resetear camino e indicarselo a las piezas que no estaban en el totem
        ResetPath();

        //Ponemos al lider en la punta del totem
        transform.position = tip;

        //Desacivamos las joints y dejamos el estado inicial del totem
        falling = false;
        anchored = false;
        grounded = false;
        baseLeftJoint.enabled = false;
        baseRightJoint.enabled = false;
        if(currentJoint!=null)
            currentJoint.enabled = false;

        //Poner de pie y parar
        transform.rotation = Quaternion.AngleAxis(0, Vector3.forward);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = 0;

        //Quitamos el anclaje actual
        currentAnchor = null;
    }

    //Resetear camino e indicarselo a las piezas que no estaban en el totem
    void ResetPath()
    {
        path.Clear();
        foreach(PieceController p in pieces)
        {
            p.ResetPath();
        }
    }

    //Anclar totem a un nuevo punto y desanclar del anterior
    void Anchor(Vector2 point)
    {
        AudioManager.audioManager.Play("Anchor");

        //Vectores a lo largo y ancho del totem
        Vector2 totemUp = transform.rotation * Vector3.up;
        

        float topLenght = Vector3.Distance(topPoint.position, currentAnchor.transform.position);               
        float botLenght = Vector3.Distance(basePoint.position, currentAnchor.transform.position);
        
        // if(topLenght<botLenght)
        //     totemUp = transform.rotation * Vector3.up;
        // else
        //     totemUp = transform.rotation * Vector3.down;
        //Debug.DrawLine(transform.position+Vec2ToVec3 (totemUp)*10, transform.position-Vec2ToVec3 (totemUp)*10, Color.gray, 5);

        Vector2 totemRight = new Vector2(totemUp.y, totemUp.x);
        //Vector desde joint hasta anclaje
        Vector2 jointAnchorLocal = point - currentJoint.connectedAnchor;
        //Vector desde posicion totem a anclaje
        Vector2 jointLocalPos = currentJoint.connectedAnchor - Vec3ToVec2(coll.bounds.center);//(transform.position);
        //Vector del ancho del totem hasta joint actual
        Vector2 cateto = jointLocalPos * totemRight / (totemRight.magnitude * totemRight.magnitude) * totemRight;
        //Obtenemos la altura a la que se situará el nuevo joint en el eje y del totem
        float anchorLocalHeight = Mathf.Sqrt(jointAnchorLocal.magnitude *jointAnchorLocal.magnitude - cateto.magnitude *cateto.magnitude);
        //anchorLocalHeight = Mathf.Min(anchorLocalHeight, collider.bounds.extents.y/2f);
        //Punto del nuevo anclaje en el totem en coordenadas mundo
        // Vector2 worldAnchorInTotem = totemUp.normalized * (anchorLocalHeight - coll.size.y * transform.localScale.y/2f) + Vec3ToVec2(coll.bounds.center);//(transform.position);
        // Vector2 worldAnchorInTotem = totemUp.normalized * (anchorLocalHeight - pieceHeight/2f) + Vec3ToVec2(transform.position);
        Vector2 worldAnchorInTotem = totemUp.normalized * (anchorLocalHeight) + Vec3ToVec2(basePoint.position);
        //Vector del joint actual al nuevo joint en el totem
        Vector2 jointAnchorTotem = worldAnchorInTotem - currentJoint.connectedAnchor;
        //Angulo que hay que rotar desde el joint actual para encajar con el anclaje
        float angle = Vector2.SignedAngle(jointAnchorTotem, jointAnchorLocal);
        transform.RotateAround(currentJoint.connectedAnchor, Vector3.forward, angle);

        //Debug.DrawLine(worldAnchorInTotem, currentJoint.connectedAnchor, Color.blue, 5);

        //FIX
        if(Mathf.Abs(angle) > 90)
        {
            if(angle > 0)
                angle = Mathf.Abs(angle) -180;
            else
                angle = Mathf.Abs(angle) +180;
        }

        //Desactivamos el anclaje previo, cambiamos y activamos al nuevo
        currentJoint.enabled = false;
        currentJoint = auxiliaryJoint;
        currentJoint.enabled = true;

        //Cambiamos el punto del nuevo joint en coordenadas locales del totem
        currentJoint.anchor = transform.InverseTransformPoint(point);
        currentJoint.anchor = new Vector2 (0, currentJoint.anchor.y);
        //currentJoint.anchor = new Vector2 (0, Mathf.Min(currentJoint.anchor.y, coll.size.y/2f - 0.1f));

        // if (anchored||true)
        // {
        //     //Debug:
        //     rb.isKinematic = true;
        //     rb.velocity = Vector2.zero;
        //     rb.angularVelocity = 0;
        // }

        anchored = true;
    }

    // IEnumerator DebugLine(Vector2 point1, Vector2 point 2)
    // {
    //     float time = 5, curr = 0;

    //     while (curr < time)
    //     {
    //         Debug.DrawLine(point1,point2, Color.red);
    //     }
    // }

    public Vector2 Vec3ToVec2(Vector3 init)
    {
        return new Vector2(init.x, init.y);
    }

    public Vector3 Vec2ToVec3(Vector2 init)
    {
        return new Vector3(init.x, init.y, 0);
    }

    //Rotar cuando esté en un anclaje
    void RotateInAnchor(float force)
    {
        rb.AddTorque(force*anchorForce, ForceMode2D.Impulse);
        //Limitar velocidad angular
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxAnchorVel, maxAnchorVel);
    }

    //Desactivar temporalmente un punto
    IEnumerator TemporaryDisableAnchor(GameObject anchor)
    {
        anchor.SetActive(false);
        yield return new WaitForSeconds(1);
        anchor.SetActive(true);
    }

    //Comprobamos si se ha dañado al totem al tocar un enemigo
    public void EnemyHit(int power)
    {
        //Si solo hay una pieza (lider) o se toca un invencible muere
        if(nPieces <= 1 || power == 0)
        {
            StartCoroutine(Death());
        }
        //Si tiene mas ataque el enemigo o no estaba cayendo el totem
        else if (power > totemPieces || !falling)
        {
            LosePiece();
        }
    }

    //Coger una nueva pieza
    void CollectPiece(Transform collectable)
    {
        AudioManager.audioManager.Play("GetPiece");

        //Reseteamos el camino para evitar problemas a la nueva pieza
        ResetPath();

        //Instanciamos, posicionamos y añadimos la pieza a las listas
        GameObject piece = Instantiate(piecePrefab);
        PieceController contr = piece.GetComponent<PieceController>();
        piece.transform.position = collectable.position;
        contr.orderNumber = pieces.Count+1;
        nPieces++;
        pieces.Add(contr);

        //TODO: Obtener sprite y colocar en la pieza
        contr.SetSprite(collectable.gameObject);

        //Desactivamos la pieza coleccionable
        checkDeletedPieces.Add(collectable.gameObject);
        collectable.gameObject.SetActive(false);

        //Delay de recoger piezas para evitar duplicados
        StartCoroutine(CollectDelay());
    }

    IEnumerator CollectDelay()
    {
        float time = 0.2f;
        collectDelay = true;
        yield return new WaitForSeconds(time);
        collectDelay = false;
    }

    void LosePiece(int num = -1)
    {
        AudioManager.audioManager.Play("LosePiece");

        //Si estaba en modo totem, se separa
        if(totemPieces>1)
        {
            SeparatePieces();
        }

        //Eliminar ultima pieza o pieza indicada
        PieceController piece;
        if(num == -1)
            piece = pieces[pieces.Count-1];
        else
            piece = pieces[num];
        pieces.Remove(piece);
        nPieces--;
        piece.Dead();
    }

    void SetCheckPoint()
    {
        AudioManager.audioManager.Play("Checkpoint");

        foreach (PieceController p in stackedPieces)
        {
            GameObject go = Instantiate(checkPiecePrefab);
            go.GetComponent<SpriteRenderer>().sprite = p.sprite;
            go.transform.position = p.transform.position;
        }

        int aux = pieces.Count-1;
        for(int i = 0; i<=aux; i++)
        {
            LosePiece();
        }
        
        // for(int i = 0; i < pieces.Count;i++)
        // {
        //     pieces[i].orderNumber = i+1;
        // }

        onCheckZone = false;

        //Actualizamos listas de piezas
        foreach(GameObject go in checkDeletedPieces)
        {
            deletedPieces.Add(go);
        }
        checkDeletedPieces.Clear();
    }

    IEnumerator Death()
    {
        dead = true;
        //TODO: Animacion muerte

        AudioManager.audioManager.Play("Death");

        //Separar totem si fuera necesario
        if(totemPieces>1)
            SeparatePieces();

        //Eliminar piezas acompañantes
        int aux = pieces.Count;
        for(int i = 0; i<aux; i++)
        {
            LosePiece();
        }

        //Desactivar collider
        coll.enabled = false;
        //Paramos totem
        rb.velocity = Vector3.zero;
        rb.angularVelocity = 0;

        //Esperar un poco
        yield return new WaitForSeconds(1f);

        //Volver al checkpoint
        transform.position = checkPointPos;

        //Resetear posiciones enemigos vivos
        foreach(EnemyController en in FindObjectsOfType<EnemyController>())
        {
            en.Reset();
        }

        //Reactivar piezas cogidas en el ultimo checkpoint
        foreach(GameObject go in checkDeletedPieces)
        {
            go.SetActive(true);
        }
        checkDeletedPieces.Clear();

        ResetPath();

        //reactivamos collider
        coll.enabled = true;
        dead = false;
    }
}
