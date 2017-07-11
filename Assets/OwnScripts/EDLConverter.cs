using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EDLConverter : MonoBehaviour
{
    // Use this for initialization
    void Start ()
    {

    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    //
    public String[] ConvertFromEdlLine(String[] words)
    {
        String[] output = new String[5];

        //
        output[0] = words[1];

        //
        output[1] = ConvertTransitionFromEDL(words[2]).ToString();

        //
        output[2] = ConvertModeFromEDL(words[3]);

        //
        output[3] = words[4];

        //
        output[4] = words[5];

        return output;
    }

    //Converts given parameters into a String which resembles a line of a edit decision list
    public String ConvertToEdlLine(int lineCnt, int trans, String mode, TimeSpan srcIN, TimeSpan srcOUT, TimeSpan recIN, TimeSpan recOUT)
    {
        String str = "";
        String fmt = "000";
        
        //Create the first column (expl: 001, 002, ..., 014, ...)
        str += lineCnt.ToString(fmt) + "  ";

        //Create the second clolumn (expl: C001, C002, ..., C014, ...)
        //str += "C" + lineCnt.ToString(fmt) + "      ";
        str += "AX" + "       ";

        //Create the third clolumn (expl: )
        str += ConvertTransitionToEDL(trans);

        //Create the fourth clolumn (expl: )
        str += ConvertModeToEDL(mode);

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

    //
    private String ConvertTransitionToEDL(int trans)
    {
        //
        if (trans == 0)
        {
            //Video and Audio
            return "AA/V" + "  ";
        }
        else if (trans == 1)
        {
            //Only Video
            return "V" + "     ";
        }
        else
        {
            //Only Audio
            return "AA" + "    ";
        }
    }

    //
    private int ConvertTransitionFromEDL(String trans)
    {
        //
        if (trans == "AA/V")
        {
            //Video and Audio
            return 0;
        }
        else if (trans == "V")
        {
            //Only Video
            return 1;
        }
        else
        {
            //Only Audio
            return 2;
        }
    }

    private String ConvertModeToEDL(String mode)
    {
        if (mode == "Cut")
        {
            //Cut
            return "C" + "   ";
        }
        else if (mode == "Dissolve")
        {
            //Dissolve
            return "D" + "   ";
        }
        else
        {
            //Wipe
            return "W" + "   ";
        }
    }

    private String ConvertModeFromEDL(String mode)
    {
        if (mode == "C")
        {
            //Cut
            return "Cut";
        }
        else if (mode == "D")
        {
            //Dissolve
            return "Dissolve";
        }
        else
        {
            //Wipe
            return "Wipe";
        }
    }
}
