using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public LinkedListNode<Vector2> nextStep;

    //Variables estáticas compartidas por todas las piezas de totem
    public static float moveSpeed = 5;
    public static float straightAngSpeed = 200;
    public static float stackSpeed = 1;
    public static float arriveThres = 0.2f;
    public static float defaultMoveDelay = 0.2f;

    //Numero identificador de cada pieza que indica su orden y el tiempo que tendra de delay al moverse
    public int orderNumber = 1;
    public float currMoveDelay, pieceMoveDelay;
    public bool stacking = false;
    public Vector2 prevPos;

    public Sprite sprite;

    Coroutine moveCoroutine;


    // Start is called before the first frame update
    void Start()
    {
        //TODO:Asignar orden
        //Obtenemos el tiempo de delay de nuestra pieza en funcion de su orden
        pieceMoveDelay = orderNumber * defaultMoveDelay;
        currMoveDelay = 0;
        sprite = GetComponent<SpriteRenderer>().sprite;
    }

    public void SetSprite(GameObject collectable)
    {
        sprite = collectable.GetComponent<SpriteRenderer>().sprite;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    public void SetSprite(Sprite _sprite)
    {
        GetComponent<SpriteRenderer>().sprite = _sprite;
    }

    // Update is called once per frame
    void Update()
    {
        //Si la pieza no esta en totem (o en proceso de apilarse) esperamos un poco y seguimos el camino descrito
        if(!stacking)
        {
            if(currMoveDelay>pieceMoveDelay)
            {
                FollowPath();
                //Enderezar si esta torcido
                if(transform.rotation.z != 0)
                {
                    float angle = Mathf.Min(Mathf.Abs(transform.rotation.z* Mathf.Rad2Deg), straightAngSpeed*Time.deltaTime);
                    if(transform.rotation.z >0)
                        transform.Rotate(0,0, -angle);
                    else
                        transform.Rotate(0,0, angle);
                }
            }
            else
                currMoveDelay += Time.deltaTime;
        }
        else
        {
            currMoveDelay = 0;
        }
    }

    //Seguir el camino del lider cuando camina
    void FollowPath()
    {
        //Si hay path, se sigue
        if(TotemController.path != null && TotemController.path.Count > 0)
        {
            //Si no tiene siguiente paso asignado se le asigna el primero
            if(nextStep == null)
            {
                nextStep = TotemController.path.First;
            } 

            //Si no esta en el ultimo punto y está cerca del siguiente punto se le asigna como objetivo
            if(nextStep.Next != null && Vector2.Distance(nextStep.Value, Vec3ToVec2(transform.position)) < arriveThres)
            {
                nextStep = nextStep.Next;
            }   

            //Nos movemos al siguiente punto. Si estamos en el ultimo punto y estamos cerca, no se mueve
            if(nextStep != TotemController.path.Last || Vector2.Distance(nextStep.Value, Vec3ToVec2(transform.position)) > arriveThres)
            {
                Vector2 dir = (nextStep.Value - Vec3ToVec2 (transform.position)).normalized;
                transform.position += Vec2ToVec3(moveSpeed * dir * Time.deltaTime);

                //TODO: Añadir movimiento de "flotar" con turbulencias para suavizar la trayectoria
            }
            else
            {
                currMoveDelay = 0;    
            }
        }
    }

    //Desplazar y apilar pieza en el totem
    public void Stack(Vector2 target, float time)
    {
        stacking = true;
        prevPos = Vec3ToVec2(transform.position);
        if(moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveToPoint(target, time, TotemController.totem.transform));
    }

    //Dejar pieza en el lugar previo a apilarlo (sirve tambien para cortar proceso a medio)
    public void Unstack(float time)
    {
        stacking = false;
        if(moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveToPoint(prevPos, time));
    }

    //Corrutina para desplazar la pieza a un punto en cierto tiempo
    public IEnumerator MoveToPoint(Vector2 target, float time, Transform parent = null)
    {
        float currTime = 0;
        float dist = Vector2.Distance(Vec3ToVec2(transform.position), target);
        float speed = dist/time;
        float angularSpeed = transform.rotation.z/time;
        
        //Desplazar y rotar al objetivo hasta alcanzar el tiempo exigido
        while (currTime < time)
        {
            Vector2 dir = (target - Vec3ToVec2(transform.position)).normalized;
            transform.position += Vec2ToVec3(dir*speed*Time.deltaTime);
            //transform.Rotate(0,0, -angularSpeed*Time.deltaTime);
            currTime += Time.deltaTime;
            yield return null;
        }
        transform.position = Vec2ToVec3(target);
        //transform.rotation = new Quaternion(0,0,0,1);
        
        //Se informa al totem de que se ha terminado de apilar
        if(parent != null)
        {
            TotemController.totem.StackingFinished(this);
            //Emparentamos la pieza al totem una vez colocada o quitamos el parent (si es null)
            transform.SetParent(parent);
            transform.localPosition = new Vector2 (0,transform.localPosition.y);
            transform.rotation = new Quaternion(0,0,0,1);
        }
        else{
            TotemController.totem.UnstackingFinished(this);
            transform.SetParent(parent);
        }
    }

    public void ResetPath()
    {
        nextStep = null;
    }

    public Vector2 Vec3ToVec2(Vector3 init)
    {
        return new Vector2(init.x, init.y);
    }

    public Vector3 Vec2ToVec3(Vector2 init)
    {
        return new Vector3(init.x, init.y, 0);
    }

    public void Dead()
    {
        Destroy(gameObject);
        //TODO: particulas
    }
}
