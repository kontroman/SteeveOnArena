using System;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    public static class ColorExtensions
    {
        //----------------------------------------------------------------------------------------------------

        #region Hex Conversions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string RGBToHex(this Color color)
        {
            return RGBToHex((Color32)color);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="color32"></param>
        /// <returns></returns>
        public static string RGBToHex(this Color32 color32)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", color32.r, color32.g, color32.b);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static Color32 HexToRGB(this string hex)
        {
            string subString_Check = hex.Substring(0, 7);

            if (subString_Check != hex)
            {
                VerboseLogs.Log($"Invalid string hex code: {hex}", LogType.Error);
                return new Color32();
            }
            
            return new Color32(
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16),
                255
            );
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Hexa Conversions
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string RGBAToHexa(this Color color)
        {
            return RGBAToHexa((Color32)color);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="color32"></param>
        /// <returns></returns>
        public static string RGBAToHexa(this Color32 color32)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color32.r, color32.g, color32.b, color32.a);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hexa"></param>
        /// <returns></returns>
        public static Color32 HexaToRGBA(this string hexa)
        {
            string subString_Check = hexa.Substring(0, 9);

            if (subString_Check != hexa)
            {
                VerboseLogs.Log($"Invalid string hexa code: {hexa}", LogType.Error);
                return new Color32();
            }
            
            return new Color32(
                Convert.ToByte(hexa.Substring(1, 2), 16),
                Convert.ToByte(hexa.Substring(3, 2), 16),
                Convert.ToByte(hexa.Substring(5, 2), 16),
                Convert.ToByte(hexa.Substring(7, 2), 16)
                );
        }

        #endregion

    } // class end
}