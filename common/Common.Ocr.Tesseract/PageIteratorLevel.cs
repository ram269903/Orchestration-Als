using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Ocr.Tesseract
{
    public enum PageIteratorLevel : int
    {
        Block,
        Para, 
        TextLine, 
        Word, 
        Symbol
    }
}
