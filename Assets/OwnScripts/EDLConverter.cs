using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EDLConverter : MonoBehaviour
{
    //Returns the parameters which can be extracted from the given edl line
    public String[] ConvertFromEdlLine(String[] words)
    {
        String[] output = new String[5];

        //Extract the second column (expl: C001, C002, ..., C014, ...)
        output[0] = words[1];

        //Extract the third column (expl: AA/V, V, AA)
        output[1] = ConvertTransitionFromEDL(words[2]).ToString();

        //Extract the fourth clolumn (expl: Cut, Dissovle, Wipe)
        output[2] = ConvertModeFromEDL(words[3]);

        //Extract the fifth clolumn (expl: 00:00:00:00, 00:00:10:00)
        output[3] = words[4];

        //Extract the sixth clolumn (expl: 00:00:10:00, 00:11:04:00)
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
        str += "AX" + "       ";

        //Create the third clolumn (expl: AA/V, V, AA)
        str += ConvertTransitionToEDL(trans);

        //Create the fourth clolumn (expl: C, D, W)
        str += ConvertModeToEDL(mode);

        //Create the fifth clolumn (expl: 00:00:00:00, 00:00:10:00)
        str += FormatFromTimeSpan(srcIN) + " ";

        //Create the sixth clolumn (expl: 00:00:10:00, 00:11:04:00)
        str += FormatFromTimeSpan(srcOUT) + " ";

        //Create the seventh clolumn (expl: 00:00:00:00, 00:00:10:00)
        str += FormatFromTimeSpan(recIN) + " ";

        //Create the eighth clolumn (expl: 00:00:10:00, 00:11:04:00)
        str += FormatFromTimeSpan(recOUT);

        return str;
    }

    //Returns the transition string to implement in the created edl line
    private String ConvertTransitionToEDL(int trans)
    {
        //Check if the transition number is 0
        if (trans == 0)
        {
            //Video and Audio
            return "AA/V" + "  ";
        }
        //Check if the transition number is 1
        else if (trans == 1)
        {
            //Only Video
            return "V" + "     ";
        }
        //Check if the transition number is 2
        else
        {
            //Only Audio
            return "AA" + "    ";
        }
    }

    //Returns the number which is extracted from the transition string from the given edl line
    private int ConvertTransitionFromEDL(String trans)
    {
        //Check if the transition string equals the audio and video mode
        if (trans == "AA/V")
        {
            //Video and Audio
            return 0;
        }
        //Check if the transition string equals the only video mode
        else if (trans == "V")
        {
            //Only Video
            return 1;
        }
        //Check if the transition string equals the only audio mode
        else
        {
            //Only Audio
            return 2;
        }
    }

    //Returns the mode string to implement in the created edl line
    private String ConvertModeToEDL(String mode)
    {
        //Check if the mode string equals the Cut mode
        if (mode == "Cut")
        {
            //Cut
            return "C" + "   ";
        }
        //Check if the mode string equals the Dissolve mode
        else if (mode == "Dissolve")
        {
            //Dissolve
            return "D" + "   ";
        }
        //Check if the mode string equals the Wipe mode
        else
        {
            //Wipe
            return "W" + "   ";
        }
    }

    //Returns the string which is extracted from the mode string from the given edl line
    private String ConvertModeFromEDL(String mode)
    {
        //Check if the mode string equals the Cut mode
        if (mode == "C")
        {
            //Cut
            return "Cut";
        }
        //Check if the mode string equals the Dissolve mode
        else if (mode == "D")
        {
            //Dissolve
            return "Dissolve";
        }
        //Check if the mode string equals the Wipe mode
        else
        {
            //Wipe
            return "Wipe";
        }
    }

    //Returns a formatted string to use in an edl line from the given TimeSpan
    private String FormatFromTimeSpan(TimeSpan time)
    {
        String output = time.ToString();

        //Check if the given TimeSpan is 00:00:00
        if (time == TimeSpan.Zero)
        {
            //Add the milliseconds section
            output += ":00";
        }
        else
        {
            //Extract and shorten the milliseconds section from the given TimeSpan
            String milSecStr = output.Substring(output.LastIndexOf(".") + 1, 2);

            //Set the output to only the hours, minutes and seconds sections (xx:xx:xx)
            output = output.Substring(0, output.LastIndexOf("."));

            //Add the milliseconds section
            output += ":" + milSecStr;
        }

        return output;
    }
}
