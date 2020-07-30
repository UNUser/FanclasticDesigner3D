package ru.fanclastic.Fanclastic3DDesigner;

import android.os.Environment;

public class AndroidPlugin 
{
    public static String GetDownloadsPath() 
	{
        return Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS)
                .getAbsolutePath();
    }
}