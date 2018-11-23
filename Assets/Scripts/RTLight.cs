﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LightType { Directional, Point }

[RequireComponent(typeof(Light))]
public abstract class RTLight : MonoBehaviour {

    public float intensity;
    public Color color;

}
