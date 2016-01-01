using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct dplane_t
{
    public Vector3 normal;	// normal vector
    public float dist;	// distance from origin
    public int type;	// plane axis identifier
}