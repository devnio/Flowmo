using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public Button StopSimulation;
    public Button NextFrame;

    public Text fps;

    public void StopSimulationPressed()
    {
        Logger.Instance.DebugInfo("STOP BUTTON PRESSED");
        VerletSimulation.Instance.StopSimulation(!VerletSimulation.Instance._stopSimulation);
    }

    public void NextFramePressed()
    {
        Logger.Instance.DebugInfo("NEXT FRAME SIMULATION", "NEXT BUTTON PRESSED");
        VerletSimulation.Instance.NextFrame();
    }

    private void Update()
    {
        fps.text = string.Format("{0:0.00}", (1f / Time.deltaTime));
    } 

}
