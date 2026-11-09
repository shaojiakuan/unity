using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyerBelt : MonoBehaviour
{
    public Transform A;
    public Transform B;
    public int NumberOfBoards = 20;
    public float Radius = 0.4f;
    public List<GameObject> boards;
    public GameObject Board;
    public float Speed=1;
    public float StartL;
    public ParticleSystem particle;
    public bool IsRunning;
    public MaterialPile pile;
    // Start is called before the first frame update
    void Start()
    { 
        GenerateBoards();
        StartL = 0;
    }
    public Vector3 getPositionAndRotationByL(float l, out Quaternion rot)
    {
        float Length = Vector3.Distance(A.position, B.position);
        float TotalLength = 2 * Length;
        TotalLength += 2 * Radius * Mathf.PI;
        while(l<0|| l>TotalLength)
        {
            if (l < 0)
                l += TotalLength;
            if (l > TotalLength)
                l -= TotalLength;
        }
        if (l < Length)
        {
            rot = Quaternion.LookRotation(transform.forward, Vector3.Cross(B.position - A.position, transform.forward).normalized);
            return A.position + Vector3.Cross(B.position - A.position, transform.forward).normalized * Radius + (B.position - A.position).normalized * l; 
        }
        else if (l >= Length && l < Length + Radius * Mathf.PI)
        {
            float t = l - Length;
            float angle = t / Radius;
            rot = Quaternion.LookRotation( transform.forward,
                  (Quaternion.AngleAxis(angle * Mathf.Rad2Deg, B.forward) * Vector3.Cross(B.position - A.position, transform.forward).normalized * Radius).normalized);
            return B.position + (Quaternion.AngleAxis(angle * Mathf.Rad2Deg, B.forward) * Vector3.Cross(B.position - A.position, transform.forward)).normalized * Radius;
     
        }
        else if (l >= Length + Radius * Mathf.PI && l < Length * 2 + Radius * Mathf.PI)
        {
            float t = l - Length - Radius * Mathf.PI; 
            rot = Quaternion.LookRotation(transform.forward, Vector3.Cross(A.position - B.position, transform.forward).normalized);
            return B.position + Vector3.Cross(A.position - B.position, transform.forward).normalized * Radius + (A.position - B.position).normalized * t; 
        }
        else
        {
            float t = l - Length * 2 - Radius * Mathf.PI;
            float angle = t / Radius;
            rot = Quaternion.LookRotation(-transform.forward,
                  (Quaternion.AngleAxis(angle * Mathf.Rad2Deg, A.forward) * Vector3.Cross(A.position - B.position, transform.forward).normalized * Radius).normalized); 
            return A.position + (Quaternion.AngleAxis(angle * Mathf.Rad2Deg, A.forward) * Vector3.Cross(A.position - B.position, transform.forward)).normalized * Radius;
        }
      
    }

    public void GenerateBoards()
    {
        float Length = Vector3.Distance(A.position, B.position);
        float TotalLength =2* Length;
        TotalLength +=2* Radius * Mathf.PI;
        for(int i=boards.Count-1;i>=0;i--)
        {
            Destroy (boards[i]); 
        }
        boards.Clear();
        float l = 0;
        while (l < TotalLength)
        {
            Quaternion rot;
            Vector3 position=getPositionAndRotationByL(l,out rot);
            GameObject newBoard = GameObject.Instantiate(Board, position, rot);
            boards.Add(newBoard);
            l += TotalLength / NumberOfBoards;
        }
    }

    private void FixedUpdate()
    {
    }
    public void SetRunning(bool running)
    {
        IsRunning = running;
        if(IsRunning)
        {
            particle.Play();
            pile.IsGenerating = true;
        }
        else
        {
            particle.Stop();

            pile.IsGenerating = false;
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (IsRunning)
        {
            StartL += Speed * Time.deltaTime;
            float Length = Vector3.Distance(A.position, B.position);
            float TotalLength = 2 * Length + 2 * Radius * Mathf.PI;
            for (int i = 0; i < NumberOfBoards; i++)
            {
                Quaternion rot;
                Vector3 position = getPositionAndRotationByL(StartL + TotalLength / NumberOfBoards * i, out rot);
                boards[i].transform.rotation = rot;
                boards[i].transform.position = position;
            }
        }
    }
}
