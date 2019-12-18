using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIClothManager : Singleton<UIClothManager>
{
    public Button GenerateClothButton;

    // Resolution
    [Header("Resolution")]
    public Slider clothResolution_Slider;
    public Text clothResolution_SliderText;


    // Stiffness
    [Header("Stiffness")]
    public Slider structuralStiffness_Slider;
    public Text structuralStiffness_SliderText;

    public Slider bendStiffness_Slider;
    public Text bendStiffness_SliderText;

    public Slider shearStiffness_Slider;
    public Text shearStiffness_SliderText;


    // Damping
    [Header("Damping")]
    public Slider structuralDamping_Slider;
    public Text structuralDamping_SliderText;

    public Slider bendDamping_Slider;
    public Text bendDamping_SliderText;

    public Slider shearDamping_Slider;
    public Text shearDamping_SliderText;

    [Header("Cloth (should get automatically assigned.)")]
    public Cloth cloth;


    private void Start()
    {
        // Setup default values
        clothResolution_Slider.value = 0.2f;

        structuralStiffness_Slider.value = 0.8f;
        bendStiffness_Slider.value = 0.8f;
        shearStiffness_Slider.value = 0.8f;

        structuralDamping_Slider.value = 0.25f;
        bendDamping_Slider.value = 0.25f;
        shearDamping_Slider.value = 0.25f;

        // Update all
        Slider_ClothResolution_Changed();

        Slider_StructuralStiffness_Changed();
        Slider_BendStiffness_Changed();
        Slider_ShearStiffness_Changed();

        Slider_StructuralDamping_Changed();
        Slider_BendDamping_Changed();
        Slider_ShearDamping_Changed();
    }

    // ================
    // Generation Methods
    // ================
    public void GenereateCloth()
    {
        cloth.GenerateCloth();
        VerletSimulation.Instance.StopSimulation(false);
    }

    public void ResetCloth()
    {
        VerletSimulation.Instance.StopSimulation(true);
        cloth.ResetCloth();
    }

    public void Slider_ClothResolution_Changed()
    {
        int sliderValue = Mathf.Clamp((int)(clothResolution_Slider.value * 12f), 3, 12);
        clothResolution_SliderText.text = sliderValue.ToString();
        cloth.clothParams.clothSize = sliderValue;
    }

    // ================
    // Stiffness Methods
    // ================
    public void Slider_StructuralStiffness_Changed()
    {
        float sliderValue = structuralStiffness_Slider.value;
        structuralStiffness_SliderText.text = sliderValue.ToString("0.00");
        cloth.clothParams.structuralStiffness = sliderValue;
    }

    public void Slider_BendStiffness_Changed()
    {
        float sliderValue = bendStiffness_Slider.value;
        bendStiffness_SliderText.text = sliderValue.ToString("0.00");
        cloth.clothParams.bendStiffness = sliderValue;
    }

    public void Slider_ShearStiffness_Changed()
    {
        float sliderValue = shearStiffness_Slider.value;
        shearStiffness_SliderText.text = sliderValue.ToString("0.00");
        cloth.clothParams.shearStiffness = sliderValue;
    }

    // ================
    // Damping Methods
    // ================
    public void Slider_StructuralDamping_Changed()
    {
        float sliderValue = structuralDamping_Slider.value;
        structuralDamping_SliderText.text = sliderValue.ToString("0.00");
        cloth.clothParams.structuralDamping = sliderValue;
    }

    public void Slider_BendDamping_Changed()
    {
        float sliderValue = bendDamping_Slider.value;
        bendDamping_SliderText.text = sliderValue.ToString("0.00");
        cloth.clothParams.bendDamping = sliderValue;
    }

    public void Slider_ShearDamping_Changed()
    {
        float sliderValue = shearDamping_Slider.value;
        shearDamping_SliderText.text = sliderValue.ToString("0.00");
        cloth.clothParams.shearDamping = sliderValue;
    }
}
