using System.Collections;
using System.Collections.Generic;
using Tofunaut.TofuECS;
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var sim = new Simulation();
        sim.RegisterComponent<int>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
