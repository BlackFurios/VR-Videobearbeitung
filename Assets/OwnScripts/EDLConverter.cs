﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EDLConverter : MonoBehaviour
{
    private ManageHighlights mh;                                //Instance of the ManageHighlights script

    // Use this for initialization
    void Start ()
    {
        //Sets the manageHighlights script
        mh = GetComponent<ManageHighlights>();
    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public String ConvertEdlLine(int lineCnt, int mode, int trans, TimeSpan srcIN, TimeSpan srcOUT, TimeSpan recIN, TimeSpan recOUT)
    {
        String str = "";
        String fmt = "000";
        
        //Create the first column (expl: 001, 002, ..., 014, ...)
        str += lineCnt.ToString(fmt) + "  ";

        //Create the second clolumn (expl: C001, C002, ..., C014, ...)
        //str += "C" + lineCnt.ToString(fmt) + "      ";
        str += "AX" + "       ";

        //Create the third clolumn (expl: )
        if (trans == 0)
        {
            //Video and Audio
            str += "AA/V" + "  ";
        }
        else if (trans == 1)
        {
            //Only Video
            str += "V" + "     ";
        }
        else
        {
            //Only Audio
            str += "AA" + "    ";
        }

        //Create the fourth clolumn (expl: )
        if (mode == 0)
        {
            //Cut
            str += "C" + "   ";
        }
        else if (mode == 1)
        {
            //Dissolve
            str += "D" + "   ";
        }
        else
        {
            //Wipe
            str += "W" + "   ";
        }

        //Create the fifth clolumn (expl: 00:00:00:00, 00:00:10:00)
        str += srcIN.ToString() + " ";

        //Create the sixth clolumn (expl: 00:00:10:00, 00:11:04:00)
        str += srcOUT.ToString() + " ";

        //Create the seventh clolumn (expl: 00:00:00:00, 00:00:10:00)
        str += recIN.ToString() + " ";

        //Create the eighth clolumn (expl: 00:00:10:00, 00:11:04:00)
        str += recOUT.ToString();

        return str;
    }
}