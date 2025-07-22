using BarcodeStandard;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Type = BarcodeStandard.Type;

namespace WMS.Application.Helpers
{
    public class BarcodeHelper
    {
        public string GenerateCode128BarcodeAsBase64String(string value)
        {
            Barcode barcode = new Barcode();
            SkiaSharp.SKImage img = barcode.Encode(Type.Code128, value, SkiaSharp.SKColors.Black, SkiaSharp.SKColors.White, 500, 150);
            //convert to .png
            SKData encoded = img.Encode();
            //return as byte array
            byte[] byteArray = encoded.ToArray();
            //return as base64string
            string base64String = $"base64:{Convert.ToBase64String(byteArray)}";
            return base64String;
        }
    }
}
