using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserModel : MonoBehaviour
{
    // Store ability informations
    private Dictionary<string, bool> ability = new Dictionary<string, bool>()
    {
         {"Double Jump", false},
         {"Dash", false },
    };
           
    // Start is called before the first frame update
    void Start()
    {
        // For test
        ability["Double Jump"] = true;
        ability["Dash"] = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool GetAbility(string name)
    {
        return ability[name];
    }
}
