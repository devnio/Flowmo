using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger : Singleton<Logger>
{
    [SerializeField]
    private bool DebugInfoActive = true;

    private void Start()
    {
        Debug.Log("Logger: Started");
    }

    /// <summary>
    /// Prints an information in the console if the flag is active.
    /// </summary>
    /// <param name="msg"></param>
    public void DebugInfo(string msg, string title = "")
    {
        if (DebugInfoActive)
        {
            Debug.Log("INFO " + title + ": " + msg);
        }
    }


    /// <summary>
    /// Debug tuples with title.
    /// </summary>
    /// <param name="tuples"></param>
    /// <param name="title"></param>
    public void DebugTuples(Tuple<int, int, float>[] tuples, string title = "")
    {
        Debug.Log(title + ": ");
        foreach(var tup in tuples)
        {
            Debug.Log("(" + tup.Item1 + ", " + tup.Item2 + ") : " + tup.Item3 + "\n");
        }
    }

    public void DebugParticleCoefficients(Vector3 currPoint, float[] coeffs, string title = "")
    {
        string text = title + ": |";
        text += "COEFFS FORM CURR POINT : " + currPoint.ToString() + "\n";

        float sum = 0f;
        for (int i = 0; i < coeffs.Length; i++)
        {
            text += "c[" + i.ToString() + "] = " + coeffs[i];
            sum += coeffs[i];
            text += ", ";
        }

        text += "SUM = " + sum.ToString() + "| \n";
        Debug.Log(text);
    }

    public void PrintMatrix(Decimal[,] matrix)
    {
        string text = "MATRIX: \n";
        // Assuem 3x4
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                text += matrix[i, j];
                text += "  ";
            }
            text += "\n";
        }
        Debug.Log(text);
    }
}
