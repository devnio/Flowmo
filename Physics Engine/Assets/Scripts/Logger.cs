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
        if (DebugInfoActive)
        {
            Debug.Log(title + ": ");
            foreach (var tup in tuples)
            {
                Debug.Log("(" + tup.Item1 + ", " + tup.Item2 + ") : " + tup.Item3 + "\n");
            }
        }
    }

    public void DebugParticleCoefficients(Vector3 currPoint, float[] coeffs, string title = "")
    {
        if (DebugInfoActive)
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
    }

    public void PrintMatrix(double[,] matrix, int rows, int cols, string title="")
    {
        if (DebugInfoActive)
        {
            string text = "MATRIX " + title + "  \n";
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    text += matrix[i, j];
                    text += "  ";
                }
                text += "\n";
            }
            Debug.Log(text);
        }
    }
}
