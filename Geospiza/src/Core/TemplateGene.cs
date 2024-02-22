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
    public string Name { get; private set; }
    public int TickCount { get; private set; }


    //Slider relevant properties
    private GH_NumberSlider _slider;

    //GenePool relevant properties
    private dynamic _genPoolList;
    public int GenePoolIndex { get; private set; }

    public TemplateGene(dynamic genPoolList, int geneIndex)
    {
        _genPoolList = genPoolList;
        GenePoolIndex = geneIndex;
        TickCount = genPoolList.TickCount;
        GeneGuid = Guid.NewGuid();
        Type = genPoolList.GetType();
        Name = genPoolList.NickName;
        GhInstanceGuid = genPoolList.InstanceGuid;
    }

    public TemplateGene(GH_NumberSlider slider)
    {
        _slider = slider;
        TickCount = slider.TickCount;
        GeneGuid = Guid.NewGuid();
        Type = slider.GetType();
        GhInstanceGuid = slider.InstanceGuid;
        Name = slider.NickName;
        GenePoolIndex = -1;
    }

    public void SetTickValue(int tickValue, StateManager stateManager)
    {
        var _allSliders = stateManager._allSliders;
        var _allGenePools = stateManager._allGenePools;
        if (_allSliders == null && _allGenePools == null)
        {
            throw new Exception("Gene class has not been initialized with a dictionary of sliders and gene pools.");
        }

        if (Type == typeof(GH_NumberSlider))
        {
            if (_allSliders != null) _allSliders[GhInstanceGuid].TickValue = tickValue;
            TickValue = tickValue;
        }
        else
        {
            if (_allGenePools != null) _allGenePools[GhInstanceGuid].set_TickValue(GenePoolIndex, tickValue);
            TickValue = tickValue;
            if (_allGenePools != null) _allGenePools[GhInstanceGuid].ExpireSolutionTopLevel(false);
        }
    }

}