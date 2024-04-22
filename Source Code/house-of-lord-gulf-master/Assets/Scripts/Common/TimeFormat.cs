using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeFormat 
{
    public static string GetFormattedUTCDate()
    {
        // This will get the current UTC time
        DateTime utcNow = DateTime.UtcNow;
        Debug.Log("UTC time "+ utcNow);
        // Formats the UTC DateTime in the desired format
        // This uses day-month-year format. Use "MM-dd-yyyy" for month-day-year
        string formattedDate = utcNow.ToString("dd-MM-yyyy");
        string formattedTime = utcNow.ToString("HH:mm:ss");
        return formattedDate+" "+ formattedTime;
    }
}
