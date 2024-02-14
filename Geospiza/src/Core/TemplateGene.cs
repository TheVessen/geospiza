using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Special;
using Microsoft.AspNetCore.Authentication;

namespace Geospiza.Core;

public class TemplateGene
{
    //General relevant properties
    public int TickValue { get; set; }
    public Type Type { get; private set; }
    public Guid GhInstanceGuid { get; private set; }
    public Guid GeneGuid { get; private set; }
    public int TickCount { get; private set; }
    private static Dictionary<Guid, GH_NumberSlider> _allSliders;
    private static Dictionary<Guid, dynamic> _allGenePools;
    
    //Slider relevant properties
    private GH_NumberSlider _slider;
    
    //GenePool relevant properties
    private dynamic _genPoolList;
    private int _genePoolIndex = -1;
    
    public TemplateGene(Dictionary<Guid, GH_NumberSlider> geneSliders, Dictionary<Guid, dynamic> genePools)
    {
        _allSliders = geneSliders;
        _allGenePools = genePools;
    }
    
    public TemplateGene(dynamic genPoolList, int geneIndex)
    {
        _genPoolList = genPoolList;
        _genePoolIndex = geneIndex;
        TickCount = genPoolList.TickCount;
        GeneGuid = Guid.NewGuid();
        Type = genPoolList.GetType();
        GhInstanceGuid = genPoolList.InstanceGuid;
    }
    
    public TemplateGene(GH_NumberSlider slider)
    {
        _slider = slider;
        TickCount = slider.TickCount;
        GeneGuid = Guid.NewGuid();
        Type = slider.GetType();
        GhInstanceGuid = slider.InstanceGuid;
    }
    
    public void SetTickValue(int tickValue)
    {
        if(_allSliders == null && _allGenePools == null)
        {
            throw new Exception("Gene class has not been initialized with a dictionary of sliders and gene pools.");
        }
        
        if(Type == typeof(GH_NumberSlider))
        {
            if (_allSliders != null) _allSliders[GhInstanceGuid].TickValue = tickValue;
            TickValue = tickValue;
        }
        else
        {
            if (_allGenePools != null) _allGenePools[GhInstanceGuid].set_TickValue(_genePoolIndex, tickValue);
            TickValue = tickValue;
            if (_allGenePools != null) _allGenePools[GhInstanceGuid].ExpireSolutionTopLevel(false);
        }
    }
}