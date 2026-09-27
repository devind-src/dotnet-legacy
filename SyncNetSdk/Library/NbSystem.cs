using System;

namespace SyncNet.Library
{
    public class NbSystem
    {
        public static string GetFolderProgramFiles()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        public static bool IsNumeric(string strnum)
        {
            if (string.IsNullOrEmpty(strnum) == true) return false;

            bool bval = true;

            try
            {
                for (int i = 0; i < strnum.Length; i++)
                {
                    string s = strnum.Substring(i, 1);

                    Int32.Parse(s);
                }
            }
            catch
            {
                bval = false;
            }

            return bval;
        }

        public static void ResizeArray(ref string[,] original, int rows, int cols)
        {
            //create a new 2 dimensional array with
            //the size we want
            string[,] newArray = new string[rows, cols];
            //copy the contents of the old array to the new one
            Array.Copy(original, newArray, original.Length);
            //set the original to the new array
            original = newArray;
        }
    }
}
